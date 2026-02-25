using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IOidcService> _oidcServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _oidcServiceMock = new Mock<IOidcService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authServiceMock.Object, _oidcServiceMock.Object, _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _authServiceMock.VerifyNoOtherCalls();
        _oidcServiceMock.VerifyNoOtherCalls();
    }

    #region Login

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequest("admin", "admin");
        var expectedResponse = new LoginResponse(
            "jwt-token", "refresh-token", DateTime.UtcNow.AddHours(1),
            new UserInfo("user-id", "admin", "Admin", new List<string> { "Admin" }));

        _authServiceMock.Setup(x => x.LoginAsync(request)).ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.AccessToken.Should().Be("jwt-token");
        response.User.Username.Should().Be("admin");
        response.User.Roles.Should().Contain("Admin");

        _authServiceMock.Verify(x => x.LoginAsync(request), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_ShouldPropagateException_WhenCredentialsAreInvalid()
    {
        // Arrange
        var request = new LoginRequest("unknown", "password");
        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid username or password"));

        // Act
        var act = () => _controller.Login(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid username or password");

        _authServiceMock.Verify(x => x.LoginAsync(request), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Refresh

    [Fact]
    public async Task Refresh_ShouldReturnOk_WhenTokenIsValid()
    {
        // Arrange
        var request = new RefreshTokenRequest("valid-token");
        var expectedResponse = new LoginResponse(
            "new-jwt", "new-refresh", DateTime.UtcNow.AddHours(1),
            new UserInfo("user-id", "admin", "Admin", new List<string> { "Admin" }));

        _authServiceMock.Setup(x => x.RefreshAsync("valid-token")).ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Refresh(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.AccessToken.Should().Be("new-jwt");

        _authServiceMock.Verify(x => x.RefreshAsync("valid-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Refresh_ShouldPropagateException_WhenTokenIsInvalid()
    {
        // Arrange
        var request = new RefreshTokenRequest("invalid-token");
        _authServiceMock.Setup(x => x.RefreshAsync("invalid-token"))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid or expired refresh token"));

        // Act
        var act = () => _controller.Refresh(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        _authServiceMock.Verify(x => x.RefreshAsync("invalid-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_ShouldReturnOk_WhenAuthenticated()
    {
        // Arrange
        var request = new LogoutRequest("refresh-token");
        SetupAuthenticatedUser("user-id");

        _authServiceMock.Setup(x => x.LogoutAsync("user-id", "refresh-token"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout(request);

        // Assert
        result.Should().BeOfType<OkResult>();

        _authServiceMock.Verify(x => x.LogoutAsync("user-id", "refresh-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Register

    [Fact]
    public async Task Register_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        SetupAuthenticatedUser("admin-id");
        var request = new RegisterRequest("newuser", "new@test.com", "password", "New User");
        var expectedDto = new UserDto("user-id", "newuser", "new@test.com", "New User",
            new List<string> { "User" }, DateTime.UtcNow, null, null);

        _authServiceMock.Setup(x => x.RegisterAsync(request)).ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserDto>().Subject;
        response.Username.Should().Be("newuser");

        _authServiceMock.Verify(x => x.RegisterAsync(request), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetProfile

    [Fact]
    public async Task GetProfile_ShouldReturnOk_WhenAuthenticated()
    {
        // Arrange
        SetupAuthenticatedUser("user-id");
        var expectedProfile = new ProfileResponse(
            "user-id", "testuser", "test@test.com", "Test",
            new List<string> { "User" }, DateTime.UtcNow, null, null);

        _authServiceMock.Setup(x => x.GetProfileAsync("user-id")).ReturnsAsync(expectedProfile);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ProfileResponse>().Subject;
        response.Username.Should().Be("testuser");

        _authServiceMock.Verify(x => x.GetProfileAsync("user-id"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateProfile

    [Fact]
    public async Task UpdateProfile_ShouldReturnOk_WhenAuthenticated()
    {
        // Arrange
        SetupAuthenticatedUser("user-id");
        var request = new UpdateProfileRequest("Updated Name", "new@test.com");
        var expectedProfile = new ProfileResponse(
            "user-id", "testuser", "new@test.com", "Updated Name",
            new List<string> { "User" }, DateTime.UtcNow, null, null);

        _authServiceMock.Setup(x => x.UpdateProfileAsync("user-id", request)).ReturnsAsync(expectedProfile);

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ProfileResponse>().Subject;
        response.DisplayName.Should().Be("Updated Name");

        _authServiceMock.Verify(x => x.UpdateProfileAsync("user-id", request), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ChangePassword

    [Fact]
    public async Task ChangePassword_ShouldReturnOk_WhenPasswordChanged()
    {
        // Arrange
        SetupAuthenticatedUser("user-id");
        var request = new ChangePasswordRequest("old-password", "new-password");

        _authServiceMock.Setup(x => x.ChangePasswordAsync("user-id", request))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        result.Should().BeOfType<OkResult>();

        _authServiceMock.Verify(x => x.ChangePasswordAsync("user-id", request), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ResetPassword

    [Fact]
    public async Task ResetPassword_ShouldReturnOk_WithTempPassword()
    {
        // Arrange
        SetupAuthenticatedUser("admin-id");
        var expectedResponse = new ResetPasswordResponse("temp-password-123");
        _authServiceMock.Setup(x => x.ResetPasswordAsync("target-user-id"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ResetPassword("target-user-id");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ResetPasswordResponse>().Subject;
        response.TempPassword.Should().Be("temp-password-123");

        _authServiceMock.Verify(x => x.ResetPasswordAsync("target-user-id"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetStatus

    [Fact]
    public void GetStatus_ShouldReturnAuthStatusResponse()
    {
        // Arrange
        var expectedStatus = new AuthStatusResponse(AuthEnabled: true, BypassEnabled: false);
        _authServiceMock.Setup(x => x.GetStatus()).Returns(expectedStatus);

        // Act
        var result = _controller.GetStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthStatusResponse>().Subject;
        response.AuthEnabled.Should().BeTrue();
        response.BypassEnabled.Should().BeFalse();

        _authServiceMock.Verify(x => x.GetStatus(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}
