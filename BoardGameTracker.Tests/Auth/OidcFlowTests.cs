using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(OidcService.HttpClientName)).Returns(() => new HttpClient(_idp, disposeHandler: false));

        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new OidcService(
            _context,
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            httpClientFactoryMock.Object,
            _cache,
            Mock.Of<ISecretProtector>(),
            Mock.Of<ILogger<OidcService>>());

        _context.OidcProviders.Add(new OidcProvider("idp", "Example IdP", Authority, "bgt-client"));
        _context.SaveChanges();
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
        query["redirect_uri"].Should().Be($"{PublicBase}/api/auth/oidc/idp/callback");
        query["code_challenge_method"].Should().Be("S256");
        query["code_challenge"].Should().NotBeNullOrEmpty();
        query["nonce"].Should().NotBeNullOrEmpty();
        query["state"].Should().Be(request.State);
        request.Url.Should().NotContain("/games/3");
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
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = await _service.StartLoginAsync("idp", PublicBase, redirect);
        var result = await _service.CompleteLoginAsync("idp", await _idp.AuthorizeAsync(request.Url), request.State, request.State);

        result.RedirectPath.Should().Be("/");
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
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, "idp", "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = await _service.StartLoginAsync("idp", PublicBase, "/players");
        var code = await _idp.AuthorizeAsync(request.Url);
        var result = await _service.CompleteLoginAsync("idp", code, request.State, request.State);

        result.RedirectPath.Should().Be("/players");
        result.Login.AccessToken.Should().Be("access-token");
        result.Login.User.Username.Should().Be(user.UserName);
        _idp.TokenRequest!["redirect_uri"].Should().Be($"{PublicBase}/api/auth/oidc/idp/callback");
        _idp.TokenRequest["code_verifier"].Should().NotBeNullOrEmpty();
        _idp.TokenRequest["grant_type"].Should().Be("authorization_code");
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).LastUsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldProvisionANewUser_WhenAllowed()
    {
        _idp.User = KnownUser("sub-new");
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.NormalizeEmail(It.IsAny<string>())).Returns((string e) => e.ToUpperInvariant());
        _userManagerMock.Setup(x => x.Users).Returns(_context.Users);

        var request = await _service.StartLoginAsync("idp", PublicBase, null);
        var result = await _service.CompleteLoginAsync("idp", await _idp.AuthorizeAsync(request.Url), request.State, request.State);

        result.Login.User.Username.Should().Be("jane");
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.UserName == "jane" && u.Email == "jane@example.com")), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).ProviderKey.Should().Be("sub-new");
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRefuseToProvision_WhenALocalUserHasTheSameEmail()
    {
        _idp.User = KnownUser("sub-new");
        _context.Users.Add(new ApplicationUser("local-jane", "jane@example.com") { NormalizedEmail = "JANE@EXAMPLE.COM" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _userManagerMock.Setup(x => x.NormalizeEmail(It.IsAny<string>())).Returns((string e) => e.ToUpperInvariant());
        _userManagerMock.Setup(x => x.Users).Returns(_context.Users);

        var request = await _service.StartLoginAsync("idp", PublicBase, null);
        var code = await _idp.AuthorizeAsync(request.Url);
        var act = () => _service.CompleteLoginAsync("idp", code, request.State, request.State);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcEmailAlreadyRegistered);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldRefuse_WhenProvisioningIsDisabled()
    {
        var provider = await _context.OidcProviders.SingleAsync(TestContext.Current.CancellationToken);
        provider.Update(provider.DisplayName, provider.Authority, provider.ClientId, null, true, provider.Scopes, false,
            null, null, null, null, null, null, null, null, null, null);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _idp.User = KnownUser("sub-new");

        var request = await _service.StartLoginAsync("idp", PublicBase, null);
        var code = await _idp.AuthorizeAsync(request.Url);
        var act = () => _service.CompleteLoginAsync("idp", code, request.State, request.State);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcProvisioningDisabled);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_WhenTheBrowserStateCookieIsMissing()
    {
        var request = await _service.StartLoginAsync("idp", PublicBase, null);

        var act = () => _service.CompleteLoginAsync("idp", "code", request.State, null);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
        _idp.TokenRequest.Should().BeNull();
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_WhenTheBrowserStateCookieDoesNotMatch()
    {
        var request = await _service.StartLoginAsync("idp", PublicBase, null);

        var act = () => _service.CompleteLoginAsync("idp", "code", request.State, "someone-elses-state");

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_AnUnknownOrReusedState()
    {
        _idp.User = KnownUser("sub-1");
        _context.ExternalLogins.Add(new ExternalLogin(LinkedUser().Id, "idp", "sub-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var request = await _service.StartLoginAsync("idp", PublicBase, null);
        var code = await _idp.AuthorizeAsync(request.Url);
        await _service.CompleteLoginAsync("idp", code, request.State, request.State);

        var replay = () => _service.CompleteLoginAsync("idp", code, request.State, request.State);
        var unknown = () => _service.CompleteLoginAsync("idp", code, "unknown", "unknown");

        await replay.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
        await unknown.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldReject_AStateStartedForTheLinkFlow()
    {
        var request = await _service.StartLinkAsync("user-1", "idp", PublicBase);

        var act = () => _service.CompleteLoginAsync("idp", "code", request.State, request.State);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidAuthSession);
    }

    [Fact]
    public async Task CompleteLoginAsync_ShouldSurfaceARejectedTokenExchange()
    {
        _idp.TokenStatus = HttpStatusCode.BadRequest;
        var request = await _service.StartLoginAsync("idp", PublicBase, null);

        var code = await _idp.AuthorizeAsync(request.Url);
        var act = () => _service.CompleteLoginAsync("idp", code, request.State, request.State);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcExchangeFailed);
    }

    [Fact]
    public async Task CompleteLinkAsync_ShouldLinkTheUserFromTheStateRecord()
    {
        var user = LinkedUser();
        _idp.User = KnownUser("sub-9");
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var request = await _service.StartLinkAsync(user.Id, "idp", PublicBase);
        var uri = new Uri(request.Url);
        HttpUtility.ParseQueryString(uri.Query)["redirect_uri"].Should().Be($"{PublicBase}/api/auth/oidc/idp/link-callback");

        var result = await _service.CompleteLinkAsync("idp", await _idp.AuthorizeAsync(request.Url), request.State, request.State);

        result.UserId.Should().Be(user.Id);
        result.ProviderName.Should().Be("idp");
        var login = await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken);
        login.UserId.Should().Be(user.Id);
        login.ProviderKey.Should().Be("sub-9");
    }

    [Fact]
    public async Task CompleteLinkAsync_ShouldRefuse_WhenTheExternalAccountIsAlreadyLinked()
    {
        var user = LinkedUser();
        _idp.User = KnownUser("sub-9");
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _context.ExternalLogins.Add(new ExternalLogin("someone-else", "idp", "sub-9"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = await _service.StartLinkAsync(user.Id, "idp", PublicBase);
        var code = await _idp.AuthorizeAsync(request.Url);
        var act = () => _service.CompleteLinkAsync("idp", code, request.State, request.State);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcAlreadyLinked);
    }

    [Fact]
    public async Task TestDiscoveryAsync_ShouldReturnTheParsedEndpoints_AndWhetherTheIssuerMatches()
    {
        var result = await _service.TestDiscoveryAsync(Authority + "/");

        result.Issuer.Should().Be(Authority);
        result.AuthorizationEndpoint.Should().Be($"{Authority}/authorize");
        result.TokenEndpoint.Should().Be($"{Authority}/token");
        result.UserInfoEndpoint.Should().Be($"{Authority}/userinfo");
        result.IssuerMatchesAuthority.Should().BeTrue();
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
        private readonly Dictionary<string, string> _codes = new();

        public FakeIdentityProviderHandler(string authority)
        {
            _authority = authority;
            Issuer = authority;
            AuthorizationEndpoint = $"{authority}/authorize";
        }

        public string Issuer { get; set; }
        public string AuthorizationEndpoint { get; set; }
        public HttpStatusCode TokenStatus { get; set; } = HttpStatusCode.OK;
        public Dictionary<string, object> User { get; set; } = new();
        public Dictionary<string, string>? TokenRequest { get; private set; }

        public Task<string> AuthorizeAsync(string authorizationUrl)
        {
            var query = HttpUtility.ParseQueryString(new Uri(authorizationUrl).Query);
            var code = Guid.NewGuid().ToString("N");
            _codes[code] = query["code_challenge"]!;
            return Task.FromResult(code);
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
                return Json(new
                {
                    issuer = Issuer,
                    authorization_endpoint = AuthorizationEndpoint,
                    token_endpoint = $"{_authority}/token",
                    userinfo_endpoint = $"{_authority}/userinfo",
                });
            }

            if (path.EndsWith("/token", StringComparison.Ordinal))
            {
                var form = HttpUtility.ParseQueryString(await request.Content!.ReadAsStringAsync(cancellationToken));
                TokenRequest = form.AllKeys.ToDictionary(k => k!, k => form[k]!);
                var expectedChallenge = _codes.GetValueOrDefault(TokenRequest["code"]);
                var actualChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(TokenRequest["code_verifier"]))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                if (TokenStatus != HttpStatusCode.OK || expectedChallenge != actualChallenge)
                {
                    return new HttpResponseMessage(TokenStatus == HttpStatusCode.OK ? HttpStatusCode.BadRequest : TokenStatus) { Content = new StringContent("{\"error\":\"invalid_grant\"}") };
                }

                return Json(new { access_token = "idp-access-token", token_type = "Bearer" });
            }

            if (path.EndsWith("/userinfo", StringComparison.Ordinal))
            {
                return request.Headers.Authorization?.Parameter == "idp-access-token"
                    ? Json(User)
                    : new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(object body) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
    }
}
