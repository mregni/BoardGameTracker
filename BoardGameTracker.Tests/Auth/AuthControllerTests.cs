using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class AuthControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _userManagerMock.VerifyNoOtherCalls();
        _signInManagerMock.VerifyNoOtherCalls();
        _tokenServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequest("admin", "admin");
        var user = new ApplicationUser("admin", "admin@test.com", "Admin");

        _userManagerMock.Setup(x => x.FindByNameAsync("admin")).ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "admin", false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>())).Returns("jwt-token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshTokenAsync(user.Id))
            .ReturnsAsync(RefreshToken.Create(user.Id, 7));
        _tokenServiceMock.Setup(x => x.GetAccessTokenExpiry()).Returns(DateTime.UtcNow.AddHours(1));

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.AccessToken.Should().Be("jwt-token");
        response.User.Username.Should().Be("admin");
        response.User.Roles.Should().Contain("Admin");

        _userManagerMock.Verify(x => x.FindByNameAsync("admin"), Times.Once);
        _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(user, "admin", false), Times.Once);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserNotFound()
    {
        // Arrange
        var request = new LoginRequest("unknown", "password");
        _userManagerMock.Setup(x => x.FindByNameAsync("unknown")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        _userManagerMock.Verify(x => x.FindByNameAsync("unknown"), Times.Once);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        // Arrange
        var request = new LoginRequest("admin", "wrong-password");
        var user = new ApplicationUser("admin", "admin@test.com");

        _userManagerMock.Setup(x => x.FindByNameAsync("admin")).ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "wrong-password", false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorized_WhenTokenIsExpiredOrRevoked()
    {
        // Arrange
        var request = new RefreshTokenRequest("expired-token");
        var expiredToken = RefreshToken.Create("user-id", 0); // Already expired (0 days)

        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("expired-token")).ReturnsAsync(expiredToken);

        // Act
        var result = await _controller.Refresh(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        _tokenServiceMock.Verify(x => x.GetRefreshTokenAsync("expired-token"), Times.Once);
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorized_WhenTokenIsInvalid()
    {
        // Arrange
        var request = new RefreshTokenRequest("invalid-token");
        _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("invalid-token")).ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _controller.Refresh(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnOk_WhenAuthenticated()
    {
        // Arrange
        var user = new ApplicationUser("testuser", "test@test.com", "Test");
        SetupAuthenticatedUser(user.Id);

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Reader" });

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserInfo>().Subject;
        response.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@test.com", "password", "New User");
        SetupAuthenticatedUser("admin-id");

        _userManagerMock.Setup(x => x.FindByNameAsync("newuser")).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "password"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Reader"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Reader" });

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserDto>().Subject;
        response.Username.Should().Be("newuser");
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenUsernameExists()
    {
        // Arrange
        var request = new RegisterRequest("existing", "new@test.com", "password", null);
        var existingUser = new ApplicationUser("existing", "existing@test.com");

        _userManagerMock.Setup(x => x.FindByNameAsync("existing")).ReturnsAsync(existingUser);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void GetStatus_ShouldReturnAuthStatusResponse()
    {
        // Act
        var result = _controller.GetStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthStatusResponse>().Subject;
        response.AuthEnabled.Should().BeTrue();
    }

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
