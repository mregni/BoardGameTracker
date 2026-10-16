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
        callback.Headers.Location!.ToString().Should().Be("/auth-callback?redirect=%2Fgames%2F3");
        callback.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith($"{OidcController.HandoffCookieName}=", StringComparison.Ordinal) && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        callback.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith($"{OidcController.SessionCookieName}=", StringComparison.Ordinal) && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        callbackUrl.ToString().Should().NotContain("id_token");

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
        using var browser = _fixture.CreateBrowserClient();
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
        using var browser = _fixture.CreateBrowserClient();

        var start = await browser.GetAsync("/api/auth/oidc/no-such-provider/login");

        start.StatusCode.Should().Be(HttpStatusCode.Found);
        start.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcProviderUnavailable)}");
    }

    [Fact]
    public async Task Login_ShouldSendARateLimitedBrowserToTheErrorPage()
    {
        using var browser = _fixture.CreateBrowserClient();
        for (var i = 0; i < 10; i++)
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
        using var browser = _fixture.CreateBrowserClient();

        var callback = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/callback?code=forged&state=forged");

        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.InvalidAuthSession)}");
    }

    [Fact]
    public async Task Callback_ShouldReportAProviderRefusal_AsAStableErrorKey()
    {
        using var browser = _fixture.CreateBrowserClient();
        await browser.GetAsync($"/api/auth/oidc/{ProviderName}/login");

        var callback = await browser.GetAsync($"/api/auth/oidc/{ProviderName}/callback?error=access_denied&state=whatever");

        callback.StatusCode.Should().Be(HttpStatusCode.Found);
        callback.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcProviderRejected)}");
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
        using var browser = _fixture.CreateBrowserClient();

        var adopt = await browser.PostAsync("/api/auth/oidc/adopt", null);

        adopt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Callback_ShouldRejectAnIdTokenThatIsNotSignedByTheProvider()
    {
        _idp.TamperIdToken = true;
        UseIdentity("tampered");
        using var browser = _fixture.CreateBrowserClient();

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
        using var browser = _fixture.CreateBrowserClient();

        var callback = await SignInAsync(browser);

        callback.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcIssuerMismatch)}");
    }

    [Fact]
    public async Task Logout_ShouldReturnTheProvidersSignOutUrl_WithTheIdTokenHint()
    {
        UseIdentity("logout");
        using var browser = _fixture.CreateBrowserClient();
        await SignInAsync(browser);
        var idToken = _idp.LastIdToken;

        var logout = await browser.PostAsync("/api/auth/oidc/logout", null);

        logout.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await logout.Content.ReadFromJsonAsync<OidcLogoutResponse>(IntegrationFixture.Json);
        var url = new Uri(body!.Url!);
        url.GetLeftPart(UriPartial.Path).Should().Be($"{_idp.Authority}/logout");
        var query = HttpUtility.ParseQueryString(url.Query);
        query["id_token_hint"].Should().Be(idToken);
        query["post_logout_redirect_uri"].Should().Be("http://localhost/login");
        query["client_id"].Should().Be(_idp.ClientId);

        var again = await browser.PostAsync("/api/auth/oidc/logout", null);
        (await again.Content.ReadFromJsonAsync<OidcLogoutResponse>(IntegrationFixture.Json))!.Url.Should().BeNull();
    }

    [Fact]
    public async Task Rename_ShouldKeepTheLinkedAccount()
    {
        UseIdentity("rename");
        LoginResponse first;
        using (var browser = _fixture.CreateBrowserClient())
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

        using var again = _fixture.CreateBrowserClient();
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

        using (var browser = _fixture.CreateBrowserClient())
        {
            await SignInAsync(browser);
            (await AdoptAsync(browser)).User.Roles.Should().BeEquivalentTo(Constants.AuthRoles.Admin);
        }

        _idp.User["groups"] = new[] { "staff" };
        using (var browser = _fixture.CreateBrowserClient())
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
        using var browser = _fixture.CreateBrowserClient();

        var callback = await SignInAsync(browser);

        callback.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcUsernameTaken)}");
    }

    [Fact]
    public async Task Admin_ShouldSeeAndUnlinkTheSsoIdentities_AndADisabledUserCannotSignInWithSso()
    {
        UseIdentity("deprovision");
        LoginResponse login;
        using (var browser = _fixture.CreateBrowserClient())
        {
            await SignInAsync(browser);
            login = await AdoptAsync(browser);
        }

        using var admin = await _fixture.CreateAdminClientAsync();
        var users = await admin.GetFromJsonAsync<List<UserDto>>("/api/admin/users", IntegrationFixture.Json);
        var listed = users!.Single(u => u.Id == login.User.Id);
        listed.ExternalLoginCount.Should().Be(1);
        listed.RoleManagedByIdentityProvider.Should().BeTrue();

        var disable = await admin.PostAsync($"/api/admin/users/{login.User.Id}/disable", null);
        disable.StatusCode.Should().Be(HttpStatusCode.OK, await disable.Content.ReadAsStringAsync());
        (await disable.Content.ReadFromJsonAsync<UserDto>(IntegrationFixture.Json))!.IsDisabled.Should().BeTrue();

        using (var browser = _fixture.CreateBrowserClient())
        {
            var refused = await SignInAsync(browser);
            refused.Headers.Location!.ToString().Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.AccountDisabled)}");
        }

        using var anonymous = _fixture.CreateClient();
        var refresh = await anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken));
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

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
}
