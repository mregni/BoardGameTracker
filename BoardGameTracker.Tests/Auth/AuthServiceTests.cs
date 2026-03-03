using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
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
            _context,
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
    }

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_ShouldReturnLoginResponse_WhenCredentialsAreValid()
    {
        // Arrange
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
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, roles)).Returns(accessToken);
        _tokenServiceMock.Setup(x => x.GenerateRefreshTokenAsync(user.Id)).ReturnsAsync(refreshToken);
        _tokenServiceMock.Setup(x => x.GetAccessTokenExpiry()).Returns(expiry);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(refreshToken.Token);
        result.ExpiresAt.Should().Be(expiry);
        result.User.Username.Should().Be("testuser");
        result.User.Roles.Should().Contain("User");

        _userManagerMock.Verify(x => x.FindByNameAsync("testuser"), Times.Once);
        _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(user, "password123", false), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(user, roles), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateRefreshTokenAsync(user.Id), Times.Once);
        _tokenServiceMock.Verify(x => x.GetAccessTokenExpiry(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenUserNotFound()
    {
        // Arrange
        var request = new LoginRequest("unknownuser", "password123");

        _userManagerMock.Setup(x => x.FindByNameAsync("unknownuser"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.InvalidCredentials);

        _userManagerMock.Verify(x => x.FindByNameAsync("unknownuser"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenPasswordIsWrong()
    {
        // Arrange
        var request = new LoginRequest("testuser", "wrongpassword");
        var user = new ApplicationUser("testuser", "test@test.com");

        _userManagerMock.Setup(x => x.FindByNameAsync("testuser")).ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "wrongpassword", false))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.InvalidCredentials);

        _userManagerMock.Verify(x => x.FindByNameAsync("testuser"), Times.Once);
        _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(user, "wrongpassword", false), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region RefreshAsync

    [Fact]
    public async Task RefreshAsync_ShouldReturnNewLoginResponse_WhenTokenIsValid()
    {
        // Arrange
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var activeToken = CreateActiveRefreshTokenWithUser(user.Id, user);
        var newRefreshToken = RefreshToken.Create(user.Id, 7);
        var roles = new List<string> { "User" };
        var accessToken = "new-access-token";
        var expiry = DateTime.UtcNow.AddHours(1);

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("valid-refresh-token"))
            .ReturnsAsync(activeToken);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
        _tokenServiceMock.Setup(x => x.GenerateRefreshTokenAsync(user.Id)).ReturnsAsync(newRefreshToken);
        _tokenServiceMock.Setup(x => x.RevokeRefreshTokenAsync(activeToken, "Replaced by new token", newRefreshToken.Token))
            .Returns(Task.CompletedTask);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, roles)).Returns(accessToken);
        _tokenServiceMock.Setup(x => x.GetAccessTokenExpiry()).Returns(expiry);

        // Act
        var result = await _authService.RefreshAsync("valid-refresh-token");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(newRefreshToken.Token);
        result.ExpiresAt.Should().Be(expiry);

        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("valid-refresh-token"), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateRefreshTokenAsync(user.Id), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeRefreshTokenAsync(activeToken, "Replaced by new token", newRefreshToken.Token), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(user, roles), Times.Once);
        _tokenServiceMock.Verify(x => x.GetAccessTokenExpiry(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowUnauthorizedAccessException_WhenTokenNotFound()
    {
        // Arrange
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("nonexistent-token"))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var act = () => _authService.RefreshAsync("nonexistent-token");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.InvalidRefreshToken);

        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("nonexistent-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowUnauthorizedAccessException_WhenTokenIsInactive()
    {
        // Arrange
        var revokedToken = CreateRevokedRefreshToken("user-id-123");

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("revoked-token"))
            .ReturnsAsync(revokedToken);

        // Act
        var act = () => _authService.RefreshAsync("revoked-token");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.InvalidRefreshToken);

        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("revoked-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region LogoutAsync

    [Fact]
    public async Task LogoutAsync_ShouldRevokeSpecificToken_WhenRefreshTokenProvided()
    {
        // Arrange
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com");
        var token = CreateActiveRefreshTokenWithUser(userId, user);

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("my-refresh-token"))
            .ReturnsAsync(token);
        _tokenServiceMock.Setup(x => x.RevokeRefreshTokenAsync(token, "Logged out", null))
            .Returns(Task.CompletedTask);

        // Act
        await _authService.LogoutAsync(userId, "my-refresh-token");

        // Assert
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("my-refresh-token"), Times.Once);
        _tokenServiceMock.Verify(x => x.RevokeRefreshTokenAsync(token, "Logged out", null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeAllTokens_WhenRefreshTokenIsNull()
    {
        // Arrange
        var userId = "user-id-123";

        _tokenServiceMock.Setup(x => x.RevokeAllUserTokensAsync(userId, "Logged out"))
            .Returns(Task.CompletedTask);

        // Act
        await _authService.LogoutAsync(userId, null);

        // Assert
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync(userId, "Logged out"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeAllTokens_WhenRefreshTokenIsEmpty()
    {
        // Arrange
        var userId = "user-id-123";

        _tokenServiceMock.Setup(x => x.RevokeAllUserTokensAsync(userId, "Logged out"))
            .Returns(Task.CompletedTask);

        // Act
        await _authService.LogoutAsync(userId, "");

        // Assert
        _tokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync(userId, "Logged out"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotRevokeToken_WhenTokenBelongsToDifferentUser()
    {
        // Arrange
        var userId = "user-id-123";
        var differentUserId = "user-id-456";
        var user = new ApplicationUser("otheruser", "other@test.com");
        var token = CreateActiveRefreshTokenWithUser(differentUserId, user);

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("other-users-token"))
            .ReturnsAsync(token);

        // Act
        await _authService.LogoutAsync(userId, "other-users-token");

        // Assert
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("other-users-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotRevokeToken_WhenTokenNotFound()
    {
        // Arrange
        var userId = "user-id-123";

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("missing-token"))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        await _authService.LogoutAsync(userId, "missing-token");

        // Assert
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("missing-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region RegisterAsync

    [Fact]
    public async Task RegisterAsync_ShouldReturnUserDto_WhenRegistrationSucceeds()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@test.com", "password123", null);
        var roles = new List<string> { "User" };

        _userManagerMock.Setup(x => x.FindByNameAsync("newuser"))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "password123"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Constants.AuthRoles.User))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(roles);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("newuser");
        result.Email.Should().Be("new@test.com");
        result.DisplayName.Should().Be("newuser");
        result.Roles.Should().Contain("User");

        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "password123"), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Constants.AuthRoles.User), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnUserDto_WhenRegistrationSucceedsWithReaderRole()
    {
        // Arrange
        var request = new RegisterRequest("reader", "reader@test.com", "password123", Constants.AuthRoles.Reader);
        var roles = new List<string> { Constants.AuthRoles.Reader };

        _userManagerMock.Setup(x => x.FindByNameAsync("reader"))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "password123"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Constants.AuthRoles.Reader))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(roles);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("reader");
        result.Roles.Should().Contain(Constants.AuthRoles.Reader);

        _userManagerMock.Verify(x => x.FindByNameAsync("reader"), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "password123"), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Constants.AuthRoles.Reader), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowValidationException_WhenRoleIsInvalid()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@test.com", "password123", "InvalidRole");

        _userManagerMock.Setup(x => x.FindByNameAsync("newuser"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.InvalidRole);

        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowDomainException_WhenUsernameAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest("existinguser", "new@test.com", "password123", null);
        var existingUser = new ApplicationUser("existinguser", "existing@test.com");

        _userManagerMock.Setup(x => x.FindByNameAsync("existinguser"))
            .ReturnsAsync(existingUser);

        // Act
        var act = () => _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(Constants.Errors.UsernameAlreadyExists);

        _userManagerMock.Verify(x => x.FindByNameAsync("existinguser"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowValidationException_WhenCreationFails()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@test.com", "weak", null);
        var identityErrors = new[] { new IdentityError { Description = "Password too short" } };

        _userManagerMock.Setup(x => x.FindByNameAsync("newuser"))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var act = () => _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Password too short");

        _userManagerMock.Verify(x => x.FindByNameAsync("newuser"), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "weak"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetProfileAsync

    [Fact]
    public async Task GetProfileAsync_ShouldReturnProfileResponse_WhenUserExists()
    {
        // Arrange
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com", "Test User");
        var roles = new List<string> { "User" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);

        // Act
        var result = await _authService.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@test.com");
        result.DisplayName.Should().Be("Test User");
        result.Roles.Should().Contain("User");

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetProfileAsync_ShouldThrowEntityNotFoundException_WhenUserNotFound()
    {
        // Arrange
        var userId = "nonexistent-user-id";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _authService.GetProfileAsync(userId);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateProfileAsync

    [Fact]
    public async Task UpdateProfileAsync_ShouldReturnUpdatedProfile_WhenEmailIsProvided()
    {
        // Arrange
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "old@test.com", "Old Name");
        var request = new UpdateProfileRequest("New Name", "new@test.com");
        var roles = new List<string> { "User" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);

        // Act
        var result = await _authService.UpdateProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("New Name");
        result.Email.Should().Be("new@test.com");

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldReturnUpdatedProfile_WhenEmailIsNotProvided()
    {
        // Arrange
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "original@test.com", "Old Name");
        var request = new UpdateProfileRequest("New Name", null);
        var roles = new List<string> { "User" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);

        // Act
        var result = await _authService.UpdateProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("New Name");
        result.Email.Should().Be("original@test.com");

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldThrowEntityNotFoundException_WhenUserNotFound()
    {
        // Arrange
        var userId = "nonexistent-user-id";
        var request = new UpdateProfileRequest("New Name", null);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _authService.UpdateProfileAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ChangePasswordAsync

    [Fact]
    public async Task ChangePasswordAsync_ShouldComplete_WhenPasswordChangedSuccessfully()
    {
        // Arrange
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com");
        var request = new ChangePasswordRequest("oldpassword", "newpassword");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, "oldpassword", "newpassword"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _authService.ChangePasswordAsync(userId, request);

        // Assert
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ChangePasswordAsync(user, "oldpassword", "newpassword"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrowEntityNotFoundException_WhenUserNotFound()
    {
        // Arrange
        var userId = "nonexistent-user-id";
        var request = new ChangePasswordRequest("oldpassword", "newpassword");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _authService.ChangePasswordAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrowValidationException_WhenPasswordChangeFails()
    {
        // Arrange
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com");
        var request = new ChangePasswordRequest("wrongpassword", "newpassword");
        var identityErrors = new[] { new IdentityError { Description = "Incorrect password" } };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, "wrongpassword", "newpassword"))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var act = () => _authService.ChangePasswordAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Incorrect password");

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.HasPasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ChangePasswordAsync(user, "wrongpassword", "newpassword"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ResetPasswordAsync

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnResetPasswordResponse_WhenResetSucceeds()
    {
        // Arrange
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com");
        var resetToken = "generated-reset-token";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, resetToken, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.ResetPasswordAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.TempPassword.Should().NotBeNullOrEmpty();
        result.TempPassword.Should().HaveLength(16);

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(user, resetToken, It.IsAny<string>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrowEntityNotFoundException_WhenUserNotFound()
    {
        // Arrange
        var userId = "nonexistent-user-id";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _authService.ResetPasswordAsync(userId);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrowValidationException_WhenResetFails()
    {
        // Arrange
        var userId = "user-id-123";
        var user = new ApplicationUser("testuser", "test@test.com");
        var resetToken = "generated-reset-token";
        var identityErrors = new[] { new IdentityError { Description = "Password reset failed" } };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, resetToken, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var act = () => _authService.ResetPasswordAsync(userId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Password reset failed");

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(user, resetToken, It.IsAny<string>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetStatus

    [Fact]
    public void GetStatus_ShouldReturnAuthEnabledAndBypassDisabled_WhenNotInDevelopment()
    {
        // Arrange
        _environmentProviderMock.Setup(x => x.IsDevelopment).Returns(false);

        // Act
        var result = _authService.GetStatus();

        // Assert
        result.Should().NotBeNull();
        result.AuthEnabled.Should().BeTrue();
        result.BypassEnabled.Should().BeFalse();

        _environmentProviderMock.Verify(x => x.IsDevelopment, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void GetStatus_ShouldReturnBypassDisabled_WhenInDevelopmentButAuthBypassNotSet()
    {
        // Arrange
        _environmentProviderMock.Setup(x => x.IsDevelopment).Returns(true);
        Environment.SetEnvironmentVariable("AUTH_BYPASS", null);

        // Act
        var result = _authService.GetStatus();

        // Assert
        result.Should().NotBeNull();
        result.AuthEnabled.Should().BeTrue();
        result.BypassEnabled.Should().BeFalse();

        _environmentProviderMock.Verify(x => x.IsDevelopment, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void GetStatus_ShouldReturnBypassEnabled_WhenInDevelopmentAndAuthBypassIsTrue()
    {
        // Arrange
        _environmentProviderMock.Setup(x => x.IsDevelopment).Returns(true);
        Environment.SetEnvironmentVariable("AUTH_BYPASS", "true");

        try
        {
            // Act
            var result = _authService.GetStatus();

            // Assert
            result.Should().NotBeNull();
            result.AuthEnabled.Should().BeTrue();
            result.BypassEnabled.Should().BeTrue();

            _environmentProviderMock.Verify(x => x.IsDevelopment, Times.Once);
            VerifyNoOtherCalls();
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTH_BYPASS", null);
        }
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
}
