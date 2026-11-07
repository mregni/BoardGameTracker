using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Email.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class AuthServiceTests : IDisposable
{
    private const string ClientAddress = "203.0.113.7";
    private const string ResetSubject = "Reset your BoardGameTracker password";
    private const string EmailChangedSubject = "Your BoardGameTracker email address was changed";

    private readonly ManualCacheClock _cacheClock = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly Mock<IPlayerService> _playerServiceMock;
    private readonly Mock<IPlayerAccountImageService> _playerAccountImageServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IPublicUrlBuilder> _publicUrlBuilderMock;
    private readonly Mock<IBackgroundEmailSender> _backgroundEmailSenderMock;
    private readonly Mock<ILoginAttemptTracker> _loginAttemptsMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly MainDbContext _context;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _playerServiceMock = new Mock<IPlayerService>();
        _emailServiceMock = new Mock<IEmailService>();
        _publicUrlBuilderMock = new Mock<IPublicUrlBuilder>();
        _backgroundEmailSenderMock = new Mock<IBackgroundEmailSender>();
        _loginAttemptsMock = new Mock<ILoginAttemptTracker>();

        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);

        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tokenServiceMock.Object,
            _environmentProviderMock.Object,
            _playerServiceMock.Object,
            _playerAccountImageServiceMock.Object,
            _emailServiceMock.Object,
            _publicUrlBuilderMock.Object,
            _backgroundEmailSenderMock.Object,
            _loginAttemptsMock.Object,
            _context,
            new MemoryCache(new MemoryCacheOptions { Clock = _cacheClock }),
            _dateTimeProviderMock.Object,
            _loggerMock.Object);

        _userManagerMock.Invocations.Clear();
        _signInManagerMock.Invocations.Clear();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _userManagerMock.VerifyNoOtherCalls();
        _signInManagerMock.VerifyNoOtherCalls();
        _tokenServiceMock.VerifyNoOtherCalls();
        _environmentProviderMock.VerifyNoOtherCalls();
        _playerServiceMock.VerifyNoOtherCalls();
        _playerAccountImageServiceMock.VerifyNoOtherCalls();
        _emailServiceMock.VerifyNoOtherCalls();
        _publicUrlBuilderMock.VerifyNoOtherCalls();
        _backgroundEmailSenderMock.VerifyNoOtherCalls();
        _loginAttemptsMock.VerifyNoOtherCalls();
        _dateTimeProviderMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    private static string ResetBody(string encodedUrl) =>
        $"<p>A password reset was requested for your account.</p><p><a href=\"{encodedUrl}\">Reset your password</a></p><p>If you didn't request this, you can safely ignore this email.</p>";

    private static string EmailChangedBody(string encodedUsername, string change) =>
        $"<p>The email address of your account <strong>{encodedUsername}</strong> {change}.</p><p>If you did not do this, change your password and contact your administrator.</p>";

    private static bool IsNewAccount(ApplicationUser user, string username, string email) =>
        user.UserName == username && user.Email == email && user.DisplayName == username && user.EmailConfirmed;

    private sealed class ManualCacheClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);
    }

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_ShouldReturnLoginResponse_WhenCredentialsAreValid()
    {
        var request = new LoginRequest("testuser", "password123");
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var roles = new List<string> { "User" };
        var refreshToken = RefreshToken.Create(user.Id, 7);
        var accessToken = "access-token-value";
        var expiry = DateTime.UtcNow.AddHours(1);

        _userManagerMock.Setup(x => x.FindByNameAsync("testuser")).ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "password123", false))
            .ReturnsAsync(SignInResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, roles, refreshToken.SessionId)).Returns(accessToken);
        _tokenServiceMock.Setup(x => x.GenerateRefreshTokenAsync(user.Id)).ReturnsAsync(refreshToken);
        _tokenServiceMock.Setup(x => x.GetAccessTokenExpiry()).Returns(expiry);

        var result = await _authService.LoginAsync(request, ClientAddress);

        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(refreshToken.PlainTextToken);
        result.ExpiresAt.Should().Be(expiry);
        result.User.Id.Should().Be(user.Id);
        result.User.Username.Should().Be("testuser");
        result.User.Roles.Should().Equal("User");
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _loginAttemptsMock.Verify(x => x.IsLockedOut("testuser", ClientAddress), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("testuser"), Times.Once);
        _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(user, "password123", false), Times.Once);
        _loginAttemptsMock.Verify(x => x.Reset("testuser", ClientAddress), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(user, roles, refreshToken.SessionId), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateRefreshTokenAsync(user.Id), Times.Once);
        _tokenServiceMock.Verify(x => x.GetAccessTokenExpiry(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User testuser logged in");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenUserNotFound()
    {
        var request = new LoginRequest("unknownuser", "password123");

        _userManagerMock.Setup(x => x.FindByNameAsync("unknownuser"))
            .ReturnsAsync((ApplicationUser?)null);

        var act = () => _authService.LoginAsync(request, ClientAddress);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.InvalidCredentials);

        _loginAttemptsMock.Verify(x => x.IsLockedOut("unknownuser", ClientAddress), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("unknownuser"), Times.Once);
        _loginAttemptsMock.Verify(x => x.RecordFailure("unknownuser", ClientAddress), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "Failed login attempt for an unknown username from 203.0.113.7");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenPasswordIsWrong()
    {
        var request = new LoginRequest("testuser", "wrongpassword");
        var user = new ApplicationUser("testuser", "test@test.com");

        _userManagerMock.Setup(x => x.FindByNameAsync("testuser")).ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "wrongpassword", false))
            .ReturnsAsync(SignInResult.Failed);

        var act = () => _authService.LoginAsync(request, ClientAddress);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.InvalidCredentials);
        user.LastLoginAt.Should().BeNull();

        _loginAttemptsMock.Verify(x => x.IsLockedOut("testuser", ClientAddress), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("testuser"), Times.Once);
        _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(user, "wrongpassword", false), Times.Once);
        _loginAttemptsMock.Verify(x => x.RecordFailure("testuser", ClientAddress), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Failed login attempt for user {user.Id} from 203.0.113.7: invalid password");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginAsync_ShouldSayTheAccountIsDisabled_WhenTheDisabledUserHasTheRightPassword()
    {
        var request = new LoginRequest("testuser", "password123");
        var user = new ApplicationUser("testuser", "test@test.com");

        _userManagerMock.Setup(x => x.FindByNameAsync("testuser")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "password123")).ReturnsAsync(true);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "password123", false))
            .ReturnsAsync(SignInResult.LockedOut);

        var act = () => _authService.LoginAsync(request, ClientAddress);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.AccountDisabled);

        _loginAttemptsMock.Verify(x => x.IsLockedOut("testuser", ClientAddress), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("testuser"), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "password123"), Times.Once);
        _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(user, "password123", false), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Login attempt for disabled user {user.Id}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginAsync_ShouldTreatAWrongPasswordLikeAnyOther_WhenTheAccountIsDisabled()
    {
        var request = new LoginRequest("testuser", "wrong-password");
        var user = new ApplicationUser("testuser", "test@test.com");

        _userManagerMock.Setup(x => x.FindByNameAsync("testuser")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrong-password")).ReturnsAsync(false);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "wrong-password", false))
            .ReturnsAsync(SignInResult.LockedOut);

        var act = () => _authService.LoginAsync(request, ClientAddress);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.InvalidCredentials);
        user.LastLoginAt.Should().BeNull();

        _loginAttemptsMock.Verify(x => x.IsLockedOut("testuser", ClientAddress), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("testuser"), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "wrong-password"), Times.Once);
        _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(user, "wrong-password", false), Times.Once);
        _loginAttemptsMock.Verify(x => x.RecordFailure("testuser", ClientAddress), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Failed login attempt for user {user.Id} from 203.0.113.7: invalid password");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginAsync_ShouldRejectWithoutCheckingThePassword_WhenClientIsLockedOutForThatAccount()
    {
        var request = new LoginRequest("testuser", "password123");
        _loginAttemptsMock.Setup(x => x.IsLockedOut("testuser", ClientAddress)).Returns(true);

        var act = () => _authService.LoginAsync(request, ClientAddress);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.AccountLockedOut);

        _loginAttemptsMock.Verify(x => x.IsLockedOut("testuser", ClientAddress), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "Login attempt from 203.0.113.7 rejected: too many failures for this account");
        VerifyNoOtherCalls();
    }

    #endregion

    #region RefreshAsync

    [Fact]
    public async Task RefreshAsync_ShouldReturnNewLoginResponse_WhenTokenIsValid()
    {
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var activeToken = CreateActiveRefreshTokenWithUser(user.Id, user);
        var newRefreshToken = RefreshToken.Create(user.Id, 7);
        var roles = new List<string> { "User" };
        var accessToken = "new-access-token";
        var expiry = DateTime.UtcNow.AddHours(1);

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("valid-refresh-token"))
            .ReturnsAsync(activeToken);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
        _tokenServiceMock.Setup(x => x.RotateRefreshTokenAsync(activeToken)).ReturnsAsync(newRefreshToken);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, roles, newRefreshToken.SessionId)).Returns(accessToken);
        _tokenServiceMock.Setup(x => x.GetAccessTokenExpiry()).Returns(expiry);

        var result = await _authService.RefreshAsync("valid-refresh-token");

        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(newRefreshToken.PlainTextToken);
        result.ExpiresAt.Should().Be(expiry);

        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("valid-refresh-token"), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _tokenServiceMock.Verify(x => x.RotateRefreshTokenAsync(activeToken), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(user, roles, newRefreshToken.SessionId), Times.Once);
        _tokenServiceMock.Verify(x => x.GetAccessTokenExpiry(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldRefuseADisabledUser_AndRevokeTheirSessions()
    {
        var user = new ApplicationUser("testuser", "test@test.com", "Test User") { LockoutEnd = DateTimeOffset.MaxValue };
        var activeToken = CreateActiveRefreshTokenWithUser(user.Id, user);
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("valid-refresh-token")).ReturnsAsync(activeToken);

        var act = () => _authService.RefreshAsync("valid-refresh-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage(Constants.Errors.AccountDisabled);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("valid-refresh-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync(user.Id, "Account disabled"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Refresh refused for disabled user {user.Id}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowUnauthorizedAccessException_WhenTokenNotFound()
    {
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("nonexistent-token"))
            .ReturnsAsync((RefreshToken?)null);

        var act = () => _authService.RefreshAsync("nonexistent-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.InvalidRefreshToken);

        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("nonexistent-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowUnauthorizedAccessException_WhenTokenIsInactive()
    {
        var revokedToken = CreateRevokedRefreshToken("user-id-123");

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("revoked-token"))
            .ReturnsAsync(revokedToken);

        var act = () => _authService.RefreshAsync("revoked-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.InvalidRefreshToken);

        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("revoked-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldContinueTheSession_WhenAReplacedTokenComesBackAndItsSuccessorWasNeverUsed()
    {
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var presented = Replaced(CreateActiveRefreshTokenWithUser(user.Id, user), RefreshToken.RotatedReason);
        var unusedSuccessor = RefreshToken.Create(user.Id, 7);
        var fresh = RefreshToken.Create(user.Id, 7);
        var roles = new List<string> { "User" };
        var expiry = DateTime.UtcNow.AddMinutes(15);
        _dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(presented.RevokedAt!.Value.AddSeconds(60));
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("old-token")).ReturnsAsync(presented);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(presented)).ReturnsAsync(unusedSuccessor);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
        _tokenServiceMock.Setup(x => x.SupersedeRefreshTokenAsync(unusedSuccessor)).ReturnsAsync(fresh);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, roles, fresh.SessionId)).Returns("access");
        _tokenServiceMock.Setup(x => x.GetAccessTokenExpiry()).Returns(expiry);

        var result = await _authService.RefreshAsync("old-token");

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be(fresh.PlainTextToken);
        result.ExpiresAt.Should().Be(expiry);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("old-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(presented), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _tokenServiceMock.Verify(x => x.SupersedeRefreshTokenAsync(unusedSuccessor), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(user, roles, fresh.SessionId), Times.Once);
        _tokenServiceMock.Verify(x => x.GetAccessTokenExpiry(), Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"A replaced refresh token whose successor was never used was presented again for user {user.Id}; treating it as a lost response");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldFollowSupersededTokens_ToTheUnusedTipOfTheSession()
    {
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var presented = Replaced(CreateActiveRefreshTokenWithUser(user.Id, user), RefreshToken.SupersededReason);
        var superseded = Replaced(RefreshToken.Create(user.Id, 7), RefreshToken.SupersededReason);
        var tip = RefreshToken.Create(user.Id, 7);
        var fresh = RefreshToken.Create(user.Id, 7);
        var roles = new List<string> { "User" };
        _dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(presented.RevokedAt!.Value);
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("old-token")).ReturnsAsync(presented);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(presented)).ReturnsAsync(superseded);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(superseded)).ReturnsAsync(tip);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
        _tokenServiceMock.Setup(x => x.SupersedeRefreshTokenAsync(tip)).ReturnsAsync(fresh);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, roles, fresh.SessionId)).Returns("access");

        var result = await _authService.RefreshAsync("old-token");

        result.RefreshToken.Should().Be(fresh.PlainTextToken);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("old-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(presented), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(superseded), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _tokenServiceMock.Verify(x => x.SupersedeRefreshTokenAsync(tip), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(user, roles, fresh.SessionId), Times.Once);
        _tokenServiceMock.Verify(x => x.GetAccessTokenExpiry(), Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"A replaced refresh token whose successor was never used was presented again for user {user.Id}; treating it as a lost response");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldRevokeEverySession_WhenAReplacedTokenComesBackAfterItsSuccessorWasUsed()
    {
        var presented = Replaced(RefreshToken.Create("user-id-123", 7), RefreshToken.RotatedReason);
        var usedSuccessor = Replaced(RefreshToken.Create("user-id-123", 7), RefreshToken.RotatedReason);
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("old-token")).ReturnsAsync(presented);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(presented)).ReturnsAsync(usedSuccessor);

        var act = () => _authService.RefreshAsync("old-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage(Constants.Errors.InvalidRefreshToken);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("old-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(presented), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync("user-id-123", "Refresh token reuse detected"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "A replaced refresh token was presented again after its session moved on or its retry window closed for user user-id-123; revoking every session");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldRevokeEverySession_WhenALaterTokenOfTheSessionWasUsed()
    {
        var presented = Replaced(RefreshToken.Create("user-id-123", 7), RefreshToken.RotatedReason);
        var superseded = Replaced(RefreshToken.Create("user-id-123", 7), RefreshToken.SupersededReason);
        var used = Replaced(RefreshToken.Create("user-id-123", 7), RefreshToken.RotatedReason);
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("old-token")).ReturnsAsync(presented);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(presented)).ReturnsAsync(superseded);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(superseded)).ReturnsAsync(used);

        var act = () => _authService.RefreshAsync("old-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage(Constants.Errors.InvalidRefreshToken);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("old-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(presented), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(superseded), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync("user-id-123", "Refresh token reuse detected"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "A replaced refresh token was presented again after its session moved on or its retry window closed for user user-id-123; revoking every session");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldRefuseWithoutRevokingAnything_WhenTheSessionOfAReplacedTokenHasEnded()
    {
        var presented = Replaced(RefreshToken.Create("user-id-123", 7), RefreshToken.RotatedReason);
        var loggedOut = RefreshToken.Create("user-id-123", 7);
        loggedOut.Revoke("Logged out");
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("old-token")).ReturnsAsync(presented);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(presented)).ReturnsAsync(loggedOut);

        var act = () => _authService.RefreshAsync("old-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage(Constants.Errors.InvalidRefreshToken);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("old-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(presented), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "A replaced refresh token was presented for user user-id-123, but its session has ended");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldRefuse_WhenTheSuccessorIsGone()
    {
        var presented = Replaced(RefreshToken.Create("user-id-123", 7), RefreshToken.RotatedReason);
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("old-token")).ReturnsAsync(presented);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(presented)).ReturnsAsync((RefreshToken?)null);

        var act = () => _authService.RefreshAsync("old-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage(Constants.Errors.InvalidRefreshToken);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("old-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(presented), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "A replaced refresh token was presented for user user-id-123, but its session has ended");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldStop_WhenTheChainLoopsBackOnItself()
    {
        var presented = Replaced(RefreshToken.Create("user-id-123", 7), RefreshToken.SupersededReason);
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("old-token")).ReturnsAsync(presented);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(presented)).ReturnsAsync(presented);

        var act = () => _authService.RefreshAsync("old-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage(Constants.Errors.InvalidRefreshToken);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("old-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(presented), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "A replaced refresh token was presented for user user-id-123, but its session has ended");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldTellTheLoserToRetry_WhenTwoLostResponsesRaceOnOneSuccessor()
    {
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var presented = Replaced(CreateActiveRefreshTokenWithUser(user.Id, user), RefreshToken.RotatedReason);
        var unusedSuccessor = RefreshToken.Create(user.Id, 7);
        var roles = new List<string> { "User" };
        _dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(presented.RevokedAt!.Value);
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("old-token")).ReturnsAsync(presented);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(presented)).ReturnsAsync(unusedSuccessor);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
        _tokenServiceMock.Setup(x => x.SupersedeRefreshTokenAsync(unusedSuccessor)).ThrowsAsync(new DbUpdateConcurrencyException());

        var act = () => _authService.RefreshAsync("old-token");

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(Constants.Errors.RefreshTokenSuperseded);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("old-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(presented), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _tokenServiceMock.Verify(x => x.SupersedeRefreshTokenAsync(unusedSuccessor), Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"A replaced refresh token whose successor was never used was presented again for user {user.Id}; treating it as a lost response");
        _loggerMock.VerifyLog(LogLevel.Information, $"Two refreshes raced on one refresh token for user {user.Id}; only the first got a new token");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldRevokeEverySession_WhenAReplacedTokenComesBackAfterTheRetryWindow()
    {
        var presented = Replaced(RefreshToken.Create("user-id-123", 7), RefreshToken.RotatedReason);
        var unusedSuccessor = RefreshToken.Create("user-id-123", 7);
        _dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(presented.RevokedAt!.Value.AddSeconds(60).AddTicks(1));
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("replaced")).ReturnsAsync(presented);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(presented)).ReturnsAsync(unusedSuccessor);

        var act = () => _authService.RefreshAsync("replaced");

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(Constants.Errors.InvalidRefreshToken);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("replaced"), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(presented), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync("user-id-123", "Refresh token reuse detected"), Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "A replaced refresh token was presented again after its session moved on or its retry window closed for user user-id-123; revoking every session");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldRefuseADisabledUser_EvenWhenTheResponseWasLost()
    {
        var user = new ApplicationUser("testuser", "test@test.com", "Test User") { LockoutEnd = DateTimeOffset.MaxValue };
        var presented = Replaced(CreateActiveRefreshTokenWithUser(user.Id, user), RefreshToken.RotatedReason);
        var unusedSuccessor = RefreshToken.Create(user.Id, 7);
        _dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(presented.RevokedAt!.Value);
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("old-token")).ReturnsAsync(presented);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(presented)).ReturnsAsync(unusedSuccessor);

        var act = () => _authService.RefreshAsync("old-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage(Constants.Errors.AccountDisabled);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("old-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(presented), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync(user.Id, "Account disabled"), Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"A replaced refresh token whose successor was never used was presented again for user {user.Id}; treating it as a lost response");
        _loggerMock.VerifyLog(LogLevel.Warning, $"Refresh refused for disabled user {user.Id}");
        VerifyNoOtherCalls();
    }

    private static RefreshToken Replaced(RefreshToken token, string reason)
    {
        token.Revoke(reason, RefreshToken.Hash(Guid.NewGuid().ToString("N")));
        return token;
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowUnauthorizedAccessException_WhenTokenIsExpired()
    {
        var expiredToken = RefreshToken.Create("user-id-123", -1);

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("expired-token"))
            .ReturnsAsync(expiredToken);

        var act = () => _authService.RefreshAsync("expired-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.InvalidRefreshToken);

        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("expired-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region LogoutAsync

    [Fact]
    public async Task LogoutAsync_ShouldRevokeSpecificToken_WhenRefreshTokenProvided()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com");
        var token = CreateActiveRefreshTokenWithUser(userId, user);

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("my-refresh-token"))
            .ReturnsAsync(token);
        _tokenServiceMock.Setup(x => x.RevokeRefreshTokenAsync(token, "Logged out"))
            .Returns(Task.CompletedTask);

        await _authService.LogoutAsync(userId, "my-refresh-token");

        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("my-refresh-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeRefreshTokenAsync(token, "Logged out"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 logged out");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("user-id-123", null)]
    [InlineData("user-id-123", "")]
    [InlineData(null, null)]
    public async Task LogoutAsync_ShouldRevokeNothing_WhenThereIsNoRefreshToken(string? userId, string? refreshToken)
    {
        await _authService.LogoutAsync(userId, refreshToken);

        _tokenServiceMock.Invocations.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeTheCookieToken_WhenTheCallerIsAnonymous()
    {
        var user = new ApplicationUser("testuser", "test@test.com");
        var token = CreateActiveRefreshTokenWithUser("user-id-123", user);

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("cookie-token")).ReturnsAsync(token);
        _tokenServiceMock.Setup(x => x.RevokeRefreshTokenAsync(token, "Logged out")).Returns(Task.CompletedTask);

        await _authService.LogoutAsync(null, "cookie-token");

        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("cookie-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeRefreshTokenAsync(token, "Logged out"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 logged out");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotRevokeToken_WhenTokenBelongsToDifferentUser()
    {
        var userId = "user-id-123";
        var differentUserId = "user-id-456";
        var user = new ApplicationUser("otheruser", "other@test.com");
        var token = CreateActiveRefreshTokenWithUser(differentUserId, user);

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("other-users-token"))
            .ReturnsAsync(token);

        await _authService.LogoutAsync(userId, "other-users-token");

        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("other-users-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotRevokeToken_WhenTokenNotFound()
    {
        var userId = "user-id-123";

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("missing-token"))
            .ReturnsAsync((RefreshToken?)null);

        await _authService.LogoutAsync(userId, "missing-token");

        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("missing-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region RegisterAsync

    [Theory]
    [InlineData(null, Constants.AuthRoles.User)]
    [InlineData(Constants.AuthRoles.Reader, Constants.AuthRoles.Reader)]
    [InlineData(Constants.AuthRoles.Admin, Constants.AuthRoles.Admin)]
    public async Task RegisterAsync_ShouldReturnUserDto_WhenRegistrationSucceeds(string? requestedRole, string expectedRole)
    {
        var request = new RegisterRequest("newuser", "new@test.com", "password123", requestedRole);
        var roles = new List<string> { expectedRole };

        _userManagerMock.Setup(x => x.FindByNameAsync("newuser"))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "newuser", "new@test.com")), "password123"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser"), expectedRole))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser")))
            .ReturnsAsync(roles);

        var result = await _authService.RegisterAsync(request);

        result.Username.Should().Be("newuser");
        result.Email.Should().Be("new@test.com");
        result.DisplayName.Should().Be("newuser");
        result.Roles.Should().Equal(expectedRole);

        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "newuser", "new@test.com")), "password123"), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser"), expectedRole), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser")), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"Admin created new user: newuser with role {expectedRole}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateALocalUser_WhileAnOidcProviderIsEnabled()
    {
        _context.OidcProviders.Add(new OidcProvider("Google", "https://accounts.google.com", "client-id"));
        await _context.SaveChangesAsync();
        var request = new RegisterRequest("kid", "kid@test.com", "password123", Constants.AuthRoles.Reader);
        _userManagerMock.Setup(x => x.FindByNameAsync("kid")).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "kid", "kid@test.com")), "password123")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "kid"), Constants.AuthRoles.Reader)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == "kid"))).ReturnsAsync([Constants.AuthRoles.Reader]);

        var result = await _authService.RegisterAsync(request);

        result.Username.Should().Be("kid");
        _userManagerMock.Verify(x => x.FindByNameAsync("kid"), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "kid", "kid@test.com")), "password123"), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "kid"), Constants.AuthRoles.Reader), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == "kid")), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Admin created new user: kid with role Reader");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowEntityNotFoundException_WhenLinkedPlayerDoesNotExist()
    {
        var request = new RegisterRequest("newuser", "new@test.com", "password123", null, PlayerId: 99);

        _userManagerMock.Setup(x => x.FindByNameAsync("newuser"))
            .ReturnsAsync((ApplicationUser?)null);

        var act = () => _authService.RegisterAsync(request);

        await act.Should().ThrowAsync<EntityNotFoundException>().Where(e => e.EntityType == nameof(Player) && e.EntityId.Equals(99));

        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowValidationException_WhenRoleIsInvalid()
    {
        var request = new RegisterRequest("newuser", "new@test.com", "password123", "InvalidRole");

        _userManagerMock.Setup(x => x.FindByNameAsync("newuser"))
            .ReturnsAsync((ApplicationUser?)null);

        var act = () => _authService.RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.InvalidRole);

        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowDomainException_WhenUsernameAlreadyExists()
    {
        var request = new RegisterRequest("existinguser", "new@test.com", "password123", null);
        var existingUser = new ApplicationUser("existinguser", "existing@test.com");

        _userManagerMock.Setup(x => x.FindByNameAsync("existinguser"))
            .ReturnsAsync(existingUser);

        var act = () => _authService.RegisterAsync(request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(Constants.Errors.UsernameAlreadyExists);

        _userManagerMock.Verify(x => x.FindByNameAsync("existinguser"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowValidationException_WhenCreationFails()
    {
        var request = new RegisterRequest("newuser", "new@test.com", "weak", null);
        var identityErrors = new[] { new IdentityError { Description = "Password too short" }, new IdentityError { Description = "Username invalid" } };

        _userManagerMock.Setup(x => x.FindByNameAsync("newuser"))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "newuser", "new@test.com")), "weak"))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var act = () => _authService.RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Password too short, Username invalid");

        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "newuser", "new@test.com")), "weak"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetProfileAsync

    [Fact]
    public async Task GetProfileAsync_ShouldReturnProfileResponse_WhenUserExists()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var roles = new List<string> { "User" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);

        var result = await _authService.GetProfileAsync(userId);

        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@test.com");
        result.DisplayName.Should().Be("Test User");
        result.Roles.Should().Equal("User");

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetProfileAsync_ShouldThrowEntityNotFoundException_WhenUserNotFound()
    {
        var userId = "nonexistent-user-id";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var act = () => _authService.GetProfileAsync(userId);

        await act.Should().ThrowAsync<EntityNotFoundException>().Where(e => e.EntityType == nameof(ApplicationUser) && e.EntityId.Equals(userId));

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateProfileAsync

    [Fact]
    public async Task UpdateProfileAsync_ShouldReturnUpdatedProfile_WhenEmailIsProvided()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "old@test.com", "Old Name");
        var request = new UpdateProfileRequest("New Name", "new@test.com", null, "current-password");
        var roles = new List<string> { "User" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        var result = await _authService.UpdateProfileAsync(userId, request);

        result.DisplayName.Should().Be("New Name");
        result.Email.Should().Be("new@test.com");
        user.EmailConfirmed.Should().BeFalse();

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "current-password"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("old@test.com", EmailChangedSubject, EmailChangedBody("testuser", "was changed to new@test.com")), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-password")]
    public async Task UpdateProfileAsync_ShouldRefuseAnEmailChange_WithoutTheRightCurrentPassword(string? currentPassword)
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "old@test.com", "Old Name");
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrong-password")).ReturnsAsync(false);

        var act = () => _authService.UpdateProfileAsync(userId, new UpdateProfileRequest("Old Name", "attacker@test.com", null, currentPassword));

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.CurrentPasswordRequired);
        user.Email.Should().Be("old@test.com");
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "wrong-password"), Times.Exactly(currentPassword == "wrong-password" ? 1 : 0));
        _loggerMock.VerifyLog(LogLevel.Warning, $"Email change for user {user.Id} refused: current password missing or wrong");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldRefuseAnEmailChange_ForAnAccountWithoutAPassword()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("ssouser", "old@test.com", "Old Name");
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(false);

        var act = () => _authService.UpdateProfileAsync(userId, new UpdateProfileRequest("Old Name", "attacker@test.com", null));

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.EmailChangeNeedsPassword);
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldNotAskForThePassword_WhenTheEmailOnlyChangesCase()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "old@test.com", "Old Name");
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        var result = await _authService.UpdateProfileAsync(userId, new UpdateProfileRequest("New Name", "OLD@test.com", null));

        result.DisplayName.Should().Be("New Name");
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateProfileAsync_ShouldClearTheEmail_WhenItIsEmptied(string email)
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "old@test.com", "Old Name");
        user.ConfirmEmail();
        var request = new UpdateProfileRequest("Old Name", email, null, "current-password");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        var result = await _authService.UpdateProfileAsync(userId, request);

        result.Email.Should().BeNull();
        user.EmailConfirmed.Should().BeFalse();

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "current-password"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("old@test.com", EmailChangedSubject, EmailChangedBody("testuser", "was removed")), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldRefuseToClearTheEmail_WithoutTheCurrentPassword()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "old@test.com", "Old Name");
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);

        var act = () => _authService.UpdateProfileAsync(userId, new UpdateProfileRequest("Old Name", "", null));

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.CurrentPasswordRequired);
        user.Email.Should().Be("old@test.com");
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Email change for user {user.Id} refused: current password missing or wrong");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldNotAskForThePassword_WhenAnAccountWithoutEmailStaysWithoutEmail()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", null, "Old Name");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        var result = await _authService.UpdateProfileAsync(userId, new UpdateProfileRequest("New Name", "", null));

        result.DisplayName.Should().Be("New Name");
        result.Email.Should().BeNull();

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldReturnUpdatedProfile_WhenEmailIsNotProvided()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "original@test.com", "Old Name");
        var request = new UpdateProfileRequest("New Name", null, null);
        var roles = new List<string> { "User" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);

        var result = await _authService.UpdateProfileAsync(userId, request);

        result.DisplayName.Should().Be("New Name");
        result.Email.Should().Be("original@test.com");

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldThrowEntityNotFoundException_WhenUserNotFound()
    {
        var userId = "nonexistent-user-id";
        var request = new UpdateProfileRequest("New Name", null, null);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var act = () => _authService.UpdateProfileAsync(userId, request);

        await act.Should().ThrowAsync<EntityNotFoundException>().Where(e => e.EntityType == nameof(ApplicationUser) && e.EntityId.Equals(userId));

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region PlayerLinking

    [Fact]
    public async Task RegisterAsync_ShouldCreateAndLinkPlayer_WhenCreatePlayerIsTrue()
    {
        var request = new RegisterRequest("newuser", "new@test.com", "password123", null, CreatePlayer: true);
        _userManagerMock.Setup(x => x.FindByNameAsync("newuser")).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "newuser", "new@test.com")), "password123")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser"), Constants.AuthRoles.User)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser" && u.PlayerId == 5))).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser"))).ReturnsAsync(new List<string> { "User" });
        _playerServiceMock.Setup(x => x.Create(It.Is<CreatePlayerCommand>(c => c.Name == "newuser" && c.Email == "new@test.com"))).ReturnsAsync(new Player("newuser") { Id = 5 });

        var result = await _authService.RegisterAsync(request);

        result.PlayerId.Should().Be(5);

        _playerServiceMock.Verify(x => x.Create(It.Is<CreatePlayerCommand>(c => c.Name == "newuser" && c.Email == "new@test.com")), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "newuser", "new@test.com")), "password123"), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser"), Constants.AuthRoles.User), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser" && u.PlayerId == 5)), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser")), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ApplyAccountImageAsync(It.Is<ApplicationUser>(u => u.PlayerId == 5)), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Admin created new user: newuser with role User");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldLinkExistingPlayer_WhenPlayerIdProvided()
    {
        _context.Players.Add(new Player("Existing") { Id = 7 });
        await _context.SaveChangesAsync();

        var request = new RegisterRequest("newuser", "new@test.com", "password123", null, PlayerId: 7);
        _userManagerMock.Setup(x => x.FindByNameAsync("newuser")).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "newuser", "new@test.com")), "password123")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser"), Constants.AuthRoles.User)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser" && u.PlayerId == 7))).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser"))).ReturnsAsync(new List<string> { "User" });

        var result = await _authService.RegisterAsync(request);

        result.PlayerId.Should().Be(7);

        _playerAccountImageServiceMock.Verify(x => x.ApplyAccountImageAsync(It.Is<ApplicationUser>(u => u.PlayerId == 7)), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "newuser", "new@test.com")), "password123"), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser"), Constants.AuthRoles.User), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser" && u.PlayerId == 7)), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser")), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Admin created new user: newuser with role User");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldNotCreateUser_WhenLinkedPlayerAlreadyTaken()
    {
        _context.Players.Add(new Player("Taken") { Id = 8 });
        var otherUser = new ApplicationUser("other", "o@test.com");
        otherUser.LinkPlayer(8);
        _context.Users.Add(otherUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest("newuser", "new@test.com", "password123", null, PlayerId: 8);
        _userManagerMock.Setup(x => x.FindByNameAsync("newuser")).ReturnsAsync((ApplicationUser?)null);

        var act = () => _authService.RegisterAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.PlayerAlreadyLinked);

        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldLinkPlayer_WhenPlayerIdProvided()
    {
        _context.Players.Add(new Player("Target") { Id = 3 });
        await _context.SaveChangesAsync();

        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com", "Name");
        var request = new UpdateProfileRequest("Name", null, 3);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        var result = await _authService.UpdateProfileAsync(userId, request);

        result.PlayerId.Should().Be(3);
        user.PlayerId.Should().Be(3);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, user), Times.Once);

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldUnlinkPlayer_WhenPlayerIdIsNull()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com", "Name");
        user.LinkPlayer(3);
        var request = new UpdateProfileRequest("Name", null, null);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        var result = await _authService.UpdateProfileAsync(userId, request);

        result.PlayerId.Should().BeNull();
        user.PlayerId.Should().BeNull();
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(3, user), Times.Once);

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldThrowDomainException_WhenPlayerLinkedToAnotherUser()
    {
        _context.Players.Add(new Player("Target") { Id = 3 });
        var otherUser = new ApplicationUser("other", "other@test.com");
        otherUser.LinkPlayer(3);
        _context.Users.Add(otherUser);
        await _context.SaveChangesAsync();

        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com", "Name");
        var request = new UpdateProfileRequest("Name", null, 3);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        var act = () => _authService.UpdateProfileAsync(userId, request);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.PlayerAlreadyLinked);

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldRefuseLinkingAPlayerWithAnotherEmail()
    {
        _context.Players.Add(new Player("Target", email: "someone-else@test.com") { Id = 3 });
        await _context.SaveChangesAsync();

        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com", "Name");
        var request = new UpdateProfileRequest("Name", null, 3);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        var act = () => _authService.UpdateProfileAsync(userId, request);

        (await act.Should().ThrowAsync<ForbiddenException>()).WithMessage(Constants.Errors.PlayerEmailMismatch);
        user.PlayerId.Should().BeNull();

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"User {user.Id} tried to link player 3 with a different email");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldLinkAPlayerWithTheSameEmail()
    {
        _context.Players.Add(new Player("Target", email: " TEST@test.com ") { Id = 3 });
        await _context.SaveChangesAsync();

        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com", "Name");
        user.ConfirmEmail();
        var request = new UpdateProfileRequest("Name", null, 3);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        var result = await _authService.UpdateProfileAsync(userId, request);

        result.PlayerId.Should().Be(3);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, user), Times.Once);
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldRefuseLinkingAPlayerWithTheSameEmail_WhenTheEmailIsNotConfirmed()
    {
        _context.Players.Add(new Player("Target", email: "test@test.com") { Id = 3 });
        await _context.SaveChangesAsync();

        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com", "Name");
        var request = new UpdateProfileRequest("Name", null, 3);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        var act = () => _authService.UpdateProfileAsync(userId, request);

        (await act.Should().ThrowAsync<ForbiddenException>()).WithMessage(Constants.Errors.PlayerEmailMismatch);
        user.PlayerId.Should().BeNull();

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"User {user.Id} tried to link player 3 with a different email");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldRefuseLinkingAPlayer_WithAnEmailChangedInTheSameRequest()
    {
        _context.Players.Add(new Player("Alice", email: "alice@test.com") { Id = 3 });
        await _context.SaveChangesAsync();

        var userId = "user-id-123";
        var user = new ApplicationUser("bob", "bob@test.com", "Bob");
        user.ConfirmEmail();
        var request = new UpdateProfileRequest("Bob", "alice@test.com", 3, "current-password");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password")).ReturnsAsync(true);

        var act = () => _authService.UpdateProfileAsync(userId, request);

        (await act.Should().ThrowAsync<ForbiddenException>()).WithMessage(Constants.Errors.PlayerEmailMismatch);
        user.PlayerId.Should().BeNull();
        user.EmailConfirmed.Should().BeFalse();

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "current-password"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"User {user.Id} tried to link player 3 with a different email");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldRefuseLinkingAPlayer_AfterTheEmailWasChangedInAnEarlierRequest()
    {
        _context.Players.Add(new Player("Alice", email: "alice@test.com") { Id = 3 });
        await _context.SaveChangesAsync();

        var userId = "user-id-123";
        var user = new ApplicationUser("bob", "bob@test.com", "Bob");
        user.ConfirmEmail();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(false);

        await _authService.UpdateProfileAsync(userId, new UpdateProfileRequest("Bob", "alice@test.com", null, "current-password"));
        var act = () => _authService.UpdateProfileAsync(userId, new UpdateProfileRequest("Bob", null, 3));

        (await act.Should().ThrowAsync<ForbiddenException>()).WithMessage(Constants.Errors.PlayerEmailMismatch);
        user.PlayerId.Should().BeNull();

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Exactly(2));
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "current-password"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        _loggerMock.VerifyLog(LogLevel.Warning, $"User {user.Id} tried to link player 3 with a different email");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldThrowEntityNotFoundException_WhenLinkedPlayerDoesNotExist()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com", "Name");
        var request = new UpdateProfileRequest("Name", null, 99);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        var act = () => _authService.UpdateProfileAsync(userId, request);

        await act.Should().ThrowAsync<EntityNotFoundException>().Where(e => e.EntityType == nameof(Player) && e.EntityId.Equals(99));

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLinkablePlayersAsync_ShouldExcludePlayersLinkedToOtherUsers()
    {
        _context.Players.Add(new Player("Alice") { Id = 1 });
        _context.Players.Add(new Player("Bob") { Id = 2 });
        var linkedUser = new ApplicationUser("bobuser", "bob@test.com");
        linkedUser.LinkPlayer(2);
        _context.Users.Add(linkedUser);
        await _context.SaveChangesAsync();

        var result = await _authService.GetLinkablePlayersAsync("current-user");

        result.Should().Equal(new PlayerLinkDto(1, "Alice"));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLinkablePlayersAsync_ShouldIncludeCallersOwnLinkedPlayer()
    {
        _context.Players.Add(new Player("Alice") { Id = 1 });
        _context.Players.Add(new Player("Mine") { Id = 9 });
        var caller = new ApplicationUser("me", "me@test.com");
        caller.LinkPlayer(9);
        _context.Users.Add(caller);
        await _context.SaveChangesAsync();

        var result = await _authService.GetLinkablePlayersAsync(caller.Id);

        result.Should().Equal(new PlayerLinkDto(1, "Alice"), new PlayerLinkDto(9, "Mine"));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLinkablePlayersAsync_ShouldHidePlayersWithAnotherEmail_ButKeepTheCallersOwnPlayer()
    {
        _context.Players.Add(new Player("Alice", email: "alice@test.com") { Id = 1 });
        _context.Players.Add(new Player("Mine", email: "old@test.com") { Id = 9 });
        _context.Players.Add(new Player("Same", email: "ME@test.com") { Id = 4 });
        _context.Players.Add(new Player("NoEmail") { Id = 5 });
        var caller = new ApplicationUser("me", "me@test.com");
        caller.ConfirmEmail();
        caller.LinkPlayer(9);
        _context.Users.Add(caller);
        await _context.SaveChangesAsync();

        var result = await _authService.GetLinkablePlayersAsync(caller.Id);

        result.Select(p => p.Id).Should().Equal(9, 5, 4);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLinkablePlayersAsync_ShouldHidePlayersWithTheSameEmail_WhenTheCallersEmailIsNotConfirmed()
    {
        _context.Players.Add(new Player("Same", email: "me@test.com") { Id = 4 });
        _context.Players.Add(new Player("NoEmail") { Id = 5 });
        var caller = new ApplicationUser("me", "me@test.com");
        _context.Users.Add(caller);
        await _context.SaveChangesAsync();

        var result = await _authService.GetLinkablePlayersAsync(caller.Id);

        result.Select(p => p.Id).Should().Equal(5);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ForgotPassword / ResetPasswordWithToken

    [Fact]
    public async Task ForgotPasswordAsync_ShouldDoNothing_WhenUserNotFound()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("ghost")).ReturnsAsync((ApplicationUser?)null);

        await _authService.ForgotPasswordAsync("ghost");

        _userManagerMock.Verify(x => x.FindByNameAsync("ghost"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Forgot-password requested for an unknown account");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ForgotPasswordAsync_ShouldDoNothing_WhenUserHasNoEmail(string? email)
    {
        var user = new ApplicationUser("nomail", email);
        _userManagerMock.Setup(x => x.FindByNameAsync("nomail")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);

        await _authService.ForgotPasswordAsync("nomail");

        _userManagerMock.Verify(x => x.FindByNameAsync("nomail"), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Forgot-password requested for user {user.Id} but no email address is set");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldNotSend_WhenEmailNotConfigured()
    {
        var user = new ApplicationUser("user", "u@test.com");
        _userManagerMock.Setup(x => x.FindByNameAsync("user")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(false);

        await _authService.ForgotPasswordAsync("user");

        _userManagerMock.Verify(x => x.FindByNameAsync("user"), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "Forgot-password requested but email is not configured");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldGenerateTokenAndSendEmail_WhenConfigured()
    {
        var user = new ApplicationUser("user", "u@test.com");
        _userManagerMock.Setup(x => x.FindByNameAsync("user")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        _publicUrlBuilderMock.Setup(x => x.BuildResetUrlAsync(user.Id, "reset-token")).ReturnsAsync("http://x/reset?user=1&token=abc");
        await _authService.ForgotPasswordAsync("user");

        _userManagerMock.Verify(x => x.FindByNameAsync("user"), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildResetUrlAsync(user.Id, "reset-token"), Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("u@test.com", ResetSubject, ResetBody("http://x/reset?user=1&amp;token=abc")), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"Queued password reset email for user {user.Id}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldSendNothing_WhenThePublicUrlIsNotConfigured()
    {
        var user = new ApplicationUser("user", "u@test.com");
        _userManagerMock.Setup(x => x.FindByNameAsync("user")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        _publicUrlBuilderMock
            .Setup(x => x.BuildResetUrlAsync(user.Id, "reset-token"))
            .ThrowsAsync(new DomainException(Constants.Errors.PublicUrlNotConfigured));

        var act = () => _authService.ForgotPasswordAsync("user");

        await act.Should().NotThrowAsync();
        _userManagerMock.Verify(x => x.FindByNameAsync("user"), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildResetUrlAsync(user.Id, "reset-token"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "Password reset email not sent because no public URL is configured");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldSendOneEmailPerCooldown_WhenRequestedRepeatedly()
    {
        var user = new ApplicationUser("user", "u@test.com");
        _userManagerMock.Setup(x => x.FindByNameAsync("user")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        _publicUrlBuilderMock.Setup(x => x.BuildResetUrlAsync(user.Id, "reset-token")).ReturnsAsync("http://x/reset");

        await _authService.ForgotPasswordAsync("user");
        await _authService.ForgotPasswordAsync("user");
        await _authService.ForgotPasswordAsync("user");

        _userManagerMock.Verify(x => x.FindByNameAsync("user"), Times.Exactly(3));
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Exactly(3));
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(3));
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildResetUrlAsync(user.Id, "reset-token"), Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("u@test.com", ResetSubject, ResetBody("http://x/reset")), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"Queued password reset email for user {user.Id}");
        _loggerMock.VerifyLog(LogLevel.Information, $"Forgot-password requested again for user {user.Id} within the cooldown; no email sent", Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldNotThrow_WhenQueueingFails()
    {
        var user = new ApplicationUser("user", "u@test.com");
        var failure = new InvalidOperationException("queue down");
        _userManagerMock.Setup(x => x.FindByNameAsync("user")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        _publicUrlBuilderMock.Setup(x => x.BuildResetUrlAsync(user.Id, "reset-token")).ReturnsAsync("http://x/reset");
        _backgroundEmailSenderMock
            .Setup(x => x.Queue("u@test.com", ResetSubject, ResetBody("http://x/reset")))
            .Throws(failure);

        var act = () => _authService.ForgotPasswordAsync("user");

        await act.Should().NotThrowAsync();
        await act.Should().NotThrowAsync();

        _userManagerMock.Verify(x => x.FindByNameAsync("user"), Times.Exactly(2));
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Exactly(2));
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(2));
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Exactly(2));
        _publicUrlBuilderMock.Verify(x => x.BuildResetUrlAsync(user.Id, "reset-token"), Times.Exactly(2));
        _backgroundEmailSenderMock.Verify(x => x.Queue("u@test.com", ResetSubject, ResetBody("http://x/reset")), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Warning, "Failed to queue password reset email", failure, Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_ShouldSucceed_WhenTokenValid()
    {
        var user = new ApplicationUser("user", "u@test.com");
        var request = new ResetPasswordConfirmRequest("user-id", "token", "newpass");
        _userManagerMock.Setup(x => x.FindByIdAsync("user-id")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "token", "newpass")).ReturnsAsync(IdentityResult.Success);

        await _authService.ResetPasswordWithTokenAsync(request);

        _userManagerMock.Verify(x => x.FindByIdAsync("user-id"), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(user, "token", "newpass"), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync(user.Id, "Password reset"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"User {user.Id} reset their password via emailed token");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldSendNothing_WhenTheAccountHasNoLocalPassword()
    {
        var user = new ApplicationUser("sso-user", "sso@test.com");
        _userManagerMock.Setup(x => x.FindByNameAsync("sso-user")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(false);

        await _authService.ForgotPasswordAsync("sso-user");

        _userManagerMock.Verify(x => x.FindByNameAsync("sso-user"), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Forgot-password requested for an account without a local password; no email sent");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_ShouldThrow_WhenTheAccountHasNoLocalPassword()
    {
        var user = new ApplicationUser("sso-user", "sso@test.com");
        var request = new ResetPasswordConfirmRequest("user-id", "token", "newpass");
        _userManagerMock.Setup(x => x.FindByIdAsync("user-id")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(false);

        var act = () => _authService.ResetPasswordWithTokenAsync(request);

        (await act.Should().ThrowAsync<ValidationException>()).WithMessage(Constants.Errors.InvalidResetToken);

        _userManagerMock.Verify(x => x.FindByIdAsync("user-id"), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_ShouldThrow_WhenUserNotFound()
    {
        var request = new ResetPasswordConfirmRequest("ghost", "token", "newpass");
        _userManagerMock.Setup(x => x.FindByIdAsync("ghost")).ReturnsAsync((ApplicationUser?)null);

        var act = () => _authService.ResetPasswordWithTokenAsync(request);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidResetToken);

        _userManagerMock.Verify(x => x.FindByIdAsync("ghost"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_ShouldThrow_WhenResetFails()
    {
        var user = new ApplicationUser("user", "u@test.com");
        var request = new ResetPasswordConfirmRequest("user-id", "bad-token", "newpass");
        _userManagerMock.Setup(x => x.FindByIdAsync("user-id")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock
            .Setup(x => x.ResetPasswordAsync(user, "bad-token", "newpass"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        var act = () => _authService.ResetPasswordWithTokenAsync(request);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidResetToken);

        _userManagerMock.Verify(x => x.FindByIdAsync("user-id"), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(user, "bad-token", "newpass"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "Failed password reset attempt for user user-id");
        VerifyNoOtherCalls();
    }

    #endregion

    #region ChangePasswordAsync

    [Fact]
    public async Task ChangePasswordAsync_ShouldComplete_WhenPasswordChangedSuccessfully()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com");
        var request = new ChangePasswordRequest("oldpassword", "newpassword");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, "oldpassword", "newpassword"))
            .ReturnsAsync(IdentityResult.Success);

        await _authService.ChangePasswordAsync(userId, request);

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ChangePasswordAsync(user, "oldpassword", "newpassword"), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync(userId, "Password changed"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "User user-id-123 changed their password");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrowEntityNotFoundException_WhenUserNotFound()
    {
        var userId = "nonexistent-user-id";
        var request = new ChangePasswordRequest("oldpassword", "newpassword");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var act = () => _authService.ChangePasswordAsync(userId, request);

        await act.Should().ThrowAsync<EntityNotFoundException>().Where(e => e.EntityType == nameof(ApplicationUser) && e.EntityId.Equals(userId));

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrowDomainException_WhenUserHasNoPassword()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("oidcuser", "oidc@test.com");
        var request = new ChangePasswordRequest("oldpassword", "newpassword");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(false);

        var act = () => _authService.ChangePasswordAsync(userId, request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(Constants.Errors.CannotChangeOidcPassword);

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrowValidationException_WhenPasswordChangeFails()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com");
        var request = new ChangePasswordRequest("wrongpassword", "newpassword");
        var identityErrors = new[] { new IdentityError { Description = "Incorrect password" }, new IdentityError { Description = "Too short" } };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, "wrongpassword", "newpassword"))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var act = () => _authService.ChangePasswordAsync(userId, request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Incorrect password, Too short");

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ChangePasswordAsync(user, "wrongpassword", "newpassword"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "Failed password change attempt for user user-id-123");
        VerifyNoOtherCalls();
    }

    #endregion

    #region ResetPasswordAsync

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnResetPasswordResponse_WhenResetSucceeds()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com");
        var resetToken = "generated-reset-token";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, resetToken, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.ResetPasswordAsync(userId);

        result.TempPassword.Should().MatchRegex("^[a-zA-Z0-9!@#$%]{16}$");

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(user, resetToken, result.TempPassword), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync(userId, "Password reset by admin"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Admin reset password for user testuser");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrowEntityNotFoundException_WhenUserNotFound()
    {
        var userId = "nonexistent-user-id";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var act = () => _authService.ResetPasswordAsync(userId);

        await act.Should().ThrowAsync<EntityNotFoundException>().Where(e => e.EntityType == nameof(ApplicationUser) && e.EntityId.Equals(userId));

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrowValidationException_WhenResetFails()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com");
        var resetToken = "generated-reset-token";
        var identityErrors = new[] { new IdentityError { Description = "Password reset failed" } };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, resetToken, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var act = () => _authService.ResetPasswordAsync(userId);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Password reset failed");

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(user, resetToken, It.Is<string>(p => p.Length == 16)), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetStatus

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetStatus_ShouldReflectEnvironmentAuthSetting(bool authEnabled)
    {
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(authEnabled);

        var result = _authService.GetStatus();

        result.AuthEnabled.Should().Be(authEnabled);

        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    private static RefreshToken CreateActiveRefreshTokenWithUser(string userId, ApplicationUser user)
    {
        var token = RefreshToken.Create(userId, 7);
        var userProperty = typeof(RefreshToken).GetProperty(
            "User",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        userProperty!.SetValue(token, user);
        return token;
    }

    private static RefreshToken CreateRevokedRefreshToken(string userId)
    {
        var token = RefreshToken.Create(userId, 7);
        token.Revoke("Test revocation");
        return token;
    }

    [Fact]
    public async Task RefreshAsync_ShouldRejectTheLoser_WhenTwoRefreshesRaceOnOneToken()
    {
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var activeToken = CreateActiveRefreshTokenWithUser(user.Id, user);
        var roles = new List<string> { "User" };

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("valid-refresh-token")).ReturnsAsync(activeToken);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
        _tokenServiceMock.Setup(x => x.RotateRefreshTokenAsync(activeToken)).ThrowsAsync(new DbUpdateConcurrencyException());

        var act = () => _authService.RefreshAsync("valid-refresh-token");

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(Constants.Errors.RefreshTokenSuperseded);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("valid-refresh-token"), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _tokenServiceMock.Verify(x => x.RotateRefreshTokenAsync(activeToken), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"Two refreshes raced on one refresh token for user {user.Id}; only the first got a new token");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldStillSave_WhenTheEmailChangeNoticeCannotBeQueued()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "old@test.com", "Old Name");
        var request = new UpdateProfileRequest("New Name", "new@test.com", null, "current-password");
        var roles = new List<string> { "User" };
        var failure = new InvalidOperationException("queue unavailable");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _backgroundEmailSenderMock
            .Setup(x => x.Queue("old@test.com", EmailChangedSubject, EmailChangedBody("testuser", "was changed to new@test.com")))
            .Throws(failure);

        var result = await _authService.UpdateProfileAsync(userId, request);

        result.Email.Should().Be("new@test.com");
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, user), Times.Once);
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "current-password"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("old@test.com", EmailChangedSubject, EmailChangedBody("testuser", "was changed to new@test.com")), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Failed to queue the email-change notice for user {user.Id}", failure);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldRefuseWithoutLookingFurther_WhenAReplacedTokenHasExpired()
    {
        var presented = Replaced(RefreshToken.Create("user-id-123", -1), RefreshToken.RotatedReason);
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("expired-token")).ReturnsAsync(presented);

        var act = () => _authService.RefreshAsync("expired-token");

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(Constants.Errors.InvalidRefreshToken);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("expired-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldRefuseWithoutRevokingAnything_WhenTheSuccessorHasExpiredUnused()
    {
        var presented = Replaced(RefreshToken.Create("user-id-123", 7), RefreshToken.RotatedReason);
        var expired = RefreshToken.Create("user-id-123", -1);
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("old-token")).ReturnsAsync(presented);
        _tokenServiceMock.Setup(x => x.GetSuccessorAsync(presented)).ReturnsAsync(expired);

        var act = () => _authService.RefreshAsync("old-token");

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(Constants.Errors.InvalidRefreshToken);
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("old-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.GetSuccessorAsync(presented), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "A replaced refresh token was presented for user user-id-123, but its session has ended");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateAPlayer_AndIgnoreThePlayerId_WhenBothAreGiven()
    {
        var request = new RegisterRequest("newuser", "new@test.com", "password123", null, PlayerId: 99, CreatePlayer: true);
        _userManagerMock.Setup(x => x.FindByNameAsync("newuser")).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "newuser", "new@test.com")), "password123")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser"), Constants.AuthRoles.User)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.PlayerId == 5))).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser"))).ReturnsAsync([Constants.AuthRoles.User]);
        _playerServiceMock.Setup(x => x.Create(It.Is<CreatePlayerCommand>(c => c.Name == "newuser" && c.Email == "new@test.com"))).ReturnsAsync(new Player("newuser") { Id = 5 });

        var result = await _authService.RegisterAsync(request);

        result.PlayerId.Should().Be(5);
        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => IsNewAccount(u, "newuser", "new@test.com")), "password123"), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser"), Constants.AuthRoles.User), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.PlayerId == 5)), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.UserName == "newuser")), Times.Once);
        _playerServiceMock.Verify(x => x.Create(It.Is<CreatePlayerCommand>(c => c.Name == "newuser" && c.Email == "new@test.com")), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ApplyAccountImageAsync(It.Is<ApplicationUser>(u => u.PlayerId == 5)), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Admin created new user: newuser with role User");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldEncodeTheUsernameAndTheEmail_InTheNotice()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("<b>bob</b>", "old@test.com", "Bob");
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(["User"]);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        await _authService.UpdateProfileAsync(userId, new UpdateProfileRequest("Bob", "a&b@test.com", null, "current-password"));

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "current-password"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("old@test.com", EmailChangedSubject, EmailChangedBody("&lt;b&gt;bob&lt;/b&gt;", "was changed to a&amp;b@test.com")), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldSendNoNotice_WhenThereWasNoPreviousEmail()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", null, "Name");
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(["User"]);

        var result = await _authService.UpdateProfileAsync(userId, new UpdateProfileRequest("Name", "new@test.com", null, "current-password"));

        result.Email.Should().Be("new@test.com");
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "current-password"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(null, user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldNotJudgeTheLink_WhenThePlayerStaysTheSame()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com", "Name");
        user.LinkPlayer(3);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(["User"]);

        var result = await _authService.UpdateProfileAsync(userId, new UpdateProfileRequest("New Name", null, 3));

        result.PlayerId.Should().Be(3);
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.RelinkAsync(3, user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "User user-id-123 updated their profile");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLinkablePlayersAsync_ShouldOnlyOfferPlayersWithoutAnEmail_WhenTheCallerIsUnknown()
    {
        _context.Players.Add(new Player("WithEmail", email: "a@test.com") { Id = 1 });
        _context.Players.Add(new Player("NoEmail") { Id = 2 });
        await _context.SaveChangesAsync();

        var result = await _authService.GetLinkablePlayersAsync("unknown-user");

        result.Should().Equal(new PlayerLinkDto(2, "NoEmail"));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldSendAgain_OnlyAfterTheFiveMinuteCooldown()
    {
        var user = new ApplicationUser("user", "u@test.com");
        _userManagerMock.Setup(x => x.FindByNameAsync("user")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        _publicUrlBuilderMock.Setup(x => x.BuildResetUrlAsync(user.Id, "reset-token")).ReturnsAsync("http://x/reset");

        await _authService.ForgotPasswordAsync("user");
        _cacheClock.UtcNow += TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(1);
        await _authService.ForgotPasswordAsync("user");
        _backgroundEmailSenderMock.Verify(x => x.Queue("u@test.com", ResetSubject, ResetBody("http://x/reset")), Times.Once);

        _cacheClock.UtcNow += TimeSpan.FromSeconds(2);
        await _authService.ForgotPasswordAsync("user");

        _userManagerMock.Verify(x => x.FindByNameAsync("user"), Times.Exactly(3));
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Exactly(3));
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(3));
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Exactly(2));
        _publicUrlBuilderMock.Verify(x => x.BuildResetUrlAsync(user.Id, "reset-token"), Times.Exactly(2));
        _backgroundEmailSenderMock.Verify(x => x.Queue("u@test.com", ResetSubject, ResetBody("http://x/reset")), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, $"Queued password reset email for user {user.Id}", Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, $"Forgot-password requested again for user {user.Id} within the cooldown; no email sent");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldKeepTheCooldownPerAccount()
    {
        var first = new ApplicationUser("first", "first@test.com");
        var second = new ApplicationUser("second", "second@test.com");
        _userManagerMock.Setup(x => x.FindByNameAsync("first")).ReturnsAsync(first);
        _userManagerMock.Setup(x => x.FindByNameAsync("second")).ReturnsAsync(second);
        _userManagerMock.Setup(x => x.HasPasswordAsync(first)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.HasPasswordAsync(second)).ReturnsAsync(true);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(first)).ReturnsAsync("first-token");
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(second)).ReturnsAsync("second-token");
        _publicUrlBuilderMock.Setup(x => x.BuildResetUrlAsync(first.Id, "first-token")).ReturnsAsync("http://x/first");
        _publicUrlBuilderMock.Setup(x => x.BuildResetUrlAsync(second.Id, "second-token")).ReturnsAsync("http://x/second");

        await _authService.ForgotPasswordAsync("first");
        await _authService.ForgotPasswordAsync("second");

        _userManagerMock.Verify(x => x.FindByNameAsync("first"), Times.Once);
        _userManagerMock.Verify(x => x.FindByNameAsync("second"), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(first), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(second), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(2));
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(first), Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(second), Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildResetUrlAsync(first.Id, "first-token"), Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildResetUrlAsync(second.Id, "second-token"), Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("first@test.com", ResetSubject, ResetBody("http://x/first")), Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("second@test.com", ResetSubject, ResetBody("http://x/second")), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"Queued password reset email for user {first.Id}");
        _loggerMock.VerifyLog(LogLevel.Information, $"Queued password reset email for user {second.Id}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldLogTheFailure_WhenTheUrlBuilderFailsForAnotherReason()
    {
        var user = new ApplicationUser("user", "u@test.com");
        var failure = new DomainException("something else");
        _userManagerMock.Setup(x => x.FindByNameAsync("user")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        _publicUrlBuilderMock.Setup(x => x.BuildResetUrlAsync(user.Id, "reset-token")).ThrowsAsync(failure);

        var act = () => _authService.ForgotPasswordAsync("user");

        await act.Should().NotThrowAsync();
        _userManagerMock.Verify(x => x.FindByNameAsync("user"), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildResetUrlAsync(user.Id, "reset-token"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "Failed to queue password reset email", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldGenerateADifferentPassword_EveryTime()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com");
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "reset-token", It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        var first = await _authService.ResetPasswordAsync(userId);
        var second = await _authService.ResetPasswordAsync(userId);

        first.TempPassword.Should().NotBe(second.TempPassword);
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Exactly(2));
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Exactly(2));
        _userManagerMock.Verify(x => x.ResetPasswordAsync(user, "reset-token", first.TempPassword), Times.Once);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(user, "reset-token", second.TempPassword), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync(userId, "Password reset by admin"), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Admin reset password for user testuser", Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact(Skip = "BUG: UpdateProfileAsync ignores a failed UpdateAsync, sends the email-change notice and reports success")]
    public async Task UpdateProfileAsync_ShouldReportTheFailure_AndSendNoNotice_WhenTheUserCannotBeSaved()
    {
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "old@test.com", "Name");
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "failed" }));

        var act = () => _authService.UpdateProfileAsync(userId, new UpdateProfileRequest("Name", "new@test.com", null, "current-password"));

        await act.Should().ThrowAsync<ValidationException>().WithMessage("failed");
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.CheckPasswordAsync(user, "current-password"), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        VerifyNoOtherCalls();
    }
}
