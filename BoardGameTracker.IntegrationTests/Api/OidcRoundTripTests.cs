using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class OidcRoundTripTests : IAsyncLifetime
{
    private const string ProviderName = "fake";
    private readonly IntegrationFixture _fixture;
    private FakeIdentityProvider _idp = null!;
    private int _providerId;

    public OidcRoundTripTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _idp = await FakeIdentityProvider.StartAsync();
        using var admin = await _fixture.CreateAdminClientAsync();
        var response = await admin.PostAsJsonAsync("/api/admin/oidc-providers", new CreateOidcProviderRequest(
            ProviderName, "Fake IdP", _idp.Authority, "bgt-client", null, "openid profile email", true,
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
    public async Task Login_ShouldRoundTripThroughTheProvider_WithoutPuttingATokenInAnyUrl()
    {
        using var browser = _fixture.CreateBrowserClient();
        using var idpBrowser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        var start = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/login?redirect=%2Fgames%2F3");

        start.StatusCode.Should().Be(HttpStatusCode.Found);
        var authorizeUrl = start.Headers.Location!;
        authorizeUrl.GetLeftPart(UriPartial.Path).Should().Be($"{_idp.Authority}/authorize");
        var authorizeQuery = HttpUtility.ParseQueryString(authorizeUrl.Query);
        authorizeQuery["redirect_uri"].Should().Be($"http://localhost/api/auth/oidc/{ProviderName}/callback");
        authorizeQuery["code_challenge_method"].Should().Be("S256");
        authorizeQuery["nonce"].Should().NotBeNullOrEmpty();
        start.Headers.GetValues("Set-Cookie").Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=", StringComparison.Ordinal) && c.Contains("httponly", StringComparison.OrdinalIgnoreCase) && c.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));

        var idpResponse = await idpBrowser.GetAsync(authorizeUrl);
        idpResponse.StatusCode.Should().Be(HttpStatusCode.Found);
        var callbackUrl = idpResponse.Headers.Location!;
        callbackUrl.GetLeftPart(UriPartial.Path).Should().Be($"http://localhost/api/auth/oidc/{ProviderName}/callback");

        var callback = await browser.GetAsync(callbackUrl.PathAndQuery);

        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be("http://localhost/auth-callback?redirect=%2Fgames%2F3");
        callback.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith($"{OidcController.HandoffCookieName}=", StringComparison.Ordinal) && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var adopt = await browser.PostAsync("/api/auth/oidc/adopt", null);
        adopt.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await adopt.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json);
        login!.User.Username.Should().Be("jane");
        login.User.DisplayName.Should().Be("Jane Doe");
        login.User.Roles.Should().BeEquivalentTo(Constants.AuthRoles.User);
        login.AccessToken.Should().NotBeNullOrEmpty();

        using var api = _fixture.CreateClient();
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var profile = await api.GetAsync("/api/auth/profile");
        profile.StatusCode.Should().Be(HttpStatusCode.OK);

        var replay = await browser.PostAsync("/api/auth/oidc/adopt", null);
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Callback_ShouldRejectAForgedState_WithoutTouchingTheProvider()
    {
        using var browser = _fixture.CreateBrowserClient();

        var callback = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/callback?code=forged&state=forged");

        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be($"http://localhost/auth-callback?error={Uri.EscapeDataString(Constants.Errors.InvalidAuthSession)}");
    }

    [Fact]
    public async Task Callback_ShouldReportAProviderRefusal_AsAStableErrorKey()
    {
        using var browser = _fixture.CreateBrowserClient();
        await browser.GetAsync($"/api/auth/oidc/{ProviderName}/login");

        var callback = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/callback?error=access_denied&state=whatever");

        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be($"http://localhost/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcProviderRejected)}");
    }

    [Fact]
    public async Task Callback_ShouldRejectAStolenState_WhenTheBrowserCookieIsMissing()
    {
        using var victim = _fixture.CreateBrowserClient();
        using var attacker = _fixture.CreateBrowserClient();
        using var idpBrowser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        var start = await victim.GetAsync($"/api/auth/oidc/{ProviderName}/login");
        var idpResponse = await idpBrowser.GetAsync(start.Headers.Location!);

        var callback = await attacker.GetAsync(idpResponse.Headers.Location!.PathAndQuery);

        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be($"http://localhost/auth-callback?error={Uri.EscapeDataString(Constants.Errors.InvalidAuthSession)}");
        var adopt = await attacker.PostAsync("/api/auth/oidc/adopt", null);
        adopt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Setup_ShouldTellTheAdminWhichRedirectUrisToRegister()
    {
        using var admin = await _fixture.CreateAdminClientAsync();

        var setup = await admin.GetFromJsonAsync<OidcSetupDto>("/api/admin/oidc-providers/setup", IntegrationFixture.Json);

        setup!.PublicBaseUrl.Should().Be("http://localhost");
        setup.PublicUrlConfigured.Should().BeFalse();
        setup.CallbackUriTemplate.Should().Be("http://localhost/api/auth/oidc/{name}/callback");
        setup.LinkCallbackUriTemplate.Should().Be("http://localhost/api/auth/oidc/{name}/link-callback");
    }

    [Fact]
    public async Task TestDiscovery_ShouldReturnTheProvidersEndpoints()
    {
        using var admin = await _fixture.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/admin/oidc-providers/test-discovery", new TestOidcDiscoveryRequest(_idp.Authority));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<OidcDiscoveryResultDto>(IntegrationFixture.Json);
        result!.Issuer.Should().Be(_idp.Authority);
        result.TokenEndpoint.Should().Be($"{_idp.Authority}/token");
        result.IssuerMatchesAuthority.Should().BeTrue();
    }

    [Fact]
    public async Task TestDiscovery_ShouldBeAdminOnly_AndRefuseAnInsecureAuthority()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        using var admin = await _fixture.CreateAdminClientAsync();

        var forbidden = await user.PostAsJsonAsync("/api/admin/oidc-providers/test-discovery", new TestOidcDiscoveryRequest(_idp.Authority));
        var insecure = await admin.PostAsJsonAsync("/api/admin/oidc-providers/test-discovery", new TestOidcDiscoveryRequest("http://idp.example.com"));

        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        insecure.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Link_ShouldAttachTheProviderToTheSignedInUser_AndUnlinkAgain()
    {
        _idp.User["sub"] = "link-sub-" + Guid.NewGuid().ToString("N");
        using var browser = _fixture.CreateBrowserClient();
        using var idpBrowser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword));
        var session = await login.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json);
        browser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        var start = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/link");
        start.StatusCode.Should().Be(HttpStatusCode.OK, await start.Content.ReadAsStringAsync());
        var authorization = await start.Content.ReadFromJsonAsync<AuthorizationUrlResponse>(IntegrationFixture.Json);
        HttpUtility.ParseQueryString(new Uri(authorization!.Url).Query)["redirect_uri"].Should().Be($"http://localhost/api/auth/oidc/{ProviderName}/link-callback");

        var idpResponse = await idpBrowser.GetAsync(authorization.Url);
        browser.DefaultRequestHeaders.Authorization = null;
        var callback = await browser.GetAsync(idpResponse.Headers.Location!.PathAndQuery);
        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be($"http://localhost/auth-callback?linked={ProviderName}");

        browser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var logins = await browser.GetFromJsonAsync<List<ExternalLoginDto>>("/api/auth/external-logins", IntegrationFixture.Json);
        logins.Should().ContainSingle(x => x.Provider == ProviderName && x.ProviderDisplayName == "Jane Doe");

        var unlink = await browser.DeleteAsync($"/api/auth/external-logins/{logins![0].Id}");
        unlink.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await browser.GetFromJsonAsync<List<ExternalLoginDto>>("/api/auth/external-logins", IntegrationFixture.Json)).Should().BeEmpty();
    }

    [Fact]
    public async Task Adopt_ShouldReturn401_WithoutAHandoff()
    {
        using var browser = _fixture.CreateBrowserClient();

        var adopt = await browser.PostAsync("/api/auth/oidc/adopt", null);

        adopt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
