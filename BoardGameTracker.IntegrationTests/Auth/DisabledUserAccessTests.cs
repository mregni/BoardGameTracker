using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public class DisabledUserAccessTests : IDisposable
{
    private const string FileName = "disabled-user-avatar.webp";
    private readonly IntegrationFixture _fixture;
    private readonly string _path;

    public DisabledUserAccessTests(IntegrationFixture fixture)
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
    public async Task DisablingAUser_ShouldStopTheirAccessTokenAndImageCookieImmediately_AndEnablingRestoresThem()
    {
        const string username = "disabled-now";
        using var admin = await _fixture.CreateAdminClientAsync();
        var register = await admin.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, $"{username}@example.com", IntegrationFixture.UserPassword, Constants.AuthRoles.User), TestContext.Current.CancellationToken);
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        var userId = (await register.Content.ReadFromJsonAsync<UserDto>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!.Id;

        using var browser = _fixture.CreateBrowserClient();
        var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var accessToken = (await login.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!.AccessToken;
        using var api = _fixture.CreateClient();
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        (await api.GetAsync("/api/auth/profile", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await browser.GetAsync($"/images/profile/{FileName}", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        var disable = await admin.PostAsync($"/api/admin/users/{userId}/disable", null, TestContext.Current.CancellationToken);
        disable.StatusCode.Should().Be(HttpStatusCode.OK, await disable.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        (await api.GetAsync("/api/auth/profile", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, "a disabled user's access token stops working right away");
        (await api.GetAsync("/api/game", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await browser.GetAsync($"/images/profile/{FileName}", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the image cookie does not outlive the account");

        var enable = await admin.PostAsync($"/api/admin/users/{userId}/enable", null, TestContext.Current.CancellationToken);
        enable.StatusCode.Should().Be(HttpStatusCode.OK);

        (await api.GetAsync("/api/auth/profile", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await browser.GetAsync($"/images/profile/{FileName}", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletingAUser_ShouldStopTheirAccessTokenImmediately()
    {
        const string username = "deleted-now";
        using var admin = await _fixture.CreateAdminClientAsync();
        var register = await admin.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, $"{username}@example.com", IntegrationFixture.UserPassword, Constants.AuthRoles.User), TestContext.Current.CancellationToken);
        var userId = (await register.Content.ReadFromJsonAsync<UserDto>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!.Id;
        using var api = await _fixture.CreateClientAsAsync(username);
        (await api.GetAsync("/api/auth/profile", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        var delete = await admin.DeleteAsync($"/api/admin/users/{userId}", TestContext.Current.CancellationToken);
        delete.IsSuccessStatusCode.Should().BeTrue(await delete.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        (await api.GetAsync("/api/auth/profile", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
