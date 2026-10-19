using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcFlowTests : IDisposable
{
    private const string Authority = "https://idp.example.com";
    private const string PublicBase = "https://games.example.com";

    private readonly MainDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly MemoryCache _cache;
    private readonly FakeIdentityProviderHandler _idp;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly SecretProtector _secretProtector;
    private readonly OidcService _service;

    public OidcFlowTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(["User"]);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock = new Mock<ITokenService>();
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>())).Returns("access-token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync((string userId) => RefreshToken.Create(userId, 7));
        _tokenServiceMock.Setup(x => x.GetAccessTokenExpiry()).Returns(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        _idp = new FakeIdentityProviderHandler(Authority);
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(x => x.CreateClient(OidcService.HttpClientName)).Returns(() => new HttpClient(_idp, disposeHandler: false));

        _cache = new MemoryCache(new MemoryCacheOptions());
        _secretProtector = new SecretProtector(new EphemeralDataProtectionProvider(), Mock.Of<ILogger<SecretProtector>>());
        _service = CreateService(_cache);

        _context.OidcProviders.Add(new OidcProvider("idp", "Example IdP", Authority, "bgt-client"));
        _context.SaveChanges();
    }

    private OidcService CreateService(IMemoryCache cache)
    {
        var configurations = new OidcConfigurationProvider(_httpClientFactoryMock.Object, Mock.Of<ILogger<OidcConfigurationProvider>>());
        return new OidcService(
            _context,
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _httpClientFactoryMock.Object,
            cache,
            _secretProtector,
            configurations,
            new OidcIdTokenValidator(configurations, Mock.Of<ILogger<OidcIdTokenValidator>>()),
            new OidcRoleReconciler(_userManagerMock.Object, Mock.Of<ILogger<OidcRoleReconciler>>()),
            Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 }),
            Mock.Of<ILogger<OidcService>>());
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
        _idp.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task StartLoginAsync_ShouldBuildAPkceRequest_WithTheServerDerivedRedirectUri()
    {
        var request = await _service.StartLoginAsync("idp", PublicBase + "/", "/games/3");

        var uri = new Uri(request.Url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        uri.GetLeftPart(UriPartial.Path).Should().Be($"{Authority}/authorize");
        query["client_id"].Should().Be("bgt-client");
        query["response_type"].Should().Be("code");
        query["scope"].Should().Be("openid profile email");
        query["redirect_uri"].Should().Be($"{PublicBase}/api/auth/oidc/idp/callback");
        query["code_challenge_method"].Should().Be("S256");
        query["code_challenge"].Should().NotBeNullOrEmpty();
        query["nonce"].Should().NotBeNullOrEmpty();
        query["state"].Should().Be(request.State);
        request.Url.Should().NotContain("/games/3");
    }

    [Fact]
    public async Task StartLoginAsync_ShouldAlwaysRequestTheOpenIdScope()
    {
        await UpdateProviderAsync(p => p.Update(p.DisplayName, p.Authority, p.ClientId, null, true, "profile  email", true,
            null, null, null, null, null, null, null, null, null, null));

        var request = await _service.StartLoginAsync("idp", PublicBase, null);

        HttpUtility.ParseQueryString(new Uri(request.Url).Query)["scope"].Should().Be("openid profile email");
    }

    [Theory]
    [InlineData("//evil.example.com")]
    [InlineData("/\\evil.example.com")]
    [InlineData("https://evil.example.com")]
    [InlineData("games")]
    [InlineData(null)]
    public async Task CompleteLoginAsync_ShouldFallBackToTheRoot_WhenTheRedirectPathIsNotLocal(string? redirect)
    {
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = await _service.StartLoginAsync("idp", PublicBase, redirect);
        var result = await _service.CompleteLoginAsync("idp", await _idp.AuthorizeAsync(request.Url), request.State, request.State, Authority);

        result.RedirectPath.Should().Be("/");
    }

    [Theory]
    [InlineData("https://evil.example.com@games.example.com")]
    [InlineData("https://games.example.com#@evil.example.com")]
    [InlineData("https://games.example.com/?next=https://evil.example.com")]
    [InlineData("https://evil.example.com")]
    public async Task StartLoginAsync_ShouldOnlyEverRedirectToTheProvidersAuthorizationEndpoint(string publicBaseUrl)
    {
        var request = await _service.StartLoginAsync("idp", publicBaseUrl, "//evil.example.com");

        var uri = new Uri(request.Url);
        uri.GetLeftPart(UriPartial.Path).Should().Be($"{Authority}/authorize");
        uri.Fragment.Should().BeEmpty();
        HttpUtility.ParseQueryString(uri.Query)["redirect_uri"].Should().Be($"{publicBaseUrl}/api/auth/oidc/idp/callback");
    }

    [Fact]
    public async Task StartLoginAsync_ShouldKeepTheQueryOfTheAuthorizationEndpoint_AndDropItsFragment()
    {
        _idp.AuthorizationEndpoint = $"{Authority}/authorize?tenant=games#ignored";

        var request = await _service.StartLoginAsync("idp", PublicBase, null);

        var uri = new Uri(request.Url);
        uri.GetLeftPart(UriPartial.Path).Should().Be($"{Authority}/authorize");
        uri.Fragment.Should().BeEmpty();
        var query = HttpUtility.ParseQueryString(uri.Query);
        query["tenant"].Should().Be("games");
        query["client_id"].Should().Be("bgt-client");
        query["state"].Should().Be(request.State);
    }

    [Fact]
    public async Task StartLoginAsync_ShouldRejectAnInsecureAuthorizationEndpoint()
    {
        _idp.AuthorizationEndpoint = "http://idp.example.com/authorize";

        var act = () => _service.StartLoginAsync("idp", PublicBase, null);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InsecureAuthority);
    }

    [Fact]
    public async Task StartLoginAsync_ShouldRejectADiscoveryDocument_WhoseIssuerDiffersFromTheAuthority()
    {
        _idp.Issuer = "https://other.example.com";

        var act = () => _service.StartLoginAsync("idp", PublicBase, null);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIssuerMismatch);
    }

    [Fact]
    public async Task StartLoginAsync_ShouldReportAnUnreachableDiscoveryDocument()
    {
        _idp.DiscoveryStatus = HttpStatusCode.InternalServerError;

        var act = () => _service.StartLoginAsync("idp", PublicBase, null);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcDiscoveryFailed);
    }

    [Fact]
    public async Task StartLoginAsync_ShouldThrow_WhenTheProviderIsUnknownOrDisabled()
    {
        var act = () => _service.StartLoginAsync("nope", PublicBase, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldLogInTheLinkedUser_AndExchangeTheCodeWithPkce()
    {
        var user = LinkedUser();
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = await _service.StartLoginAsync("idp", PublicBase, "/players");
        var code = await _idp.AuthorizeAsync(request.Url);
        var result = await _service.CompleteLoginAsync("idp", code, request.State, request.State, Authority);

        result.RedirectPath.Should().Be("/players");
        result.Login.AccessToken.Should().Be("access-token");
        result.Login.User.Username.Should().Be(user.UserName);
        _idp.TokenRequest!["redirect_uri"].Should().Be($"{PublicBase}/api/auth/oidc/idp/callback");
        _idp.TokenRequest["code_verifier"].Should().NotBeNullOrEmpty();
        _idp.TokenRequest["grant_type"].Should().Be("authorization_code");
        _idp.TokenRequest["client_id"].Should().Be("bgt-client");
        _idp.TokenRequest.Should().NotContainKey("client_secret");
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).LastUsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldSendTheDecryptedClientSecret_ForAConfidentialClient()
    {
        await UpdateProviderAsync(p => p.Update(p.DisplayName, p.Authority, p.ClientId, _secretProtector.Protect("client-s3cret"), p.Enabled, p.Scopes,
            p.AutoProvisionUsers, null, null, null, null, null, null, null, null, null, null));
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await LoginAsync();

        _idp.TokenRequest!["client_id"].Should().Be("bgt-client");
        _idp.TokenRequest["client_secret"].Should().Be("client-s3cret");
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_AnIdTokenWithATamperedSignature()
    {
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var attacker = new TestIdTokens(_idp.Keys.KeyId);
        _idp.Signer = attacker;

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()), Times.Never);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_AnIdTokenWithAnotherNonce()
    {
        _idp.User = KnownUser("sub-1");
        _idp.NonceOverride = "replayed-nonce";

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_ATokenResponseWithoutAnIdToken()
    {
        _idp.User = KnownUser("sub-1");
        _idp.OmitIdToken = true;

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_UserInfoForAnotherSubject()
    {
        _idp.User = KnownUser("sub-1");
        _idp.UserInfo = KnownUser("sub-2");

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRejectAnIssParameterFromAnotherIssuer_BeforeExchangingTheCode()
    {
        _idp.User = KnownUser("sub-1");

        var act = () => LoginAsync(issuer: "https://evil.example.com");

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIssuerMismatch);
        _idp.TokenRequest.Should().BeNull();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRejectAMissingIssParameter_WhenTheProviderAdvertisesIt()
    {
        _idp.User = KnownUser("sub-1");
        _idp.AdvertisesIssParameter = true;

        var act = () => LoginAsync(issuer: null);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIssuerMismatch);
        _idp.TokenRequest.Should().BeNull();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldAcceptAMissingIssParameter_WhenTheProviderDoesNotAdvertiseIt()
    {
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await LoginAsync(issuer: null);

        result.Login.User.Username.Should().Be("jane-linked");
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldUseTheIdTokenClaims_WhenTheProviderHasNoUserInfoEndpoint()
    {
        _idp.HasUserInfo = false;
        _idp.User = KnownUser("sub-new");
        SetUpProvisioning();

        var result = await LoginAsync();

        result.Login.User.Username.Should().Be("jane");
        _idp.UserInfoCalls.Should().Be(0);
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.UserName == "jane" && u.Email == "jane@example.com" && u.DisplayName == "Jane Doe")), Times.Once);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldPreferUserInfoClaims_ButKeepTheIdTokenSubject()
    {
        _idp.User = KnownUser("sub-new");
        _idp.UserInfo = KnownUser("sub-new");
        _idp.UserInfo["name"] = "Jane From UserInfo";
        SetUpProvisioning();

        await LoginAsync();

        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.DisplayName == "Jane From UserInfo")), Times.Once);
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).ProviderKey.Should().Be("sub-new");
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldProvisionANewUser_KeyedByTheIssuer()
    {
        _idp.User = KnownUser("sub-new");
        SetUpProvisioning();

        var result = await LoginAsync();

        result.Login.User.Username.Should().Be("jane");
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.UserName == "jane" && u.Email == "jane@example.com")), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.RoleManagedByIdentityProvider), "User"), Times.Once);
        var login = await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken);
        login.ProviderKey.Should().Be("sub-new");
        login.Issuer.Should().Be(Authority);
        login.Provider.Should().Be("idp");
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldProvisionWithTheConfiguredDefaultRole()
    {
        await UpdateProviderAsync(p => p.SetDefaultRole("Reader"));
        _idp.User = KnownUser("sub-new");
        SetUpProvisioning();

        await LoginAsync();

        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Reader"), Times.Once);
    }

    [Theory]
    [InlineData("Domain Admins", false)]
    [InlineData("Admins", true)]
    [InlineData("Users, Admins", true)]
    [InlineData("bgt-Admins", false)]
    public async Task CompleteLoginAsync_ShouldGrantAdmin_OnlyForAnExactGroupInAScalarClaim(string groups, bool expectAdmin)
    {
        await ProvisionWithGroups(groups);

        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), expectAdmin ? "Admin" : "User"), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), expectAdmin ? "User" : "Admin"), Times.Never);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldGrantAdmin_OnlyForAnExactGroupInAnArrayClaim()
    {
        await ProvisionWithGroups(new[] { "Domain Admins", "Staff" });

        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"), Times.Never);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReconcileTheRoleOfAnIdentityProviderManagedUser_OnEveryLogin()
    {
        await ConfigureAdminGroupAsync();
        var user = LinkedUser();
        user.ManageRoleByIdentityProvider();
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = KnownUser("sub-1");
        _idp.User["groups"] = new[] { "Admins" };
        _userManagerMock.Setup(x => x.AddToRoleAsync(user, "Admin")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);

        await LoginAsync();

        _userManagerMock.Verify(x => x.AddToRoleAsync(user, "Admin"), Times.Once);
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { "User" }))), Times.Once);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldNotTouchTheRoleOfALocallyManagedUser()
    {
        await ConfigureAdminGroupAsync();
        var user = LinkedUser();
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = KnownUser("sub-1");
        _idp.User["groups"] = new[] { "Admins" };

        await LoginAsync();

        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldFindTheLinkByIssuer_AfterTheProviderWasRenamed()
    {
        var user = LinkedUser();
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "old-name", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = KnownUser("sub-1");

        var result = await LoginAsync();

        result.Login.User.Username.Should().Be(user.UserName);
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).Provider.Should().Be("idp");
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldNotReuseALinkOfAnotherIssuer_ThatHasTheSameSubject()
    {
        var victim = LinkedUser();
        _context.ExternalLogins.Add(new ExternalLogin(victim.Id, "idp", "https://previous-idp.example.com", "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = KnownUser("sub-1");
        SetUpProvisioning();

        var result = await LoginAsync();

        result.Login.User.Username.Should().Be("jane");
        result.Login.User.Id.Should().NotBe(victim.Id);
        (await _context.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRekeyALegacyLink_OnTheIssuerReportedByDiscovery()
    {
        await UpdateProviderAsync(p => p.Update(p.DisplayName, "https://IDP.example.com/", p.ClientId, null, true, p.Scopes, true,
            null, null, null, null, null, null, null, null, null, null));
        var user = LinkedUser();
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", "https://IDP.example.com", "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = KnownUser("sub-1");

        var result = await LoginAsync();

        result.Login.User.Id.Should().Be(user.Id);
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).Issuer.Should().Be(Authority);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldNeverMatchALegacyLinkWithoutAnIssuer()
    {
        var victim = LinkedUser();
        _context.ExternalLogins.Add(new ExternalLogin(victim.Id, "idp", null, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = KnownUser("sub-1");
        SetUpProvisioning();

        var result = await LoginAsync();

        result.Login.User.Id.Should().NotBe(victim.Id);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRefuseADisabledUser()
    {
        var user = LinkedUser();
        user.LockoutEnd = DateTimeOffset.MaxValue;
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = KnownUser("sub-1");

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.AccountDisabled);
        _tokenServiceMock.Verify(x => x.GenerateRefreshTokenAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRefuseToProvision_WhenAnUnlinkedLocalUserHasTheSameUsername()
    {
        _idp.User = KnownUser("sub-new");
        SetUpProvisioning();
        _userManagerMock.Setup(x => x.FindByNameAsync("jane")).ReturnsAsync(new ApplicationUser("jane", "other@example.com"));

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcUsernameTaken);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        (await _context.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRefuseToProvision_WhenALocalUserHasTheSameEmail()
    {
        _idp.User = KnownUser("sub-new");
        _context.Users.Add(new ApplicationUser("local-jane", "jane@example.com") { NormalizedEmail = "JANE@EXAMPLE.COM" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _userManagerMock.Setup(x => x.NormalizeEmail(It.IsAny<string>())).Returns((string e) => e.ToUpperInvariant());
        _userManagerMock.Setup(x => x.Users).Returns(_context.Users);

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcEmailAlreadyRegistered);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRefuse_WhenProvisioningIsDisabled()
    {
        await UpdateProviderAsync(p => p.Update(p.DisplayName, p.Authority, p.ClientId, null, true, p.Scopes, false,
            null, null, null, null, null, null, null, null, null, null));
        _idp.User = KnownUser("sub-new");

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcProvisioningDisabled);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_WhenTheBrowserStateCookieIsMissing()
    {
        var request = await _service.StartLoginAsync("idp", PublicBase, null);

        var act = () => _service.CompleteLoginAsync("idp", "code", request.State, null, Authority);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
        _idp.TokenRequest.Should().BeNull();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_WhenTheBrowserStateCookieDoesNotMatch()
    {
        var request = await _service.StartLoginAsync("idp", PublicBase, null);

        var act = () => _service.CompleteLoginAsync("idp", "code", request.State, "someone-elses-state", Authority);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_AnUnknownOrReusedState()
    {
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var request = await _service.StartLoginAsync("idp", PublicBase, null);
        var code = await _idp.AuthorizeAsync(request.Url);
        await _service.CompleteLoginAsync("idp", code, request.State, request.State, Authority);

        var replay = () => _service.CompleteLoginAsync("idp", code, request.State, request.State, Authority);
        var unknown = () => _service.CompleteLoginAsync("idp", code, "unknown", "unknown", Authority);

        await replay.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
        await unknown.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_AStateStartedForTheLinkFlow()
    {
        var request = await _service.StartLinkAsync("user-1", "idp", PublicBase);

        var act = () => _service.CompleteLoginAsync("idp", "code", request.State, request.State, Authority);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldSurfaceARejectedTokenExchange()
    {
        _idp.TokenStatus = HttpStatusCode.BadRequest;

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcExchangeFailed);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldSurfaceAFailingUserInfoEndpoint()
    {
        _idp.User = KnownUser("sub-1");
        _idp.UserInfoStatus = HttpStatusCode.InternalServerError;

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcUserInfoFailed);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldStartAnOidcSession_WhenTheProviderSupportsSignOut()
    {
        _idp.User = KnownUser("sub-1");
        var user = LinkedUser();
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await LoginAsync();

        result.SessionKey.Should().NotBeNullOrEmpty();
        var session = await _context.OidcSessions.SingleAsync(TestContext.Current.CancellationToken);
        session.UserId.Should().Be(user.Id);
        session.KeyHash.Should().Be(OidcService.HashKey(result.SessionKey!));
        session.IdToken.Should().NotBe(_idp.LastIdToken);
        _secretProtector.Unprotect(session.IdToken).Should().Be(_idp.LastIdToken);
        session.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldNotStartAnOidcSession_WhenTheProviderHasNoSignOutEndpoint()
    {
        _idp.HasEndSession = false;
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await LoginAsync();

        result.SessionKey.Should().BeNull();
        (await _context.OidcSessions.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task EndSessionAsync_ShouldBuildTheProvidersSignOutUrl_OnlyOnce()
    {
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var login = await LoginAsync();

        var url = await _service.EndSessionAsync(login.SessionKey, PublicBase + "/");
        var again = await _service.EndSessionAsync(login.SessionKey, PublicBase);

        var uri = new Uri(url!);
        uri.GetLeftPart(UriPartial.Path).Should().Be($"{Authority}/logout");
        var query = HttpUtility.ParseQueryString(uri.Query);
        query["id_token_hint"].Should().Be(_idp.LastIdToken);
        query["post_logout_redirect_uri"].Should().Be($"{PublicBase}/login");
        query["client_id"].Should().Be("bgt-client");
        again.Should().BeNull();
        (await _context.OidcSessions.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task CompleteLinkAsync_ShouldLinkTheUserFromTheStateRecord_KeyedByTheIssuer()
    {
        var user = LinkedUser();
        _idp.User = KnownUser("sub-9");
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var request = await _service.StartLinkAsync(user.Id, "idp", PublicBase);
        var uri = new Uri(request.Url);
        HttpUtility.ParseQueryString(uri.Query)["redirect_uri"].Should().Be($"{PublicBase}/api/auth/oidc/idp/link-callback");

        var result = await _service.CompleteLinkAsync("idp", await _idp.AuthorizeAsync(request.Url), request.State, request.State, Authority);

        result.UserId.Should().Be(user.Id);
        result.ProviderName.Should().Be("idp");
        var login = await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken);
        login.UserId.Should().Be(user.Id);
        login.ProviderKey.Should().Be("sub-9");
        login.Issuer.Should().Be(Authority);
    }

    [Fact]
    public async Task CompleteLinkAsync_ShouldRefuse_WhenTheExternalAccountIsAlreadyLinked()
    {
        var user = LinkedUser();
        _idp.User = KnownUser("sub-9");
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        var someoneElse = new ApplicationUser("someone-else", "else@example.com");
        _context.Users.Add(someoneElse);
        _context.ExternalLogins.Add(new ExternalLogin(someoneElse.Id, "renamed", Authority, "sub-9"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = await _service.StartLinkAsync(user.Id, "idp", PublicBase);
        var code = await _idp.AuthorizeAsync(request.Url);
        var act = () => _service.CompleteLinkAsync("idp", code, request.State, request.State, Authority);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcAlreadyLinked);
    }

    [Fact]
    public async Task CompleteLinkAsync_ShouldReject_ATamperedIdToken()
    {
        var user = LinkedUser();
        _idp.User = KnownUser("sub-9");
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        using var attacker = new TestIdTokens(_idp.Keys.KeyId);
        _idp.Signer = attacker;

        var request = await _service.StartLinkAsync(user.Id, "idp", PublicBase);
        var code = await _idp.AuthorizeAsync(request.Url);
        var act = () => _service.CompleteLinkAsync("idp", code, request.State, request.State, Authority);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        (await _context.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task TestDiscoveryAsync_ShouldReturnTheParsedEndpoints_AndWhetherTheIssuerMatches()
    {
        var result = await _service.TestDiscoveryAsync(Authority + "/");

        result.Issuer.Should().Be(Authority);
        result.AuthorizationEndpoint.Should().Be($"{Authority}/authorize");
        result.TokenEndpoint.Should().Be($"{Authority}/token");
        result.UserInfoEndpoint.Should().Be($"{Authority}/userinfo");
        result.EndSessionEndpoint.Should().Be($"{Authority}/logout");
        result.IssuerMatchesAuthority.Should().BeTrue();
    }

    [Fact]
    public async Task TestDiscoveryAsync_ShouldReportMissingOptionalEndpointsAsNull()
    {
        _idp.HasUserInfo = false;
        _idp.HasEndSession = false;

        var result = await _service.TestDiscoveryAsync(Authority);

        result.UserInfoEndpoint.Should().BeNull();
        result.EndSessionEndpoint.Should().BeNull();
    }

    [Fact]
    public async Task TestDiscoveryAsync_ShouldFlagAnIssuerMismatch_InsteadOfThrowing()
    {
        _idp.Issuer = "https://other.example.com";

        var result = await _service.TestDiscoveryAsync(Authority);

        result.Issuer.Should().Be("https://other.example.com");
        result.IssuerMatchesAuthority.Should().BeFalse();
    }

    [Fact]
    public async Task TestDiscoveryAsync_ShouldRejectAnInsecureAuthority_WithoutCallingIt()
    {
        var act = () => _service.TestDiscoveryAsync("http://idp.example.com");

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InsecureAuthority);
    }

    [Fact]
    public async Task TestDiscoveryAsync_ShouldReportAnUnreachableProvider()
    {
        var act = () => _service.TestDiscoveryAsync("https://nowhere.example.com");

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcDiscoveryFailed);
    }

    [Fact]
    public void Handoff_ShouldBeSingleUse()
    {
        var login = new LoginResponse("a", "r", DateTime.UtcNow, new UserInfo("1", "jane", null, ["User"]));

        var key = _service.CreateHandoff(login);

        _service.TakeHandoff(key).Should().Be(login);
        _service.TakeHandoff(key).Should().BeNull();
        _service.TakeHandoff(null).Should().BeNull();
        _service.TakeHandoff("unknown").Should().BeNull();
    }

    [Fact]
    public async Task Handoff_ShouldBeSingleUse_WhenTwoRequestsTakeItAtTheSameTime()
    {
        using var lockstep = new LockstepCache(_cache);
        var service = CreateService(lockstep);
        var login = new LoginResponse("a", "r", DateTime.UtcNow, new UserInfo("1", "jane", null, ["User"]));
        var key = service.CreateHandoff(login);
        lockstep.Arm(readers: 2);

        var results = await Task.WhenAll(
            Task.Run(() => service.TakeHandoff(key), TestContext.Current.CancellationToken),
            Task.Run(() => service.TakeHandoff(key), TestContext.Current.CancellationToken));

        results.Should().ContainSingle(x => x != null).Which.Should().Be(login);
    }

    private async Task<OidcLoginResult> LoginAsync(string? issuer = Authority)
    {
        var request = await _service.StartLoginAsync("idp", PublicBase, null);
        var code = await _idp.AuthorizeAsync(request.Url);
        return await _service.CompleteLoginAsync("idp", code, request.State, request.State, issuer);
    }

    private async Task UpdateProviderAsync(Action<OidcProvider> update)
    {
        var provider = await _context.OidcProviders.SingleAsync(TestContext.Current.CancellationToken);
        update(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private Task ConfigureAdminGroupAsync() =>
        UpdateProviderAsync(p => p.Update(p.DisplayName, p.Authority, p.ClientId, null, true, p.Scopes, true,
            null, null, null, null, null, null, "groups", "Admins", null, null));

    private void SetUpProvisioning()
    {
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.NormalizeEmail(It.IsAny<string>())).Returns((string e) => e.ToUpperInvariant());
        _userManagerMock.Setup(x => x.Users).Returns(_context.Users);
    }

    private async Task ProvisionWithGroups(object groups)
    {
        await ConfigureAdminGroupAsync();
        var user = KnownUser("sub-" + Guid.NewGuid().ToString("N"));
        user["groups"] = groups;
        _idp.User = user;
        SetUpProvisioning();

        await LoginAsync();
    }

    private ApplicationUser LinkedUser()
    {
        var user = new ApplicationUser("jane-linked", "linked@example.com", "Jane");
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    private static Dictionary<string, object> KnownUser(string sub) => new()
    {
        ["sub"] = sub,
        ["email"] = "jane@example.com",
        ["preferred_username"] = "jane",
        ["name"] = "Jane Doe",
    };

    private sealed class FakeIdentityProviderHandler : HttpMessageHandler
    {
        private readonly string _authority;
        private readonly Dictionary<string, (string Challenge, string Nonce)> _codes = new();

        public FakeIdentityProviderHandler(string authority)
        {
            _authority = authority;
            Issuer = authority;
            AuthorizationEndpoint = $"{authority}/authorize";
            Signer = Keys;
        }

        public TestIdTokens Keys { get; } = new();
        public TestIdTokens Signer { get; set; }
        public string Issuer { get; set; }
        public string AuthorizationEndpoint { get; set; }
        public HttpStatusCode DiscoveryStatus { get; set; } = HttpStatusCode.OK;
        public HttpStatusCode TokenStatus { get; set; } = HttpStatusCode.OK;
        public HttpStatusCode UserInfoStatus { get; set; } = HttpStatusCode.OK;
        public bool HasUserInfo { get; set; } = true;
        public bool HasEndSession { get; set; } = true;
        public bool AdvertisesIssParameter { get; set; }
        public bool OmitIdToken { get; set; }
        public string? NonceOverride { get; set; }
        public Dictionary<string, object> User { get; set; } = new();
        public Dictionary<string, object>? UserInfo { get; set; }
        public Dictionary<string, string>? TokenRequest { get; private set; }
        public string? LastIdToken { get; private set; }
        public int UserInfoCalls { get; private set; }

        public Task<string> AuthorizeAsync(string authorizationUrl)
        {
            var query = HttpUtility.ParseQueryString(new Uri(authorizationUrl).Query);
            var code = Guid.NewGuid().ToString("N");
            _codes[code] = (query["code_challenge"]!, query["nonce"]!);
            return Task.FromResult(code);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Keys.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!string.Equals(request.RequestUri!.GetLeftPart(UriPartial.Authority), _authority, StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpRequestException($"No route to {request.RequestUri.Host}");
            }

            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/.well-known/openid-configuration", StringComparison.Ordinal))
            {
                if (DiscoveryStatus != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(DiscoveryStatus);
                }

                var document = new Dictionary<string, object>
                {
                    ["issuer"] = Issuer,
                    ["authorization_endpoint"] = AuthorizationEndpoint,
                    ["token_endpoint"] = $"{_authority}/token",
                    ["jwks_uri"] = $"{_authority}/certs",
                    ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
                    ["authorization_response_iss_parameter_supported"] = AdvertisesIssParameter,
                };
                if (HasUserInfo)
                {
                    document["userinfo_endpoint"] = $"{_authority}/userinfo";
                }

                if (HasEndSession)
                {
                    document["end_session_endpoint"] = $"{_authority}/logout";
                }

                return Json(document);
            }

            if (path.EndsWith("/certs", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Keys.JwksJson, Encoding.UTF8, "application/json") };
            }

            if (path.EndsWith("/token", StringComparison.Ordinal))
            {
                var form = HttpUtility.ParseQueryString(await request.Content!.ReadAsStringAsync(cancellationToken));
                TokenRequest = form.AllKeys.ToDictionary(k => k!, k => form[k]!);
                var issued = _codes.GetValueOrDefault(TokenRequest["code"]);
                var actualChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(TokenRequest["code_verifier"]))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                if (TokenRequest.GetValueOrDefault("client_id") != "bgt-client")
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("{\"error\":\"invalid_client\"}") };
                }

                if (TokenStatus != HttpStatusCode.OK || issued.Challenge != actualChallenge)
                {
                    return new HttpResponseMessage(TokenStatus == HttpStatusCode.OK ? HttpStatusCode.BadRequest : TokenStatus) { Content = new StringContent("{\"error\":\"invalid_grant\"}") };
                }

                var claims = TestIdTokens.Claims(Issuer, "bgt-client", User["sub"].ToString()!, NonceOverride ?? issued.Nonce);
                foreach (var (name, value) in User.Where(c => c.Key != "sub"))
                {
                    claims[name] = value;
                }

                LastIdToken = Signer.Sign(claims);
                return OmitIdToken
                    ? Json(new { access_token = "idp-access-token", token_type = "Bearer" })
                    : Json(new { access_token = "idp-access-token", token_type = "Bearer", id_token = LastIdToken });
            }

            if (path.EndsWith("/userinfo", StringComparison.Ordinal))
            {
                UserInfoCalls++;
                if (UserInfoStatus != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(UserInfoStatus);
                }

                return request.Headers.Authorization?.Parameter == "idp-access-token"
                    ? Json(UserInfo ?? User)
                    : new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(object body) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
    }

    private sealed class LockstepCache(IMemoryCache inner) : IMemoryCache
    {
        private Barrier? _barrier;

        public void Arm(int readers) => _barrier = new Barrier(readers);

        public bool TryGetValue(object key, out object? value)
        {
            var found = inner.TryGetValue(key, out value);
            _barrier?.SignalAndWait(TimeSpan.FromSeconds(5));
            return found;
        }

        public ICacheEntry CreateEntry(object key) => inner.CreateEntry(key);

        public void Remove(object key) => inner.Remove(key);

        public void Dispose() => _barrier?.Dispose();
    }
}
