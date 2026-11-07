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
using BoardGameTracker.Core.Auth.Pictures;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcFlowTests : IDisposable
{
    private const string Authority = "https://idp.example.com";
    private const string PublicBase = "https://games.example.com";

    private readonly MainDbContext _context;
    private const string LinkedUserId = "user-linked";
    private const string SsoUserId = "user-sso";

    private readonly Mock<IUserStore<ApplicationUser>> _userStoreMock = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<OidcService>> _loggerMock = new();
    private ApplicationUser? _createdUser;
    private readonly MemoryCache _cache;
    private readonly FakeIdentityProviderHandler _idp;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly SecretProtector _secretProtector;
    private readonly OidcService _service;
    private readonly IdentityProviderPictureQueue _pictureQueue = new();

    public OidcFlowTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);

        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            _userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _ = _userManagerMock.Object;
        _userManagerMock.Invocations.Clear();

        _tokenServiceMock = new Mock<ITokenService>();

        _idp = new FakeIdentityProviderHandler(Authority);
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(x => x.CreateClient(OidcService.HttpClientName)).Returns(() => new HttpClient(_idp, disposeHandler: false));

        _cache = new MemoryCache(new MemoryCacheOptions());
        _secretProtector = new SecretProtector(new EphemeralDataProtectionProvider(), NullLogger<SecretProtector>.Instance);
        _service = CreateService(_cache);

        _context.OidcProviders.Add(new OidcProvider("Example IdP", Authority, "bgt-client"));
        _context.SaveChanges();
    }

    private OidcService CreateService(IMemoryCache cache)
    {
        var configurations = new OidcConfigurationProvider(_httpClientFactoryMock.Object, NullLogger<OidcConfigurationProvider>.Instance);
        return new OidcService(
            _context,
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _httpClientFactoryMock.Object,
            cache,
            _secretProtector,
            configurations,
            new OidcIdTokenValidator(configurations, NullLogger<OidcIdTokenValidator>.Instance),
            new OidcRoleReconciler(_userManagerMock.Object, NullLogger<OidcRoleReconciler>.Instance),
            _pictureQueue,
            Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 }),
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _userStoreMock.VerifyNoOtherCalls();
        _httpClientFactoryMock.VerifyNoOtherCalls();
        _userManagerMock.VerifyNoOtherCalls();
        _tokenServiceMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    private void SetUpSignIn(string userName)
    {
        _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == userName))).ReturnsAsync(["User"]);
        _userManagerMock.Setup(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.UserName == userName))).ReturnsAsync(IdentityResult.Success);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.Is<ApplicationUser>(u => u.UserName == userName), It.Is<IList<string>>(r => r.SequenceEqual(new[] { "User" })), It.Is<Guid>(g => g != Guid.Empty))).Returns("access-token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync((string userId) => RefreshToken.Create(userId, 7));
        _tokenServiceMock.Setup(x => x.GetAccessTokenExpiry()).Returns(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private void VerifySignedIn(string userName, string userId, int times = 1, int roleReads = 1)
    {
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.UserName == userName && u.LastLoginAt != null)), Times.Exactly(times));
        _userManagerMock.Verify(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == userName)), Times.Exactly(roleReads));
        _tokenServiceMock.Verify(x => x.GenerateRefreshTokenAsync(userId), Times.Exactly(times));
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(It.Is<ApplicationUser>(u => u.UserName == userName), It.Is<IList<string>>(r => r.SequenceEqual(new[] { "User" })), It.Is<Guid>(g => g != Guid.Empty)), Times.Exactly(times));
        _tokenServiceMock.Verify(x => x.GetAccessTokenExpiry(), Times.Exactly(times));
    }

    private void VerifyProvisioned(string userName, string role)
    {
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.UserName == userName)), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == userName), role), Times.Once);
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
        var request = await _service.StartLoginAsync(PublicBase + "/", "/games/3");

        var uri = new Uri(request.Url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        uri.GetLeftPart(UriPartial.Path).Should().Be($"{Authority}/authorize");
        query["client_id"].Should().Be("bgt-client");
        query["response_type"].Should().Be("code");
        query["scope"].Should().Be("openid profile email");
        query["redirect_uri"].Should().Be($"{PublicBase}/api/auth/oidc/callback");
        query["code_challenge_method"].Should().Be("S256");
        query["code_challenge"].Should().NotBeNullOrEmpty();
        query["nonce"].Should().NotBeNullOrEmpty();
        query["state"].Should().Be(request.State);
        request.Url.Should().NotContain("/games/3");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartLoginAsync_ShouldAlwaysRequestTheOpenIdScope()
    {
        await UpdateProviderAsync(p => p.Update(p.DisplayName, p.Authority, p.ClientId, null, true, "profile  email", true,
            null, null, null, null, null, null, null, null, null, null));

        var request = await _service.StartLoginAsync(PublicBase, null);

        HttpUtility.ParseQueryString(new Uri(request.Url).Query)["scope"].Should().Be("openid profile email");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        VerifyNoOtherCalls();
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

        var request = await _service.StartLoginAsync(PublicBase, redirect);
        var result = await _service.CompleteLoginAsync(await _idp.AuthorizeAsync(request.Url), request.State, request.State, Authority);

        result.RedirectPath.Should().Be("/");
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("https://evil.example.com@games.example.com")]
    [InlineData("https://games.example.com#@evil.example.com")]
    [InlineData("https://games.example.com/?next=https://evil.example.com")]
    [InlineData("https://evil.example.com")]
    public async Task StartLoginAsync_ShouldOnlyEverRedirectToTheProvidersAuthorizationEndpoint(string publicBaseUrl)
    {
        var request = await _service.StartLoginAsync(publicBaseUrl, "//evil.example.com");

        var uri = new Uri(request.Url);
        uri.GetLeftPart(UriPartial.Path).Should().Be($"{Authority}/authorize");
        uri.Fragment.Should().BeEmpty();
        HttpUtility.ParseQueryString(uri.Query)["redirect_uri"].Should().Be($"{publicBaseUrl}/api/auth/oidc/callback");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartLoginAsync_ShouldKeepTheQueryOfTheAuthorizationEndpoint_AndDropItsFragment()
    {
        _idp.AuthorizationEndpoint = $"{Authority}/authorize?tenant=games#ignored";

        var request = await _service.StartLoginAsync(PublicBase, null);

        var uri = new Uri(request.Url);
        uri.GetLeftPart(UriPartial.Path).Should().Be($"{Authority}/authorize");
        uri.Fragment.Should().BeEmpty();
        var query = HttpUtility.ParseQueryString(uri.Query);
        query["tenant"].Should().Be("games");
        query["client_id"].Should().Be("bgt-client");
        query["state"].Should().Be(request.State);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartLoginAsync_ShouldRejectAnInsecureAuthorizationEndpoint()
    {
        _idp.AuthorizationEndpoint = "http://idp.example.com/authorize";

        var act = () => _service.StartLoginAsync(PublicBase, null);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InsecureAuthority);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartLoginAsync_ShouldRejectADiscoveryDocument_WhoseIssuerDiffersFromTheAuthority()
    {
        _idp.Issuer = "https://other.example.com";

        var act = () => _service.StartLoginAsync(PublicBase, null);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIssuerMismatch);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Error, "OIDC discovery for Example IdP reports issuer https://other.example.com, which does not match the configured authority");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartLoginAsync_ShouldReportAnUnreachableDiscoveryDocument()
    {
        _idp.DiscoveryStatus = HttpStatusCode.InternalServerError;

        var act = () => _service.StartLoginAsync(PublicBase, null);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcDiscoveryFailed);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartLoginAsync_ShouldThrow_WhenNoProviderIsEnabled()
    {
        await UpdateProviderAsync(p => p.Update(p.DisplayName, p.Authority, p.ClientId, null, false, p.Scopes, p.AutoProvisionUsers,
            null, null, null, null, null, null, null, null, null, null));

        var act = () => _service.StartLoginAsync(PublicBase, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldLogInTheLinkedUser_AndExchangeTheCodeWithPkce()
    {
        var user = LinkedUser();
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = await _service.StartLoginAsync(PublicBase, "/players");
        var code = await _idp.AuthorizeAsync(request.Url);
        var result = await _service.CompleteLoginAsync(code, request.State, request.State, Authority);

        result.RedirectPath.Should().Be("/players");
        result.Login.AccessToken.Should().Be("access-token");
        result.Login.User.Username.Should().Be(user.UserName);
        result.Login.User.NeedsPlayerLink.Should().BeFalse();
        _idp.TokenRequest!["redirect_uri"].Should().Be($"{PublicBase}/api/auth/oidc/callback");
        _idp.TokenRequest["code_verifier"].Should().NotBeNullOrEmpty();
        _idp.TokenRequest["grant_type"].Should().Be("authorization_code");
        _idp.TokenRequest["client_id"].Should().Be("bgt-client");
        _idp.TokenRequest.Should().NotContainKey("client_secret");
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).LastUsedAt.Should().NotBeNull();
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
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
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
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
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_AnIdTokenWithAnotherNonce()
    {
        _idp.User = KnownUser("sub-1");
        _idp.NonceOverride = "replayed-nonce";

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_ATokenResponseWithoutAnIdToken()
    {
        _idp.User = KnownUser("sub-1");
        _idp.OmitIdToken = true;

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Error, "OIDC token response from Example IdP carried no id_token");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_UserInfoForAnotherSubject()
    {
        _idp.User = KnownUser("sub-1");
        _idp.UserInfo = KnownUser("sub-2");

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Error, "User info from Example IdP describes a different subject than the id_token");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRejectAnIssParameterFromAnotherIssuer_BeforeExchangingTheCode()
    {
        _idp.User = KnownUser("sub-1");

        var act = () => LoginAsync(issuer: "https://evil.example.com");

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIssuerMismatch);
        _idp.TokenRequest.Should().BeNull();
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Warning, "The callback for Example IdP carried the iss parameter https://evil.example.com, which is not the provider's issuer");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRejectAMissingIssParameter_WhenTheProviderAdvertisesIt()
    {
        _idp.User = KnownUser("sub-1");
        _idp.AdvertisesIssParameter = true;

        var act = () => LoginAsync(issuer: null);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIssuerMismatch);
        _idp.TokenRequest.Should().BeNull();
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Warning, "The callback from Example IdP carried no iss parameter although the provider advertises it");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldAcceptAMissingIssParameter_WhenTheProviderDoesNotAdvertiseIt()
    {
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await LoginAsync(issuer: null);

        result.Login.User.Username.Should().Be("jane-linked");
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
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
        VerifySignedIn("jane", _createdUser!.Id);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "jane"), "User"), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [none]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
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
        VerifySignedIn("jane", _createdUser!.Id);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "jane"), "User"), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldProvisionANewUser_KeyedByTheIssuer()
    {
        _idp.User = KnownUser("sub-new");
        SetUpProvisioning();

        var result = await LoginAsync();

        result.Login.User.Username.Should().Be("jane");
        result.Login.User.NeedsPlayerLink.Should().BeTrue();
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.UserName == "jane" && u.Email == "jane@example.com" && u.EmailConfirmed && u.PlayerLinkPending)), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.RoleManagedByIdentityProvider), "User"), Times.Once);
        var login = await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken);
        login.ProviderKey.Should().Be("sub-new");
        login.Issuer.Should().Be(Authority);
        login.Provider.Should().Be("Example IdP");
        VerifySignedIn("jane", _createdUser!.Id);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldProvisionWithTheConfiguredDefaultRole()
    {
        await UpdateProviderAsync(p => p.SetDefaultRole("Reader"));
        _idp.User = KnownUser("sub-new");
        SetUpProvisioning();

        await LoginAsync();

        VerifyProvisioned("jane", "Reader");
        VerifySignedIn("jane", _createdUser!.Id);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("Domain Admins", false)]
    [InlineData("Admins", true)]
    [InlineData("Users, Admins", true)]
    [InlineData("bgt-Admins", false)]
    public async Task CompleteLoginAsync_ShouldGrantAdmin_OnlyForAnExactGroupInAScalarClaim(string groups, bool expectAdmin)
    {
        await ProvisionWithGroups(groups);

        VerifyProvisioned("jane", expectAdmin ? "Admin" : "User");
        VerifySignedIn("jane", _createdUser!.Id);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, groups, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, groups, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldGrantAdmin_OnlyForAnExactGroupInAnArrayClaim()
    {
        await ProvisionWithGroups(new[] { "Domain Admins", "Staff" });

        VerifyProvisioned("jane", "User");
        VerifySignedIn("jane", _createdUser!.Id);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, groups, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, groups, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
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

        await LoginAsync();

        _userManagerMock.Verify(x => x.AddToRoleAsync(user, "Admin"), Times.Once);
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { "User" }))), Times.Once);
        VerifySignedIn("jane-linked", LinkedUserId, roleReads: 2);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, groups, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, groups, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldMakeAnIdentityProviderManagedUserAdmin_WithTheDefaultAdminGroup()
    {
        var user = LinkedUser();
        user.ManageRoleByIdentityProvider();
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = KnownUser("sub-1");
        _idp.User["groups"] = new[] { "bgt-admins" };

        await LoginAsync();

        _userManagerMock.Verify(x => x.AddToRoleAsync(user, "Admin"), Times.Once);
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { "User" }))), Times.Once);
        VerifySignedIn("jane-linked", LinkedUserId, roleReads: 2);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, groups, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, groups, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldProvisionWithTheUsernameAsDisplayName_WhenTheProviderSendsNoNameOrEmail()
    {
        _idp.User = new Dictionary<string, object> { ["sub"] = "sub-anna", ["preferred_username"] = "anna", ["name"] = "", ["email"] = " " };
        SetUpProvisioning("anna", null);

        var result = await LoginAsync();

        result.Login.User.Username.Should().Be("anna");
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.UserName == "anna" && u.DisplayName == "anna" && u.Email == "sub-anna" + OidcService.PlaceholderEmailDomain && !u.EmailConfirmed)), Times.Once);
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).ProviderDisplayName.Should().Be("anna");
        VerifySignedIn("anna", _createdUser!.Id);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "anna"), "User"), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("anna"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReadTheDefaultClaims_WhenTheClaimTypesAreEmptyStrings()
    {
        await UpdateProviderAsync(p => p.Update(p.DisplayName, p.Authority, p.ClientId, null, true, p.Scopes, true,
            null, null, null, "", " ", "", "", " ", null, null));
        _idp.User = KnownUser("sub-new");
        SetUpProvisioning();

        await LoginAsync();

        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.UserName == "jane" && u.Email == "jane@example.com" && u.DisplayName == "Jane Doe")), Times.Once);
        VerifySignedIn("jane", _createdUser!.Id);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "jane"), "User"), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldFillTheEmptyDisplayNameAndPlaceholderEmail_OnALaterSignIn()
    {
        var user = SsoUser("anna", "sub-anna" + OidcService.PlaceholderEmailDomain, null);
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-anna"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = new Dictionary<string, object> { ["sub"] = "sub-anna", ["preferred_username"] = "anna", ["name"] = "Anna Smith", ["email"] = "anna@example.com" };
        SetUpEmailLookup("anna@example.com");

        var result = await LoginAsync();

        result.Login.User.DisplayName.Should().Be("Anna Smith");
        user.DisplayName.Should().Be("Anna Smith");
        user.Email.Should().Be("anna@example.com");
        user.EmailConfirmed.Should().BeTrue();
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).ProviderDisplayName.Should().Be("Anna Smith");
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        VerifySignedIn("anna", SsoUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.NormalizeEmail("anna@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Filled in the empty display name of user user-sso from Example IdP");
        _loggerMock.VerifyLog(LogLevel.Information, "Replaced the placeholder email of user user-sso with the email from Example IdP");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-sso logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldFillAnEmptyDisplayNameWithTheUsername_WhenTheProviderStillSendsNoName()
    {
        var user = SsoUser("anna", "sub-anna" + OidcService.PlaceholderEmailDomain, " ");
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-anna"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = new Dictionary<string, object> { ["sub"] = "sub-anna", ["preferred_username"] = "anna" };

        await LoginAsync();

        user.DisplayName.Should().Be("anna");
        user.Email.Should().Be("sub-anna" + OidcService.PlaceholderEmailDomain);
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).ProviderDisplayName.Should().Be("anna");
        VerifySignedIn("anna", SsoUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, exp, iat, iss, nbf, nonce, preferred_username, sub] and user info claim types [preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Filled in the empty display name of user user-sso from Example IdP");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-sso logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldNeverOverwriteADisplayNameOrEmailThatIsAlreadySet()
    {
        var user = SsoUser("anna", "anna@home.example.com", "Annie");
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-anna", "Old name"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = new Dictionary<string, object> { ["sub"] = "sub-anna", ["preferred_username"] = "anna", ["name"] = "Anna Smith", ["email"] = "anna@example.com" };
        SetUpEmailLookup("anna@example.com");

        await LoginAsync();

        user.DisplayName.Should().Be("Annie");
        user.Email.Should().Be("anna@home.example.com");
        user.EmailConfirmed.Should().BeFalse();
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).ProviderDisplayName.Should().Be("Old name");
        VerifySignedIn("anna", SsoUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-sso logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldConfirmTheEmail_WhenTheProviderReportsTheSameEmail()
    {
        var user = SsoUser("anna", "Anna@Example.com", "Anna");
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-anna"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = new Dictionary<string, object> { ["sub"] = "sub-anna", ["preferred_username"] = "anna", ["email"] = "anna@example.com" };

        await LoginAsync();

        user.Email.Should().Be("Anna@Example.com");
        user.EmailConfirmed.Should().BeTrue();
        VerifySignedIn("anna", SsoUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, nbf, nonce, preferred_username, sub] and user info claim types [email, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Confirmed the email of user user-sso because Example IdP reports the same email");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-sso logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldNotFillTheProfileOfALinkedLocalAccount()
    {
        var user = new ApplicationUser("local-anna", "local@example.com") { PasswordHash = "hash" };
        SetUpSignIn("local-anna");
        _context.Users.Add(user);
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-anna"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = new Dictionary<string, object> { ["sub"] = "sub-anna", ["preferred_username"] = "anna", ["name"] = "Anna Smith", ["email"] = "anna@example.com" };

        await LoginAsync();

        user.DisplayName.Should().BeNull();
        user.Email.Should().Be("local@example.com");
        VerifySignedIn("local-anna", user.Id);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Existing OIDC user {user.Id} logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldKeepThePlaceholderEmail_WhenAnotherAccountUsesTheProviderEmail()
    {
        var user = SsoUser("anna", "sub-anna" + OidcService.PlaceholderEmailDomain, "Anna");
        _context.Users.Add(new ApplicationUser("other", "anna@example.com") { NormalizedEmail = "ANNA@EXAMPLE.COM" });
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-anna"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = new Dictionary<string, object> { ["sub"] = "sub-anna", ["preferred_username"] = "anna", ["email"] = "anna@example.com" };
        SetUpEmailLookup("anna@example.com");

        await LoginAsync();

        user.Email.Should().Be("sub-anna" + OidcService.PlaceholderEmailDomain);
        user.DisplayName.Should().Be("Anna");
        VerifySignedIn("anna", SsoUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.NormalizeEmail("anna@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, nbf, nonce, preferred_username, sub] and user info claim types [email, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Warning, "User user-sso from Example IdP keeps a placeholder email because another account already uses the email from the provider");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-sso logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLinkAsync_ShouldLabelTheLinkWithTheUsername_WhenTheProviderSendsNoName()
    {
        var user = LinkedUser();
        _idp.User = new Dictionary<string, object> { ["sub"] = "sub-9", ["preferred_username"] = "anna" };
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var request = await _service.StartLinkAsync(user.Id, PublicBase);
        await _service.CompleteLinkAsync(await _idp.AuthorizeAsync(request.Url), request.State, request.State, Authority);

        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).ProviderDisplayName.Should().Be("anna");
        user.DisplayName.Should().Be("Jane");
        user.Email.Should().Be("linked@example.com");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByIdAsync("user-linked"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Link flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, exp, iat, iss, nbf, nonce, preferred_username, sub] and user info claim types [preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Linked OIDC provider Example IdP to user user-linked");
        VerifyNoOtherCalls();
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
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, groups, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, groups, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldFindTheLinkByIssuer_AndRelabelItWithTheDisplayName()
    {
        var user = LinkedUser();
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "old-name", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = KnownUser("sub-1");

        var result = await LoginAsync();

        result.Login.User.Username.Should().Be(user.UserName);
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).Provider.Should().Be("Example IdP");
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
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
        VerifySignedIn("jane", _createdUser!.Id);
        VerifyProvisioned("jane", "User");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
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
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Re-keyed external login 1 on the issuer reported by Example IdP");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
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
        VerifySignedIn("jane", _createdUser!.Id);
        VerifyProvisioned("jane", "User");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
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
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Warning, "Disabled user user-linked tried to sign in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRefuseToProvision_WhenAnUnlinkedLocalUserHasTheSameUsername()
    {
        _idp.User = KnownUser("sub-new");
        SetUpProvisioning();
        _userManagerMock.Setup(x => x.FindByNameAsync("jane")).ReturnsAsync(new ApplicationUser("jane", "other@example.com"));

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcUsernameTaken);
        (await _context.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Warning, "A new user from Example IdP has the username of an existing account that is not linked; manual linking required");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRefuseToProvision_WhenALocalUserHasTheSameEmail()
    {
        _idp.User = KnownUser("sub-new");
        _context.Users.Add(new ApplicationUser("local-jane", "jane@example.com") { Id = "user-local", NormalizedEmail = "JANE@EXAMPLE.COM" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        SetUpEmailLookup("jane@example.com");

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcEmailAlreadyRegistered);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Warning, $"A new user from Example IdP shares the email of existing user user-local; manual linking required");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRefuse_WhenProvisioningIsDisabled()
    {
        await UpdateProviderAsync(p => p.Update(p.DisplayName, p.Authority, p.ClientId, null, true, p.Scopes, false,
            null, null, null, null, null, null, null, null, null, null));
        _idp.User = KnownUser("sub-new");

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcProvisioningDisabled);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Warning, "OIDC login failed at Example IdP: user not found and auto-provisioning is disabled");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_WhenTheBrowserStateCookieIsMissing()
    {
        var request = await _service.StartLoginAsync(PublicBase, null);

        var act = () => _service.CompleteLoginAsync("code", request.State, null, Authority);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
        _idp.TokenRequest.Should().BeNull();
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Warning, "OIDC state for Example IdP did not match the pending Login flow");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_WhenTheBrowserStateCookieDoesNotMatch()
    {
        var request = await _service.StartLoginAsync(PublicBase, null);

        var act = () => _service.CompleteLoginAsync("code", request.State, "someone-elses-state", Authority);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Warning, "OIDC state for Example IdP did not match the pending Login flow");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_AnUnknownOrReusedState()
    {
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var request = await _service.StartLoginAsync(PublicBase, null);
        var code = await _idp.AuthorizeAsync(request.Url);
        await _service.CompleteLoginAsync(code, request.State, request.State, Authority);

        var replay = () => _service.CompleteLoginAsync(code, request.State, request.State, Authority);
        var unknown = () => _service.CompleteLoginAsync(code, "unknown", "unknown", Authority);

        await replay.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
        await unknown.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        _loggerMock.VerifyLog(LogLevel.Warning, "OIDC state for Example IdP did not match the pending Login flow", Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_AStateStartedForTheLinkFlow()
    {
        var request = await _service.StartLinkAsync("user-1", PublicBase);

        var act = () => _service.CompleteLoginAsync("code", request.State, request.State, Authority);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Link flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Warning, "OIDC state for Example IdP did not match the pending Login flow");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldSurfaceARejectedTokenExchange()
    {
        _idp.TokenStatus = HttpStatusCode.BadRequest;

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcExchangeFailed);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Error, "OIDC token exchange failed for Example IdP: BadRequest {\"error\":\"invalid_grant\"}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldSurfaceAFailingUserInfoEndpoint()
    {
        _idp.User = KnownUser("sub-1");
        _idp.UserInfoStatus = HttpStatusCode.InternalServerError;

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcUserInfoFailed);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Error, "Failed to fetch user info from Example IdP: InternalServerError ");
        VerifyNoOtherCalls();
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
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
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
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
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
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        _loggerMock.VerifyLog(LogLevel.Information, "Ending the identity provider session of user user-linked at Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLinkAsync_ShouldLinkTheUserFromTheStateRecord_KeyedByTheIssuer()
    {
        var user = LinkedUser();
        _idp.User = KnownUser("sub-9");
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var request = await _service.StartLinkAsync(user.Id, PublicBase);
        var uri = new Uri(request.Url);
        HttpUtility.ParseQueryString(uri.Query)["redirect_uri"].Should().Be($"{PublicBase}/api/auth/oidc/link-callback");

        var result = await _service.CompleteLinkAsync(await _idp.AuthorizeAsync(request.Url), request.State, request.State, Authority);

        result.UserId.Should().Be(user.Id);
        var login = await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken);
        login.UserId.Should().Be(user.Id);
        login.ProviderKey.Should().Be("sub-9");
        login.Issuer.Should().Be(Authority);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByIdAsync("user-linked"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Link flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Linked OIDC provider Example IdP to user user-linked");
        VerifyNoOtherCalls();
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

        var request = await _service.StartLinkAsync(user.Id, PublicBase);
        var code = await _idp.AuthorizeAsync(request.Url);
        var act = () => _service.CompleteLinkAsync(code, request.State, request.State, Authority);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcAlreadyLinked);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByIdAsync("user-linked"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Link flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLinkAsync_ShouldReject_ATamperedIdToken()
    {
        var user = LinkedUser();
        _idp.User = KnownUser("sub-9");
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        using var attacker = new TestIdTokens(_idp.Keys.KeyId);
        _idp.Signer = attacker;

        var request = await _service.StartLinkAsync(user.Id, PublicBase);
        var code = await _idp.AuthorizeAsync(request.Url);
        var act = () => _service.CompleteLinkAsync(code, request.State, request.State, Authority);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        (await _context.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByIdAsync("user-linked"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Link flow at Example IdP");
        VerifyNoOtherCalls();
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
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestDiscoveryAsync_ShouldReportMissingOptionalEndpointsAsNull()
    {
        _idp.HasUserInfo = false;
        _idp.HasEndSession = false;

        var result = await _service.TestDiscoveryAsync(Authority);

        result.UserInfoEndpoint.Should().BeNull();
        result.EndSessionEndpoint.Should().BeNull();
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestDiscoveryAsync_ShouldFlagAnIssuerMismatch_InsteadOfThrowing()
    {
        _idp.Issuer = "https://other.example.com";

        var result = await _service.TestDiscoveryAsync(Authority);

        result.Issuer.Should().Be("https://other.example.com");
        result.IssuerMatchesAuthority.Should().BeFalse();
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestDiscoveryAsync_ShouldRejectAnInsecureAuthority_WithoutCallingIt()
    {
        var act = () => _service.TestDiscoveryAsync("http://idp.example.com");

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InsecureAuthority);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestDiscoveryAsync_ShouldReportAnUnreachableProvider()
    {
        var act = () => _service.TestDiscoveryAsync("https://nowhere.example.com");

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcDiscoveryFailed);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Error, "OIDC discovery at https://nowhere.example.com could not be reached");
        VerifyNoOtherCalls();
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
        VerifyNoOtherCalls();
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
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldQueueThePictureClaim_ForTheSignedInUser()
    {
        var user = LinkedUser();
        _idp.User = KnownUser("sub-1");
        _idp.User["picture"] = "https://idp.example.com/avatars/jane.png";
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await LoginAsync();

        var queued = await TakeQueuedPictureAsync();
        queued.Should().Be(new IdentityProviderPictureImport(user.Id, "https://idp.example.com/avatars/jane.png", Authority));
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, picture, preferred_username, sub] and user info claim types [email, name, picture, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReadThePictureFromUserInfo_WhenTheIdTokenHasNone()
    {
        _idp.User = KnownUser("sub-new");
        _idp.UserInfo = KnownUser("sub-new");
        _idp.UserInfo["picture"] = "https://idp.example.com/avatars/userinfo.png";
        SetUpProvisioning();

        var result = await LoginAsync();

        var queued = await TakeQueuedPictureAsync();
        queued.UserId.Should().Be(result.Login.User.Id);
        queued.PictureUrl.Should().Be("https://idp.example.com/avatars/userinfo.png");
        VerifySignedIn("jane", _createdUser!.Id);
        VerifyProvisioned("jane", "User");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, picture, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldUseTheConfiguredPictureClaim()
    {
        await UpdateProviderAsync(p => p.SetPictureClaimType("avatar_url"));
        _idp.User = KnownUser("sub-new");
        _idp.User["picture"] = "https://idp.example.com/avatars/ignored.png";
        _idp.User["avatar_url"] = "https://idp.example.com/avatars/custom.png";
        SetUpProvisioning();

        await LoginAsync();

        (await TakeQueuedPictureAsync()).PictureUrl.Should().Be("https://idp.example.com/avatars/custom.png");
        VerifySignedIn("jane", _createdUser!.Id);
        VerifyProvisioned("jane", "User");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, avatar_url, email, exp, iat, iss, name, nbf, nonce, picture, preferred_username, sub] and user info claim types [avatar_url, email, name, picture, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldQueueNothing_WhenThereIsNoPictureClaim()
    {
        _idp.User = KnownUser("sub-new");
        SetUpProvisioning();

        await LoginAsync();

        using var nothingQueued = new CancellationTokenSource();
        _pictureQueue.DequeueAsync(nothingQueued.Token).IsCompleted.Should().BeFalse();
        await nothingQueued.CancelAsync();
        VerifySignedIn("jane", _createdUser!.Id);
        VerifyProvisioned("jane", "User");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldStillSignIn_WhenThePictureQueueIsFull()
    {
        for (var i = 0; i < IdentityProviderPictureQueue.Capacity; i++)
        {
            _pictureQueue.Enqueue(new IdentityProviderPictureImport($"other-{i}", "https://idp.example.com/a.png", Authority));
        }

        _idp.User = KnownUser("sub-new");
        _idp.User["picture"] = "https://idp.example.com/avatars/jane.png";
        SetUpProvisioning();

        var result = await LoginAsync();

        result.Login.AccessToken.Should().Be("access-token");
        VerifySignedIn("jane", _createdUser!.Id);
        VerifyProvisioned("jane", "User");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, picture, preferred_username, sub] and user info claim types [email, name, picture, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLinkAsync_ShouldQueueThePictureClaim()
    {
        var user = LinkedUser();
        _idp.User = KnownUser("sub-9");
        _idp.User["picture"] = "https://idp.example.com/avatars/linked.png";
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var request = await _service.StartLinkAsync(user.Id, PublicBase);
        await _service.CompleteLinkAsync(await _idp.AuthorizeAsync(request.Url), request.State, request.State, Authority);

        (await TakeQueuedPictureAsync()).Should().Be(new IdentityProviderPictureImport(user.Id, "https://idp.example.com/avatars/linked.png", Authority));
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByIdAsync("user-linked"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Link flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, picture, preferred_username, sub] and user info claim types [email, name, picture, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Linked OIDC provider Example IdP to user user-linked");
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Constants_ShouldKeepTheirValues()
    {
        OidcService.HttpClientName.Should().Be("oidc");
        OidcService.CallbackPath.Should().Be("/api/auth/oidc/callback");
        OidcService.LinkCallbackPath.Should().Be("/api/auth/oidc/link-callback");
        OidcService.PlaceholderEmailDomain.Should().Be("@sso.external");
        OidcService.PendingAuthorizationLifetime.Should().Be(TimeSpan.FromMinutes(10));
        OidcService.HandoffLifetime.Should().Be(TimeSpan.FromMinutes(1));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_AnEmptyState()
    {
        var act = () => _service.CompleteLoginAsync("code", " ", " ", Authority);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldUseTheEmailAsUsername_WhenTheProviderSendsNoUsername()
    {
        _idp.User = new Dictionary<string, object> { ["sub"] = "sub-new", ["email"] = "jane@example.com" };
        SetUpProvisioning("jane@example.com");

        var result = await LoginAsync();

        result.Login.User.Username.Should().Be("jane@example.com");
        _userManagerMock.Verify(x => x.FindByNameAsync("jane@example.com"), Times.Once);
        VerifySignedIn("jane@example.com", _createdUser!.Id);
        VerifyProvisioned("jane@example.com", "User");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, nbf, nonce, sub] and user info claim types [email, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldGenerateAUsername_WhenTheProviderSendsNoUsernameOrEmail()
    {
        _idp.User = new Dictionary<string, object> { ["sub"] = "sub-new" };
        SetUpProvisioning("sso_sub-new", null);

        var result = await LoginAsync();

        result.Login.User.Username.Should().Be("sso_sub-new");
        _createdUser!.Email.Should().Be("sub-new@sso.external");
        _createdUser.DisplayName.Should().Be("sso_sub-new");
        _userManagerMock.Verify(x => x.FindByNameAsync("sso_sub-new"), Times.Once);
        VerifySignedIn("sso_sub-new", _createdUser.Id);
        VerifyProvisioned("sso_sub-new", "User");
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, exp, iat, iss, nbf, nonce, sub] and user info claim types [sub]");
        _loggerMock.VerifyLog(LogLevel.Information, $"Auto-provisioned user {_createdUser!.Id} from Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldFail_WhenCreatingTheUserFails()
    {
        _idp.User = KnownUser("sub-new");
        SetUpEmailLookup("jane@example.com");
        _userManagerMock.Setup(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.UserName == "jane")))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName" }));

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcProvisioningFailed);
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.UserName == "jane")), Times.Once);
        (await _context.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _userManagerMock.Verify(x => x.FindByNameAsync("jane"), Times.Once);
        _userManagerMock.Verify(x => x.NormalizeEmail("jane@example.com"), Times.Once);
        _userManagerMock.Verify(x => x.Users, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Error, "Failed to auto-provision a user from Example IdP: DuplicateUserName");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldFail_WhenUserInfoCarriesNoSubject()
    {
        _idp.User = KnownUser("sub-1");
        _idp.UserInfo = new Dictionary<string, object> { ["name"] = "Jane" };

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcUserInfoFailed);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Error, "User info from Example IdP carried no sub claim");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldSkipUserInfo_WhenTheTokenResponseHasNoAccessToken()
    {
        _idp.OmitAccessToken = true;
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await LoginAsync();

        _idp.UserInfoCalls.Should().Be(0);
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [none]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldUseTheConfiguredEndpoints_InsteadOfTheDiscoveredOnes()
    {
        await UpdateProviderAsync(p => p.Update(p.DisplayName, p.Authority, p.ClientId, null, true, p.Scopes, true,
            $"{Authority}/custom/authorize", $"{Authority}/custom/token", $"{Authority}/custom/userinfo", null, null, null, null, null, null, null));
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", Authority, "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = await _service.StartLoginAsync(PublicBase, null);
        var result = await _service.CompleteLoginAsync(await _idp.AuthorizeAsync(request.Url), request.State, request.State, Authority);

        new Uri(request.Url).AbsolutePath.Should().Be("/custom/authorize");
        _idp.Paths.Should().Contain("/custom/token").And.Contain("/custom/userinfo");
        result.RedirectPath.Should().Be("/");
        VerifySignedIn("jane-linked", LinkedUserId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(3));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Debug, "Sign-in via Example IdP received id_token claim types [aud, email, exp, iat, iss, name, nbf, nonce, preferred_username, sub] and user info claim types [email, name, preferred_username, sub]");
        _loggerMock.VerifyLog(LogLevel.Information, "Existing OIDC user user-linked logged in via Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRejectAnInsecureConfiguredTokenEndpoint()
    {
        await UpdateProviderAsync(p => p.Update(p.DisplayName, p.Authority, p.ClientId, null, true, p.Scopes, true,
            null, "http://idp.example.com/token", null, null, null, null, null, null, null, null));
        _idp.User = KnownUser("sub-1");

        var act = () => LoginAsync();

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InsecureAuthority);
        _idp.TokenRequest.Should().BeNull();
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLinkAsync_ShouldThrow_WhenTheUserNoLongerExists()
    {
        var request = await _service.StartLinkAsync("gone", PublicBase);

        var act = () => _service.CompleteLinkAsync("code", request.State, request.State, Authority);

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _idp.TokenRequest.Should().BeNull();
        _userManagerMock.Verify(x => x.FindByIdAsync("gone"), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Link flow at Example IdP");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteLinkAsync_ShouldReject_AStateStartedForTheLoginFlow()
    {
        var request = await _service.StartLoginAsync(PublicBase, null);

        var act = () => _service.CompleteLinkAsync("code", request.State, request.State, Authority);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
        _httpClientFactoryMock.Verify(x => x.CreateClient(OidcService.HttpClientName), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Started OIDC Login flow at Example IdP");
        _loggerMock.VerifyLog(LogLevel.Warning, "OIDC state for Example IdP did not match the pending Link flow");
        VerifyNoOtherCalls();
    }

    private async Task<IdentityProviderPictureImport> TakeQueuedPictureAsync()
    {
        var pending = _pictureQueue.DequeueAsync(TestContext.Current.CancellationToken);
        pending.IsCompleted.Should().BeTrue();
        return await pending;
    }

    private async Task<OidcLoginResult> LoginAsync(string? issuer = Authority)
    {
        var request = await _service.StartLoginAsync(PublicBase, null);
        var code = await _idp.AuthorizeAsync(request.Url);
        return await _service.CompleteLoginAsync(code, request.State, request.State, issuer);
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

    private void SetUpProvisioning(string userName = "jane", string? email = "jane@example.com")
    {
        _userManagerMock.Setup(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.UserName == userName)))
            .Callback((ApplicationUser user) => _createdUser = user)
            .ReturnsAsync(IdentityResult.Success);
        SetUpSignIn(userName);
        if (email != null)
        {
            SetUpEmailLookup(email);
        }
    }

    private void SetUpEmailLookup(string email)
    {
        _userManagerMock.Setup(x => x.NormalizeEmail(email)).Returns(email.ToUpperInvariant());
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
        var user = new ApplicationUser("jane-linked", "linked@example.com", "Jane") { Id = LinkedUserId };
        _context.Users.Add(user);
        _context.SaveChanges();
        SetUpSignIn("jane-linked");
        return user;
    }

    private ApplicationUser SsoUser(string userName, string email, string? displayName)
    {
        var user = new ApplicationUser(userName, email, displayName) { Id = SsoUserId };
        _context.Users.Add(user);
        _context.SaveChanges();
        SetUpSignIn(userName);
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
        public bool OmitAccessToken { get; set; }
        public List<string> Paths { get; } = [];
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
            Paths.Add(path);
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
                if (OmitAccessToken)
                {
                    return Json(new { token_type = "Bearer", id_token = LastIdToken });
                }

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
