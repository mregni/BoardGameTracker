using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using SkiaSharp;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class PlayerLinkPromptTests : IAsyncLifetime
{
    private static readonly Uri Https = new("https://localhost");
    private readonly IntegrationFixture _fixture;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];
    private FakeIdentityProvider _idp = null!;
    private int _providerId;

    public PlayerLinkPromptTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _idp = await FakeIdentityProvider.StartAsync();
        _idp.ClientSecret = "fake-idp-player-link-secret";
        _idp.Picture = CreatePng();
        _idp.User["sub"] = $"fake-sub-link-{_suffix}";
        _idp.User["email"] = $"lina-{_suffix}@example.com";
        _idp.User["preferred_username"] = $"lina-{_suffix}";
        _idp.User["name"] = $"Lina Link {_suffix}";
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
    public async Task FirstSignIn_ShouldAskForAPlayer_UntilTheAccountIsLinked()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await _fixture.CreateAdminClientAsync();
        var player = await CreatePlayerAsync(admin, $"Someone {_suffix}", $"LINA-{_suffix}@example.com");
        var partly = await CreatePlayerAsync(admin, $"Lina Link {_suffix} Junior", null);
        var stranger = await CreatePlayerAsync(admin, $"Lina Link {_suffix} Senior", $"senior-{_suffix}@example.com");
        using var browser = _fixture.CreateBrowserClient(Https);

        var first = await SignInAsync(browser);

        first.User.NeedsPlayerLink.Should().BeTrue();
        using var api = BearerClient(first.AccessToken);
        await WaitForProfileImageAsync(api);

        var suggestions = await api.GetFromJsonAsync<PlayerLinkSuggestionsResponse>("/api/auth/player-link/suggestions", IntegrationFixture.Json, ct);
        suggestions!.Suggestions[0].Id.Should().Be(player.Id);
        suggestions.Suggestions[0].Reason.Should().Be(PlayerLinkMatchReason.Email);
        suggestions.Suggestions.Select(s => s.Id).Should().Contain(partly.Id);
        suggestions.Players.Select(p => p.Id).Should().Contain([player.Id, partly.Id]);
        suggestions.Players.Select(p => p.Id).Should().NotContain(stranger.Id);
        suggestions.CanCreatePlayer.Should().BeTrue();

        var forced = await api.PostAsJsonAsync("/api/auth/player-link", new LinkOwnPlayerRequest { PlayerId = stranger.Id }, ct);
        forced.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await forced.Content.ReadAsStringAsync(ct)).Should().Contain("error.auth.player-email-mismatch");

        var linked = await api.PostAsJsonAsync("/api/auth/player-link", new LinkOwnPlayerRequest { PlayerId = player.Id }, ct);

        linked.StatusCode.Should().Be(HttpStatusCode.OK, await linked.Content.ReadAsStringAsync(ct));
        var profile = await linked.Content.ReadFromJsonAsync<ProfileResponse>(IntegrationFixture.Json, ct);
        profile!.PlayerId.Should().Be(player.Id);
        profile.NeedsPlayerLink.Should().BeFalse();
        var synced = await admin.GetFromJsonAsync<PlayerDto>($"/api/player/{player.Id}", IntegrationFixture.Json, ct);
        synced!.ImageSource.Should().Be(PlayerImageSource.Account);
        synced.Image.Should().NotBeNullOrEmpty();

        using var user = await _fixture.CreateClientAsAsync("user");
        var conflict = await user.PostAsJsonAsync("/api/auth/player-link", new LinkOwnPlayerRequest { PlayerId = player.Id }, ct);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await conflict.Content.ReadAsStringAsync(ct)).Should().Contain("error.auth.player-already-linked");

        var second = await SignInAsync(browser);
        second.User.NeedsPlayerLink.Should().BeFalse();

        await admin.DeleteAsync($"/api/player/{player.Id}", ct);
        await admin.DeleteAsync($"/api/player/{partly.Id}", ct);
        await admin.DeleteAsync($"/api/player/{stranger.Id}", ct);
    }

    [Fact]
    public async Task Reader_ShouldNotCreateAPlayer_ButMaySkip()
    {
        var ct = TestContext.Current.CancellationToken;
        using var reader = await _fixture.CreateClientAsAsync("reader");

        var created = await reader.PostAsync("/api/auth/player-link/new", null, ct);
        created.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var suggestions = await reader.GetFromJsonAsync<PlayerLinkSuggestionsResponse>("/api/auth/player-link/suggestions", IntegrationFixture.Json, ct);
        suggestions!.CanCreatePlayer.Should().BeFalse();

        var skipped = await reader.PostAsync("/api/auth/player-link/skip", null, ct);
        skipped.StatusCode.Should().Be(HttpStatusCode.OK);
        (await skipped.Content.ReadFromJsonAsync<ProfileResponse>(IntegrationFixture.Json, ct))!.NeedsPlayerLink.Should().BeFalse();
    }

    private static async Task<PlayerDto> CreatePlayerAsync(HttpClient admin, string name, string? email)
    {
        var response = await admin.PostAsJsonAsync("/api/player", new CreatePlayerCommand { Name = name, Email = email }, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return (await response.Content.ReadFromJsonAsync<PlayerDto>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;
    }

    private HttpClient BearerClient(string accessToken)
    {
        var api = _fixture.CreateClient();
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return api;
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

    private static async Task WaitForProfileImageAsync(HttpClient api)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var profile = await api.GetFromJsonAsync<ProfileResponse>("/api/auth/profile", IntegrationFixture.Json, TestContext.Current.CancellationToken);
            if (profile!.ProfileImage != null)
            {
                profile.NeedsPlayerLink.Should().BeTrue();
                return;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException("The identity provider picture was not imported");
    }

    private static byte[] CreatePng()
    {
        using var bitmap = new SKBitmap(64, 64);
        bitmap.Erase(SKColors.SeaGreen);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
