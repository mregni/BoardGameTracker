using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class OidcPictureTests : IAsyncLifetime
{
    private static readonly Uri Https = new("https://localhost");
    private readonly IntegrationFixture _fixture;
    private FakeIdentityProvider _idp = null!;
    private int _providerId;

    public OidcPictureTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _idp = await FakeIdentityProvider.StartAsync();
        _idp.ClientSecret = "fake-idp-picture-secret";
        _idp.Picture = CreatePng();
        _idp.User["sub"] = "fake-sub-picture";
        _idp.User["email"] = "pia@example.com";
        _idp.User["preferred_username"] = "pia";
        _idp.User["name"] = "Pia Picture";
        _idp.User["picture"] = $"{_idp.Authority}/avatar.png";

        using var admin = await _fixture.CreateAdminClientAsync();
        var response = await admin.PostAsJsonAsync("/api/admin/oidc-providers", new CreateOidcProviderRequest(
            "Fake IdP", _idp.Authority, _idp.ClientId, _idp.ClientSecret, "openid profile email", true,
            null, null, null, null, null, null, null, null, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        _providerId = (await response.Content.ReadFromJsonAsync<OidcProviderDto>(IntegrationFixture.Json))!.Id;
    }

    public async ValueTask DisposeAsync()
    {
        using var admin = await _fixture.CreateAdminClientAsync();
        await admin.DeleteAsync($"/api/admin/oidc-providers/{_providerId}");
        await _idp.DisposeAsync();
    }

    [Fact]
    public async Task Login_ShouldStoreTheProviderPicture_AndServeItWithTheImageCookie()
    {
        using var browser = _fixture.CreateBrowserClient(Https);

        var first = await SignInAsync(browser);

        var profile = await WaitForProfileImageAsync(first.AccessToken);
        profile.ProfileImage.Should().StartWith("/images/profile/");
        _idp.PictureRequests.Should().Be(1);
        await using (var scope = _fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var user = await context.Users.SingleAsync(u => u.UserName == "pia", TestContext.Current.CancellationToken);
            user.ProfileImageSource.Should().Be(ProfileImageSource.IdentityProvider);
            user.ProfileImageSourceUrl.Should().Be($"{_idp.Authority}/avatar.png");
        }

        var served = await browser.GetAsync(profile.ProfileImage, TestContext.Current.CancellationToken);
        served.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await SignInAsync(browser);
        second.User.ProfileImage.Should().Be(profile.ProfileImage, "the sign-in response carries the stored picture");
        await Task.Delay(500, TestContext.Current.CancellationToken);
        _idp.PictureRequests.Should().Be(1, "an unchanged picture address is not downloaded again");

        File.Delete(PathHelper.MapImageWebPathToPhysical(profile.ProfileImage!)!);
    }

    [Fact]
    public async Task ProfileImage_ShouldBeUploadedAndRemoved_ByTheSignedInUser()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(CreatePng());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "File", "me.png");

        var uploaded = await user.PutAsync("/api/auth/profile/image", form, TestContext.Current.CancellationToken);

        uploaded.StatusCode.Should().Be(HttpStatusCode.OK, await uploaded.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var profile = await uploaded.Content.ReadFromJsonAsync<ProfileResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken);
        profile!.ProfileImage.Should().StartWith("/images/profile/");
        var physical = PathHelper.MapImageWebPathToPhysical(profile.ProfileImage!)!;
        File.Exists(physical).Should().BeTrue();

        var removed = await user.DeleteAsync("/api/auth/profile/image", TestContext.Current.CancellationToken);

        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await removed.Content.ReadFromJsonAsync<ProfileResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!.ProfileImage.Should().BeNull();
        File.Exists(physical).Should().BeFalse();
    }

    [Fact]
    public async Task PictureClient_ShouldRefuseToConnect_WithoutVettedAddresses()
    {
        var factory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient(BoardGameTracker.Core.Auth.Pictures.IdentityProviderPictureDownloader.HttpClientName);

        var action = () => client.GetAsync($"{_idp.Authority}/avatar.png", TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<HttpRequestException>();
        _idp.PictureRequests.Should().Be(0);
    }

    private static async Task<LoginResponse> SignInAsync(HttpClient browser)
    {
        using var idpBrowser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var start = await browser.GetAsync("/api/auth/oidc/login", TestContext.Current.CancellationToken);
        var idpResponse = await idpBrowser.GetAsync(start.Headers.Location!, TestContext.Current.CancellationToken);
        var callback = await browser.GetAsync(idpResponse.Headers.Location!.PathAndQuery, TestContext.Current.CancellationToken);
        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().NotContain("error");

        var adopt = await browser.PostAsync("/api/auth/oidc/adopt", null, TestContext.Current.CancellationToken);
        adopt.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await adopt.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;
    }

    private async Task<ProfileResponse> WaitForProfileImageAsync(string accessToken)
    {
        using var api = _fixture.CreateClient();
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var profile = await api.GetFromJsonAsync<ProfileResponse>("/api/auth/profile", IntegrationFixture.Json, TestContext.Current.CancellationToken);
            if (profile!.ProfileImage != null || DateTime.UtcNow > deadline)
            {
                return profile;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
    }

    private static byte[] CreatePng()
    {
        using var bitmap = new SKBitmap(64, 64);
        bitmap.Erase(SKColors.OrangeRed);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
