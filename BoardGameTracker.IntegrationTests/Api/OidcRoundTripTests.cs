using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Core.Email.Interfaces;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class OidcRoundTripTests : IAsyncLifetime
{
    private const string ProviderName = "fake";
    private static readonly Uri Https = new("https://localhost");
    private static readonly Uri LanHttp = new("http://192.168.1.20:5444");
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
        _idp.ClientSecret = "fake-idp-secret";
        using var admin = await _fixture.CreateAdminClientAsync();
        var response = await admin.PostAsJsonAsync("/api/admin/oidc-providers", new CreateOidcProviderRequest(
            ProviderName, "Fake IdP", _idp.Authority, _idp.ClientId, _idp.ClientSecret, "openid profile email", true,
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
        using var browser = _fixture.CreateBrowserClient(Https);
        using var idpBrowser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        var start = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/login?redirect=%2Fgames%2F3");

        start.StatusCode.Should().Be(HttpStatusCode.Found);
        var authorizeUrl = start.Headers.Location!;
        authorizeUrl.GetLeftPart(UriPartial.Path).Should().Be($"{_idp.Authority}/authorize");
        var authorizeQuery = HttpUtility.ParseQueryString(authorizeUrl.Query);
        authorizeQuery["redirect_uri"].Should().Be($"https://localhost/api/auth/oidc/{ProviderName}/callback");
        authorizeQuery["code_challenge_method"].Should().Be("S256");
        authorizeQuery["nonce"].Should().NotBeNullOrEmpty();
        start.Headers.GetValues("Set-Cookie").Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=", StringComparison.Ordinal) && c.Contains("httponly", StringComparison.OrdinalIgnoreCase) && c.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));

        var idpResponse = await idpBrowser.GetAsync(authorizeUrl);
        idpResponse.StatusCode.Should().Be(HttpStatusCode.Found);
        var callbackUrl = idpResponse.Headers.Location!;
        callbackUrl.GetLeftPart(UriPartial.Path).Should().Be($"https://localhost/api/auth/oidc/{ProviderName}/callback");

        var callback = await browser.GetAsync(callbackUrl.PathAndQuery);

        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be("/auth-callback?redirect=%2Fgames%2F3");
        callback.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith($"{OidcController.HandoffCookieName}=", StringComparison.Ordinal) && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        callback.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith($"{OidcController.SessionCookieName}=", StringComparison.Ordinal) && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        callbackUrl.ToString().Should().NotContain("id_token");

        var adopt = await browser.PostAsync("/api/auth/oidc/adopt", null);
        adopt.StatusCode.Should().Be(HttpStatusCode.OK);
        adopt.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith($"{AuthSessionCookies.RefreshCookieName}=", StringComparison.Ordinal) && c.Contains("path=/api/auth;", StringComparison.Ordinal) && c.Contains("httponly", StringComparison.Ordinal) && c.Contains("samesite=lax", StringComparison.Ordinal));
        (await adopt.Content.ReadAsStringAsync()).Should().NotContainEquivalentOf("refreshToken");
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

        using var refresh = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refresh.Headers.Add(RequireSessionHeaderAttribute.HeaderName, RequireSessionHeaderAttribute.HeaderValue);
        (await browser.SendAsync(refresh)).StatusCode.Should().Be(HttpStatusCode.OK, "the adopted session lives on in the refresh cookie");
    }

    [Fact]
    public async Task Login_ShouldKeepWorking_AfterTheProviderIsSavedWithoutRetypingTheSecret()
    {
        using (var admin = await _fixture.CreateAdminClientAsync())
        {
            var update = await admin.PutAsJsonAsync($"/api/admin/oidc-providers/{_providerId}", new UpdateOidcProviderRequest(
                _providerId, "Fake IdP renamed", _idp.Authority, _idp.ClientId, null, true, "openid profile email", true,
                null, null, null, null, null, null, null, null, null, null));
            update.StatusCode.Should().Be(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
        }

        _idp.User["sub"] = "fake-sub-secret";
        _idp.User["email"] = "sam@example.com";
        _idp.User["preferred_username"] = "sam";
        _idp.User["name"] = "Sam Doe";
        using var browser = _fixture.CreateBrowserClient(Https);
        using var idpBrowser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var start = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/login");
        var idpResponse = await idpBrowser.GetAsync(start.Headers.Location!);

        var callback = await browser.GetAsync(idpResponse.Headers.Location!.PathAndQuery);

        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be("/auth-callback?redirect=%2F");
        callback.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith($"{OidcController.HandoffCookieName}=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_ShouldSendTheBrowserToTheErrorPage_WhenTheProviderDoesNotExist()
    {
        using var browser = _fixture.CreateBrowserClient(Https);

        var start = await browser.GetAsync("/api/auth/oidc/no-such-provider/login");

        start.StatusCode.Should().Be(HttpStatusCode.Found);
        start.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcProviderUnavailable)}");
    }

    [Fact]
    public async Task Login_ShouldContinueOnThePublicUrl_WhenStartedFromAnotherAddress_AndFinishThere()
    {
        UseIdentity("public-host");
        using var browser = _fixture.CreateBrowserClientWith(
            services => services.AddScoped<IPublicUrlBuilder>(_ => new FixedPublicUrl("https://localhost")),
            new Uri("http://127.0.0.1:5444"));
        using var idpBrowser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        using var start = new HttpRequestMessage(HttpMethod.Get, $"/api/auth/oidc/{ProviderName}/login?redirect=%2Fgames");
        start.Headers.Referrer = new Uri("http://127.0.0.1:5444/login?redirect=%2Fgames");
        var moved = await browser.SendAsync(start);

        moved.StatusCode.Should().Be(HttpStatusCode.Found);
        moved.Headers.Location.Should().Be(new Uri($"https://localhost/api/auth/oidc/{ProviderName}/login?redirect=%2Fgames"));
        moved.Headers.Contains("Set-Cookie").Should().BeFalse("no state cookie may land on the address the callback never returns to");

        var onPublicHost = await browser.GetAsync(moved.Headers.Location);
        onPublicHost.StatusCode.Should().Be(HttpStatusCode.Found);
        var idpResponse = await idpBrowser.GetAsync(onPublicHost.Headers.Location!);
        var callbackUrl = idpResponse.Headers.Location!;
        callbackUrl.GetLeftPart(UriPartial.Authority).Should().Be("https://localhost");

        var callback = await browser.GetAsync(callbackUrl);

        callback.Headers.Location!.ToString().Should().Be("/auth-callback?redirect=%2Fgames");
        var adopt = await browser.PostAsync("https://localhost/api/auth/oidc/adopt", null);
        adopt.StatusCode.Should().Be(HttpStatusCode.OK, await adopt.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SingleSignOn_ShouldNotUseUpThePasswordLoginLimit()
    {
        using var browser = _fixture.CreateBrowserClient(Https);
        for (var i = 0; i < RateLimitPolicies.AuthPermitsPerMinute + 2; i++)
        {
            (await browser.PostAsync("/api/auth/oidc/adopt", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var password = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword));
        password.StatusCode.Should().Be(HttpStatusCode.OK, "SSO requests have their own budget");
    }

    [Fact]
    public async Task Login_ShouldSendARateLimitedBrowserToTheErrorPage()
    {
        using var browser = _fixture.CreateBrowserClient(Https);
        for (var i = 0; i < RateLimitPolicies.OidcPermitsPerMinute; i++)
        {
            await browser.GetAsync($"/api/auth/oidc/{ProviderName}/login");
        }

        var limited = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/login");

        limited.StatusCode.Should().Be(HttpStatusCode.Found);
        limited.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.TooManyRequests)}");
    }

    [Fact]
    public async Task Callback_ShouldRejectAForgedState_WithoutTouchingTheProvider()
    {
        using var browser = _fixture.CreateBrowserClient(Https);

        var callback = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/callback?code=forged&state=forged");

        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.InvalidAuthSession)}");
    }

    [Fact]
    public async Task Callback_ShouldReportAProviderRefusal_AsAStableErrorKey()
    {
        using var browser = _fixture.CreateBrowserClient(Https);
        await browser.GetAsync($"/api/auth/oidc/{ProviderName}/login");

        var callback = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/callback?error=access_denied&state=whatever");

        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcProviderRejected)}");
    }

    [Fact]
    public async Task Callback_ShouldRejectAStolenState_WhenTheBrowserCookieIsMissing()
    {
        using var victim = _fixture.CreateBrowserClient(Https);
        using var attacker = _fixture.CreateBrowserClient(Https);
        using var idpBrowser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        var start = await victim.GetAsync($"/api/auth/oidc/{ProviderName}/login");
        var idpResponse = await idpBrowser.GetAsync(start.Headers.Location!);

        var callback = await attacker.GetAsync(idpResponse.Headers.Location!.PathAndQuery);

        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.InvalidAuthSession)}");
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
        setup.PostLogoutRedirectUri.Should().Be("http://localhost/login");
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
        using var browser = _fixture.CreateBrowserClient(Https);
        using var idpBrowser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword));
        var session = await login.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json);
        browser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        var start = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/link");
        start.StatusCode.Should().Be(HttpStatusCode.OK, await start.Content.ReadAsStringAsync());
        var authorization = await start.Content.ReadFromJsonAsync<AuthorizationUrlResponse>(IntegrationFixture.Json);
        HttpUtility.ParseQueryString(new Uri(authorization!.Url).Query)["redirect_uri"].Should().Be($"https://localhost/api/auth/oidc/{ProviderName}/link-callback");

        var idpResponse = await idpBrowser.GetAsync(authorization.Url);
        browser.DefaultRequestHeaders.Authorization = null;
        var callback = await browser.GetAsync(idpResponse.Headers.Location!.PathAndQuery);
        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be($"/auth-callback?linked={ProviderName}");

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
        using var browser = _fixture.CreateBrowserClient(Https);

        var adopt = await browser.PostAsync("/api/auth/oidc/adopt", null);

        adopt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Callback_ShouldRejectAnIdTokenThatIsNotSignedByTheProvider()
    {
        _idp.TamperIdToken = true;
        UseIdentity("tampered");
        using var browser = _fixture.CreateBrowserClient(Https);

        var callback = await SignInAsync(browser);

        callback.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcIdTokenInvalid)}");
        callback.Headers.TryGetValues("Set-Cookie", out var cookies);
        (cookies ?? []).Should().NotContain(c => c.StartsWith($"{OidcController.HandoffCookieName}=", StringComparison.Ordinal));
        var adopt = await browser.PostAsync("/api/auth/oidc/adopt", null);
        adopt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Callback_ShouldRejectAnIssParameterOfAnotherIssuer()
    {
        _idp.IssuerParameterOverride = "https://evil.example.com";
        UseIdentity("mixup");
        using var browser = _fixture.CreateBrowserClient(Https);

        var callback = await SignInAsync(browser);

        callback.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcIssuerMismatch)}");
    }

    [Fact]
    public async Task Logout_ShouldReturnTheProvidersSignOutUrl_WithTheIdTokenHint()
    {
        UseIdentity("logout");
        using var browser = _fixture.CreateBrowserClient(Https);
        await SignInAsync(browser);
        var idToken = _idp.LastIdToken;

        var logout = await browser.PostAsync("/api/auth/oidc/logout", null);

        logout.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await logout.Content.ReadFromJsonAsync<OidcLogoutResponse>(IntegrationFixture.Json);
        var url = new Uri(body!.Url!);
        url.GetLeftPart(UriPartial.Path).Should().Be($"{_idp.Authority}/logout");
        var query = HttpUtility.ParseQueryString(url.Query);
        query["id_token_hint"].Should().Be(idToken);
        query["post_logout_redirect_uri"].Should().Be("https://localhost/login");
        query["client_id"].Should().Be(_idp.ClientId);

        var again = await browser.PostAsync("/api/auth/oidc/logout", null);
        (await again.Content.ReadFromJsonAsync<OidcLogoutResponse>(IntegrationFixture.Json))!.Url.Should().BeNull();
    }

    [Fact]
    public async Task Rename_ShouldKeepTheLinkedAccount()
    {
        UseIdentity("rename");
        LoginResponse first;
        using (var browser = _fixture.CreateBrowserClient(Https))
        {
            await SignInAsync(browser);
            first = await AdoptAsync(browser);
        }

        using (var admin = await _fixture.CreateAdminClientAsync())
        {
            var update = await admin.PutAsJsonAsync($"/api/admin/oidc-providers/{_providerId}", new UpdateOidcProviderRequest(
                _providerId, "Fake IdP", _idp.Authority, _idp.ClientId, null, true, "openid profile email", true,
                null, null, null, null, null, null, null, null, null, null, null, "fake-renamed"));
            update.StatusCode.Should().Be(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
        }

        using var again = _fixture.CreateBrowserClient(Https);
        var callback = await SignInAsync(again, "fake-renamed");
        callback.Headers.Location!.ToString().Should().StartWith("/auth-callback?redirect=");
        var second = await AdoptAsync(again);

        second.User.Id.Should().Be(first.User.Id);
        using var api = _fixture.CreateClient();
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", second.AccessToken);
        var logins = await api.GetFromJsonAsync<List<ExternalLoginDto>>("/api/auth/external-logins", IntegrationFixture.Json);
        logins.Should().ContainSingle().Which.Provider.Should().Be("fake-renamed");
    }

    [Fact]
    public async Task Login_ShouldReconcileTheRoleFromTheGroupsClaim_OnEveryLogin()
    {
        await ConfigureRoleMappingAsync();
        UseIdentity("roles");
        _idp.User["groups"] = new[] { "bgt-admins" };

        using (var browser = _fixture.CreateBrowserClient(Https))
        {
            await SignInAsync(browser);
            (await AdoptAsync(browser)).User.Roles.Should().BeEquivalentTo(Constants.AuthRoles.Admin);
        }

        _idp.User["groups"] = new[] { "staff" };
        using (var browser = _fixture.CreateBrowserClient(Https))
        {
            await SignInAsync(browser);
            (await AdoptAsync(browser)).User.Roles.Should().BeEquivalentTo(Constants.AuthRoles.Reader);
        }
    }

    [Fact]
    public async Task Login_ShouldNotDeadEnd_WhenALocalAccountHasTheSameUsername()
    {
        UseIdentity("clash");
        _idp.User["preferred_username"] = "user";
        using var browser = _fixture.CreateBrowserClient(Https);

        var callback = await SignInAsync(browser);

        callback.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcUsernameTaken)}");
    }

    [Fact]
    public async Task Admin_ShouldSeeAndUnlinkTheSsoIdentities_AndADisabledUserCannotSignInWithSso()
    {
        UseIdentity("deprovision");
        using var session = _fixture.CreateBrowserClient(Https);
        await SignInAsync(session);
        var login = await AdoptAsync(session);

        using var admin = await _fixture.CreateAdminClientAsync();
        var users = await admin.GetFromJsonAsync<List<UserDto>>("/api/admin/users", IntegrationFixture.Json);
        var listed = users!.Single(u => u.Id == login.User.Id);
        listed.ExternalLoginCount.Should().Be(1);
        listed.RoleManagedByIdentityProvider.Should().BeTrue();

        var disable = await admin.PostAsync($"/api/admin/users/{login.User.Id}/disable", null);
        disable.StatusCode.Should().Be(HttpStatusCode.OK, await disable.Content.ReadAsStringAsync());
        (await disable.Content.ReadFromJsonAsync<UserDto>(IntegrationFixture.Json))!.IsDisabled.Should().BeTrue();

        using (var browser = _fixture.CreateBrowserClient(Https))
        {
            var refused = await SignInAsync(browser);
            refused.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.AccountDisabled)}");
        }

        using var refresh = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refresh.Headers.Add(RequireSessionHeaderAttribute.HeaderName, RequireSessionHeaderAttribute.HeaderValue);
        (await session.SendAsync(refresh)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, "disabling the user revoked the refresh cookie's token");

        using var anonymous = _fixture.CreateClient();

        var adminLogin = await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(IntegrationFixture.AdminUsername, IntegrationFixture.AdminPassword));
        adminLogin.StatusCode.Should().Be(HttpStatusCode.OK);

        var links = await admin.GetFromJsonAsync<List<ExternalLoginDto>>($"/api/admin/users/{login.User.Id}/external-logins", IntegrationFixture.Json);
        links.Should().ContainSingle();
        var unlink = await admin.DeleteAsync($"/api/admin/users/{login.User.Id}/external-logins/{links![0].Id}");
        unlink.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.GetFromJsonAsync<List<ExternalLoginDto>>($"/api/admin/users/{login.User.Id}/external-logins", IntegrationFixture.Json)).Should().BeEmpty();

        var enable = await admin.PostAsync($"/api/admin/users/{login.User.Id}/enable", null);
        (await enable.Content.ReadFromJsonAsync<UserDto>(IntegrationFixture.Json))!.IsDisabled.Should().BeFalse();
    }

    [Fact]
    public async Task Admin_ShouldNotBeAbleToDisableThemselves()
    {
        using var admin = await _fixture.CreateAdminClientAsync();
        var users = await admin.GetFromJsonAsync<List<UserDto>>("/api/admin/users", IntegrationFixture.Json);
        var self = users!.Single(u => u.Username == IntegrationFixture.AdminUsername);

        var disable = await admin.PostAsync($"/api/admin/users/{self.Id}/disable", null);

        disable.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ShouldBeRefusedOverPlainHttp_OnANonLoopbackAddress()
    {
        using var browser = _fixture.CreateBrowserClient(LanHttp);

        var start = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/login?redirect=%2Fgames");

        start.StatusCode.Should().Be(HttpStatusCode.Found);
        start.Headers.Location!.ToString().Should().Be($"/login?error={Uri.EscapeDataString(Constants.Errors.OidcHttpsRequired)}");
        start.Headers.TryGetValues("Set-Cookie", out var cookies);
        (cookies ?? []).Should().NotContain(c => c.StartsWith($"{OidcController.StateCookieName}=", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("login?redirect=%2Fgames", "/login")]
    [InlineData("callback?code=code&state=state", "/auth-callback")]
    [InlineData("link-callback?code=code&state=state", "/auth-callback")]
    public async Task OidcFlow_ShouldIgnoreSpoofedForwardedHeaders_WithoutATrustedProxy(string endpoint, string refusalPage)
    {
        using var browser = _fixture.CreateBrowserClient(LanHttp);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/auth/oidc/{ProviderName}/{endpoint}");
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-Host", "localhost");
        request.Headers.Add("X-Forwarded-For", "127.0.0.1");

        var response = await browser.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.ToString().Should().Be($"{refusalPage}?error={Uri.EscapeDataString(Constants.Errors.OidcHttpsRequired)}");
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        (cookies ?? []).Should().NotContain(c => c.StartsWith("bgt_oidc_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_ShouldOnlyRedirectToTheProvidersAuthorizationEndpoint_WhateverTheRequestHost()
    {
        using var browser = _fixture.CreateBrowserClient(new Uri("https://evil.example.com"));

        var start = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/login?redirect=%2F%2Fevil.example.com");

        start.StatusCode.Should().Be(HttpStatusCode.Found);
        start.Headers.Location!.GetLeftPart(UriPartial.Path).Should().Be($"{_idp.Authority}/authorize");
        start.Headers.Location.Fragment.Should().BeEmpty();
    }

    [Fact]
    public async Task Login_ShouldSetSecureOidcCookies_OverHttps()
    {
        UseIdentity("secure");
        using var browser = _fixture.CreateBrowserClient(Https);
        using var idpBrowser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        var start = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/login");
        var idpResponse = await idpBrowser.GetAsync(start.Headers.Location!);
        var callback = await browser.GetAsync(idpResponse.Headers.Location!.PathAndQuery);

        start.Headers.GetValues("Set-Cookie").Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=", StringComparison.Ordinal))
            .Which.Should().ContainEquivalentOf("secure");
        callback.Headers.GetValues("Set-Cookie").Where(c => c.StartsWith("bgt_oidc_", StringComparison.Ordinal))
            .Should().HaveCount(3).And.OnlyContain(c => c.Contains("secure", StringComparison.OrdinalIgnoreCase));
        (await AdoptAsync(browser)).AccessToken.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("callback")]
    [InlineData("link-callback")]
    public async Task Callbacks_ShouldBeRefusedOverPlainHttp_OnANonLoopbackAddress(string callback)
    {
        using var browser = _fixture.CreateBrowserClient(LanHttp);

        var response = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/{callback}?code=code&state=state");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcHttpsRequired)}");
    }

    [Fact]
    public async Task Link_ShouldBeRefusedWithTheKey_OverPlainHttp_OnANonLoopbackAddress()
    {
        using var browser = _fixture.CreateBrowserClient(LanHttp);
        var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword));
        login.StatusCode.Should().Be(HttpStatusCode.OK, await login.Content.ReadAsStringAsync());
        var session = await login.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json);
        browser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        var start = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/link");

        start.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await start.Content.ReadAsStringAsync()).Should().Contain(Constants.Errors.OidcHttpsRequired);
    }

    [Fact]
    public async Task Provider_ShouldReportThatHttpsIsRequired_OnlyOverPlainHttpOnANonLoopbackAddress()
    {
        using var lan = _fixture.CreateBrowserClient(LanHttp);
        using var secure = _fixture.CreateBrowserClient(Https);
        using var loopback = _fixture.CreateBrowserClient();

        var overLan = await lan.GetFromJsonAsync<OidcProviderInfo>("/api/auth/oidc/provider", IntegrationFixture.Json);
        var overHttps = await secure.GetFromJsonAsync<OidcProviderInfo>("/api/auth/oidc/provider", IntegrationFixture.Json);
        var overLoopback = await loopback.GetFromJsonAsync<OidcProviderInfo>("/api/auth/oidc/provider", IntegrationFixture.Json);

        overLan!.HttpsRequired.Should().BeTrue();
        overHttps!.HttpsRequired.Should().BeFalse();
        overLoopback!.HttpsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task Admin_ShouldNotEnableTheProvider_OverPlainHttpWithoutAnHttpsPublicUrl()
    {
        using var token = await _fixture.CreateAdminClientAsync();
        using var admin = _fixture.CreateBrowserClient(LanHttp);
        admin.DefaultRequestHeaders.Authorization = token.DefaultRequestHeaders.Authorization;

        var setup = await admin.GetFromJsonAsync<OidcSetupDto>("/api/admin/oidc-providers/setup", IntegrationFixture.Json);
        var enable = await admin.PutAsJsonAsync($"/api/admin/oidc-providers/{_providerId}", new UpdateOidcProviderRequest(
            _providerId, "Fake IdP", _idp.Authority, _idp.ClientId, null, true, "openid profile email", true,
            null, null, null, null, null, null, null, null, null, null));
        var disable = await admin.PutAsJsonAsync($"/api/admin/oidc-providers/{_providerId}", new UpdateOidcProviderRequest(
            _providerId, "Fake IdP", _idp.Authority, _idp.ClientId, null, false, "openid profile email", true,
            null, null, null, null, null, null, null, null, null, null));
        var reenable = await token.PutAsJsonAsync($"/api/admin/oidc-providers/{_providerId}", new UpdateOidcProviderRequest(
            _providerId, "Fake IdP", _idp.Authority, _idp.ClientId, null, true, "openid profile email", true,
            null, null, null, null, null, null, null, null, null, null));

        setup!.HttpsRequired.Should().BeTrue();
        enable.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await enable.Content.ReadAsStringAsync()).Should().Contain(Constants.Errors.OidcHttpsRequired);
        disable.StatusCode.Should().Be(HttpStatusCode.OK, await disable.Content.ReadAsStringAsync());
        reenable.StatusCode.Should().Be(HttpStatusCode.OK, await reenable.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PasswordLogin_ShouldKeepWorking_OverPlainHttpOnANonLoopbackAddress()
    {
        using var browser = _fixture.CreateBrowserClient(LanHttp);

        var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword));

        login.StatusCode.Should().Be(HttpStatusCode.OK, await login.Content.ReadAsStringAsync());
        (await login.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json))!.AccessToken.Should().NotBeNullOrEmpty();
    }

    private void UseIdentity(string name)
    {
        var unique = $"{name}-{Guid.NewGuid():N}"[..20];
        _idp.User["sub"] = $"sub-{unique}";
        _idp.User["email"] = $"{unique}@example.com";
        _idp.User["preferred_username"] = unique;
        _idp.User["name"] = $"{name} user";
        _idp.User.Remove("groups");
    }

    private async Task ConfigureRoleMappingAsync()
    {
        using var admin = await _fixture.CreateAdminClientAsync();
        var update = await admin.PutAsJsonAsync($"/api/admin/oidc-providers/{_providerId}", new UpdateOidcProviderRequest(
            _providerId, "Fake IdP", _idp.Authority, _idp.ClientId, null, true, "openid profile email", true,
            null, null, null, null, null, null, "groups", "bgt-admins", null, null, Constants.AuthRoles.Reader));
        update.StatusCode.Should().Be(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
    }

    private static async Task<HttpResponseMessage> SignInAsync(HttpClient browser, string providerName = ProviderName)
    {
        using var idpBrowser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var start = await browser.GetAsync($"/api/auth/oidc/{providerName}/login");
        start.StatusCode.Should().Be(HttpStatusCode.Found);
        var idpResponse = await idpBrowser.GetAsync(start.Headers.Location!);
        var callback = await browser.GetAsync(idpResponse.Headers.Location!.PathAndQuery);
        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        return callback;
    }

    private static async Task<LoginResponse> AdoptAsync(HttpClient browser)
    {
        var adopt = await browser.PostAsync("/api/auth/oidc/adopt", null);
        adopt.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await adopt.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json))!;
    }

    private sealed class FixedPublicUrl(string baseUrl) : IPublicUrlBuilder
    {
        public Task<string> BuildRsvpUrlAsync(Guid linkId) => Task.FromResult($"{baseUrl}/rsvp/{linkId}");

        public Task<string> BuildResetUrlAsync(string userId, string token) => Task.FromResult($"{baseUrl}/reset-password");

        public Task<string?> GetConfiguredBaseUrlAsync() => Task.FromResult<string?>(baseUrl);
    }
}
