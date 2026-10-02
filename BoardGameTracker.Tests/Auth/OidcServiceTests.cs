using System;
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
            Mock.Of<ISecretProtector>(),
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
        _context.ExternalLogins.Add(new ExternalLogin("user-1", "google", "key-1", "Google"));
        _context.ExternalLogins.Add(new ExternalLogin("user-1", "github", "key-2"));
        _context.ExternalLogins.Add(new ExternalLogin("user-2", "google", "key-3"));
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
        var login = new ExternalLogin("user-1", "google", "key-1");
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
        var login = new ExternalLogin("user-1", "google", "key-1");
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
        var first = new ExternalLogin("user-1", "google", "key-1");
        var second = new ExternalLogin("user-1", "keycloak", "key-2");
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
        var login = new ExternalLogin("user-2", "google", "key-1");
        _context.ExternalLogins.Add(login);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => _service.UnlinkExternalLoginAsync("user-1", login.Id);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        var remaining = await _context.ExternalLogins.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().ContainSingle();
        VerifyNoOtherCalls();
    }

    #endregion
}
