using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class ProfileImageAccessTests : IDisposable
{
    private const string FileName = "integration-avatar.webp";
    private readonly IntegrationFixture _fixture;
    private readonly string _path;

    public ProfileImageAccessTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
        Directory.CreateDirectory(PathHelper.FullProfileImagePath);
        _path = Path.Combine(PathHelper.FullProfileImagePath, FileName);
        File.WriteAllBytes(_path, [0x52, 0x49, 0x46, 0x46]);
    }

    public void Dispose()
    {
        File.Delete(_path);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ProfileImage_ShouldRequireASession()
    {
        using var anonymous = _fixture.CreateClient();

        var response = await anonymous.GetAsync($"/images/profile/{FileName}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProfileImage_ShouldBeServed_WithABearerToken()
    {
        using var user = await _fixture.CreateClientAsAsync("reader");

        var response = await user.GetAsync($"/images/profile/{FileName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProfileImage_ShouldBeServed_WithTheCookieFromLogin_AndRefusedAfterLogout()
    {
        using var browser = _fixture.CreateBrowserClient();

        var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest(IntegrationFixture.AdminUsername, IntegrationFixture.AdminPassword));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        login.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith($"{ProfileImageCookie.Name}=", StringComparison.Ordinal) && c.Contains("path=/images/profile", StringComparison.OrdinalIgnoreCase));
        var session = await login.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json);

        var served = await browser.GetAsync($"/images/profile/{FileName}");
        served.StatusCode.Should().Be(HttpStatusCode.OK);

        var cover = await browser.GetAsync("/images/cover/does-not-exist.webp");
        cover.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout") { Content = JsonContent.Create(new LogoutRequest(session!.RefreshToken)) };
        logout.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var loggedOut = await browser.SendAsync(logout);
        loggedOut.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refused = await browser.GetAsync($"/images/profile/{FileName}");
        refused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VersionInfo_ShouldRequireASession()
    {
        using var anonymous = _fixture.CreateClient();

        var response = await anonymous.GetAsync("/api/settings/version-info");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
