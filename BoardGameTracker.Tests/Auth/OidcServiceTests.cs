using System.Net.Http;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcServiceTests : IDisposable
{
    private readonly MainDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly MemoryCache _cache;
    private readonly Mock<ILogger<OidcService>> _loggerMock;
    private readonly Mock<IOidcConfigurationProvider> _configurationsMock = new();
    private readonly Mock<IOidcIdTokenValidator> _idTokenValidatorMock = new();
    private readonly Mock<IOidcRoleReconciler> _roleReconcilerMock = new();
    private readonly Mock<ISecretProtector> _secretProtectorMock = new();
    private readonly OidcService _service;

    public OidcServiceTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<OidcService>>();

        _service = new OidcService(
            _context,
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _httpClientFactoryMock.Object,
            _cache,
            _secretProtectorMock.Object,
            _configurationsMock.Object,
            _idTokenValidatorMock.Object,
            _roleReconcilerMock.Object,
            Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 }),
            _loggerMock.Object);

        _userManagerMock.Invocations.Clear();
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _userManagerMock.VerifyNoOtherCalls();
        _tokenServiceMock.VerifyNoOtherCalls();
        _httpClientFactoryMock.VerifyNoOtherCalls();
        _configurationsMock.VerifyNoOtherCalls();
        _idTokenValidatorMock.VerifyNoOtherCalls();
        _roleReconcilerMock.VerifyNoOtherCalls();
        _secretProtectorMock.VerifyNoOtherCalls();
    }

    private static OidcProvider CreateDisabledProvider(string name)
    {
        var provider = new OidcProvider(name, name, "https://idp.example.com", "client-id");
        provider.Update(name, "https://idp.example.com", "client-id", null, false, "openid", true,
            null, null, null, null, null, null, null, null, null, null);
        return provider;
    }

    #region GetEnabledProviderAsync

    [Fact]
    public async Task GetEnabledProviderAsync_ShouldReturnProviderInfo_WhenEnabledProviderExists()
    {
        _context.OidcProviders.Add(new OidcProvider("google", "Google", "https://accounts.google.com", "client-id"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetEnabledProviderAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("google");
        result.DisplayName.Should().Be("Google");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetEnabledProviderAsync_ShouldReturnNull_WhenNoProviderExists()
    {
        var result = await _service.GetEnabledProviderAsync();

        result.Should().BeNull();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetEnabledProviderAsync_ShouldReturnNull_WhenProviderIsDisabled()
    {
        _context.OidcProviders.Add(CreateDisabledProvider("google"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetEnabledProviderAsync();

        result.Should().BeNull();
        VerifyNoOtherCalls();
    }

    #endregion

    #region HasEnabledProviderAsync

    [Fact]
    public async Task HasEnabledProviderAsync_ShouldReturnTrue_WhenEnabledProviderExists()
    {
        _context.OidcProviders.Add(new OidcProvider("google", "Google", "https://accounts.google.com", "client-id"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.HasEnabledProviderAsync();

        result.Should().BeTrue();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HasEnabledProviderAsync_ShouldReturnFalse_WhenOnlyDisabledProviderExists()
    {
        _context.OidcProviders.Add(CreateDisabledProvider("google"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.HasEnabledProviderAsync();

        result.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetExternalLoginsAsync

    [Fact]
    public async Task GetExternalLoginsAsync_ShouldReturnOnlyLoginsOfThatUser()
    {
        _context.ExternalLogins.Add(new ExternalLogin("user-1", "google", "https://accounts.google.com", "key-1", "Google"));
        _context.ExternalLogins.Add(new ExternalLogin("user-1", "github", "https://github.com", "key-2"));
        _context.ExternalLogins.Add(new ExternalLogin("user-2", "google", "https://accounts.google.com", "key-3"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetExternalLoginsAsync("user-1");

        result.Should().HaveCount(2);
        result.Should().Contain(x => x.Provider == "google" && x.ProviderKey == "key-1");
        result.Should().Contain(x => x.Provider == "github" && x.ProviderKey == "key-2");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetExternalLoginsAsync_ShouldReturnEmptyList_WhenUserHasNoLogins()
    {
        var result = await _service.GetExternalLoginsAsync("user-1");

        result.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    #endregion

    #region UnlinkExternalLoginAsync

    [Fact]
    public async Task UnlinkExternalLoginAsync_ShouldRemoveLogin_WhenLoginBelongsToUser()
    {
        _context.Users.Add(new ApplicationUser("alice", "alice@example.com", "Alice") { Id = "user-1", PasswordHash = "hash" });
        var login = new ExternalLogin("user-1", "google", "https://accounts.google.com", "key-1");
        _context.ExternalLogins.Add(login);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.UnlinkExternalLoginAsync("user-1", login.Id);

        var remaining = await _context.ExternalLogins.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnlinkExternalLoginAsync_ShouldRefuse_WhenItIsThePasswordlessUsersOnlySignInMethod()
    {
        _context.Users.Add(new ApplicationUser("sso-only", "sso@example.com", "Sso") { Id = "user-1" });
        var login = new ExternalLogin("user-1", "google", "https://accounts.google.com", "key-1");
        _context.ExternalLogins.Add(login);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => _service.UnlinkExternalLoginAsync("user-1", login.Id);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.LastSignInMethod);
        (await _context.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnlinkExternalLoginAsync_ShouldAllowAPasswordlessUser_ToRemoveOneOfSeveralLogins()
    {
        _context.Users.Add(new ApplicationUser("sso-only", "sso@example.com", "Sso") { Id = "user-1" });
        var first = new ExternalLogin("user-1", "google", "https://accounts.google.com", "key-1");
        var second = new ExternalLogin("user-1", "keycloak", "https://keycloak.example.com", "key-2");
        _context.ExternalLogins.AddRange(first, second);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.UnlinkExternalLoginAsync("user-1", first.Id);

        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).Id.Should().Be(second.Id);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnlinkExternalLoginAsync_ShouldThrowEntityNotFoundException_WhenLoginDoesNotExist()
    {
        var act = () => _service.UnlinkExternalLoginAsync("user-1", 999);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnlinkExternalLoginAsync_ShouldThrowEntityNotFoundException_WhenLoginBelongsToAnotherUser()
    {
        var login = new ExternalLogin("user-2", "google", "https://accounts.google.com", "key-1");
        _context.ExternalLogins.Add(login);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => _service.UnlinkExternalLoginAsync("user-1", login.Id);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        var remaining = await _context.ExternalLogins.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().ContainSingle();
        VerifyNoOtherCalls();
    }

    #endregion

    #region EndSessionAsync

    private async Task<(OidcProvider Provider, string Key)> AddSessionAsync(DateTime expiresAt, bool enabled = true)
    {
        var provider = new OidcProvider("kc", "Keycloak", "https://sso.example.com/realms/bgt", "bgt");
        if (!enabled)
        {
            provider.Update("Keycloak", provider.Authority, "bgt", null, false, "openid", true, null, null, null, null, null, null, null, null, null, null);
        }

        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        const string key = "session-key";
        _context.OidcSessions.Add(new OidcSession(OidcService.HashKey(key), "user-1", provider.Id, "protected-id-token", expiresAt));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (provider, key);
    }

    private void SetUpMetadata(string? endSessionEndpoint)
    {
        _configurationsMock.Setup(x => x.GetAsync("https://sso.example.com/realms/bgt", default)).ReturnsAsync(new OpenIdConnectConfiguration
        {
            Issuer = "https://sso.example.com/realms/bgt",
            EndSessionEndpoint = endSessionEndpoint,
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-key")]
    public async Task EndSessionAsync_ShouldReturnNull_WithoutAKnownSession(string? key)
    {
        var result = await _service.EndSessionAsync(key, "https://games.example.com");

        result.Should().BeNull();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EndSessionAsync_ShouldBuildTheSignOutUrl_AndForgetTheSession()
    {
        var (_, key) = await AddSessionAsync(DateTime.UtcNow.AddDays(1));
        SetUpMetadata("https://sso.example.com/realms/bgt/protocol/openid-connect/logout");
        _secretProtectorMock.Setup(x => x.Unprotect("protected-id-token")).Returns("raw.id.token");

        var result = await _service.EndSessionAsync(key, "https://games.example.com/");

        result.Should().Be("https://sso.example.com/realms/bgt/protocol/openid-connect/logout?id_token_hint=raw.id.token&post_logout_redirect_uri=https%3A%2F%2Fgames.example.com%2Flogin&client_id=bgt");
        (await _context.OidcSessions.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        _configurationsMock.Verify(x => x.GetAsync("https://sso.example.com/realms/bgt", default), Times.Once);
        _secretProtectorMock.Verify(x => x.Unprotect("protected-id-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EndSessionAsync_ShouldReturnNull_WhenTheProviderHasNoSignOutEndpoint()
    {
        var (_, key) = await AddSessionAsync(DateTime.UtcNow.AddDays(1));
        SetUpMetadata(null);

        var result = await _service.EndSessionAsync(key, "https://games.example.com");

        result.Should().BeNull();
        (await _context.OidcSessions.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        _configurationsMock.Verify(x => x.GetAsync("https://sso.example.com/realms/bgt", default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EndSessionAsync_ShouldReturnNull_WhenTheSignOutEndpointIsInsecure()
    {
        var (_, key) = await AddSessionAsync(DateTime.UtcNow.AddDays(1));
        SetUpMetadata("http://sso.example.com/logout");

        var result = await _service.EndSessionAsync(key, "https://games.example.com");

        result.Should().BeNull();
        _configurationsMock.Verify(x => x.GetAsync("https://sso.example.com/realms/bgt", default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EndSessionAsync_ShouldReturnNull_ForAnExpiredSession()
    {
        var (_, key) = await AddSessionAsync(DateTime.UtcNow.AddMinutes(-1));

        var result = await _service.EndSessionAsync(key, "https://games.example.com");

        result.Should().BeNull();
        (await _context.OidcSessions.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EndSessionAsync_ShouldReturnNull_WhenTheProviderWasDisabled()
    {
        var (_, key) = await AddSessionAsync(DateTime.UtcNow.AddDays(1), enabled: false);

        var result = await _service.EndSessionAsync(key, "https://games.example.com");

        result.Should().BeNull();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EndSessionAsync_ShouldReturnNull_WhenTheProviderCannotBeReached()
    {
        var (_, key) = await AddSessionAsync(DateTime.UtcNow.AddDays(1));
        _configurationsMock.Setup(x => x.GetAsync("https://sso.example.com/realms/bgt", default))
            .ThrowsAsync(new DomainException(Constants.Errors.OidcDiscoveryFailed));

        var result = await _service.EndSessionAsync(key, "https://games.example.com");

        result.Should().BeNull();
        _configurationsMock.Verify(x => x.GetAsync("https://sso.example.com/realms/bgt", default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_ShouldOnlyRemoveExpiredSessions()
    {
        _context.OidcSessions.Add(new OidcSession("old", "user-1", 1, "t", DateTime.UtcNow.AddMinutes(-1)));
        _context.OidcSessions.Add(new OidcSession("new", "user-1", 1, "t", DateTime.UtcNow.AddDays(1)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.CleanupExpiredSessionsAsync();

        (await _context.OidcSessions.SingleAsync(TestContext.Current.CancellationToken)).KeyHash.Should().Be("new");
        VerifyNoOtherCalls();
    }

    #endregion
}
