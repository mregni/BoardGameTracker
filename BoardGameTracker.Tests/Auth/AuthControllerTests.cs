using System.Collections.Generic;
using System.Security.Claims;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IOidcService> _oidcServiceMock;
    private readonly Mock<IProfileImageTicketService> _imageTicketsMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<IUserProfileImageService> _profileImagesMock = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _oidcServiceMock = new Mock<IOidcService>();
        _imageTicketsMock = new Mock<IProfileImageTicketService>();
        _imageTicketsMock.Setup(x => x.Issue(It.IsAny<string>())).Returns("image-ticket");
        _imageTicketsMock.Setup(x => x.Lifetime).Returns(TimeSpan.FromDays(7));
        _loggerMock = new Mock<ILogger<AuthController>>();
        var sessionCookies = new AuthSessionCookies(_imageTicketsMock.Object, Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 }));
        _controller = new AuthController(_authServiceMock.Object, _oidcServiceMock.Object, sessionCookies, _profileImagesMock.Object, _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _authServiceMock.VerifyNoOtherCalls();
        _oidcServiceMock.VerifyNoOtherCalls();
        _profileImagesMock.VerifyNoOtherCalls();
    }

    private string SetCookie(string name) =>
        _controller.HttpContext.Response.Headers.SetCookie.Single(c => c!.StartsWith(name + "=", StringComparison.Ordinal))!;

    private void VerifyImageCookie(bool issued)
    {
        var cookie = SetCookie(ProfileImageCookie.Name);
        cookie.Should().Contain(ProfileImageCookie.Name + "=" + (issued ? "image-ticket" : ""));
        cookie.Should().Contain("path=/images/profile");
        cookie.Should().Contain("httponly");
        if (!issued)
        {
            cookie.Should().Contain("expires=Thu, 01 Jan 1970");
        }
    }

    private void VerifyRefreshCookie(string? value, bool secure = false)
    {
        var cookie = SetCookie(AuthSessionCookies.RefreshCookieName);
        cookie.Should().StartWith($"{AuthSessionCookies.RefreshCookieName}={value};");
        cookie.Should().Contain("path=/api/auth;");
        cookie.Should().Contain("samesite=lax");
        cookie.Should().Contain("httponly");
        if (secure)
        {
            cookie.Should().Contain("secure");
        }
        else
        {
            cookie.Should().NotContain("secure");
        }

        if (value == null)
        {
            cookie.Should().Contain("expires=Thu, 01 Jan 1970");
        }
        else
        {
            cookie.Should().Contain("max-age=604800");
        }
    }

    private static DefaultHttpContext ContextWithRefreshCookie(string? refreshToken)
    {
        var context = new DefaultHttpContext();
        if (refreshToken != null)
        {
            context.Request.Headers.Cookie = $"{AuthSessionCookies.RefreshCookieName}={refreshToken}";
        }

        return context;
    }

    private static LoginResponse Session(string accessToken, string refreshToken) => new(
        accessToken, refreshToken, DateTime.UtcNow.AddHours(1),
        new UserInfo("user-id", "admin", "Admin", new List<string> { "Admin" }));

    #region Login

    [Fact]
    public async Task Login_ShouldReturnTheAccessToken_AndPutTheRefreshTokenInAnHttpOnlyCookie()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new LoginRequest("admin", "admin");
        var expectedResponse = Session("jwt-token", "refresh-token");

        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<string>())).ReturnsAsync(expectedResponse);

        var result = await _controller.Login(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expectedResponse);
        VerifyRefreshCookie("refresh-token");
        VerifyImageCookie(issued: true);

        _authServiceMock.Verify(x => x.LoginAsync(request, It.IsAny<string>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_ShouldMarkTheRefreshCookieSecure_WhenTheRequestIsHttps()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        _controller.ControllerContext = new ControllerContext { HttpContext = context };
        var request = new LoginRequest("admin", "admin");
        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<string>())).ReturnsAsync(Session("jwt-token", "refresh-token"));

        await _controller.Login(request);

        VerifyRefreshCookie("refresh-token", secure: true);
        _authServiceMock.Verify(x => x.LoginAsync(request, It.IsAny<string>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_ShouldPropagateException_WhenCredentialsAreInvalid()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new LoginRequest("unknown", "password");
        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException(Constants.Errors.InvalidCredentials));

        var act = () => _controller.Login(request);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(Constants.Errors.InvalidCredentials);
        _controller.HttpContext.Response.Headers.SetCookie.Should().BeEmpty();

        _authServiceMock.Verify(x => x.LoginAsync(request, It.IsAny<string>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Refresh

    [Fact]
    public async Task Refresh_ShouldUseTheCookie_AndRotateIt()
    {
        var expectedResponse = Session("new-jwt", "new-refresh");
        _controller.ControllerContext = new ControllerContext { HttpContext = ContextWithRefreshCookie("valid-token") };
        _authServiceMock.Setup(x => x.RefreshAsync("valid-token")).ReturnsAsync(expectedResponse);

        var result = await _controller.Refresh();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expectedResponse);
        VerifyRefreshCookie("new-refresh");
        VerifyImageCookie(issued: true);

        _authServiceMock.Verify(x => x.RefreshAsync("valid-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Refresh_ShouldThrow_AndClearTheCookie_WhenThereIsNoRefreshCookie(string? cookie)
    {
        var context = ContextWithRefreshCookie(cookie);
        var response = new StartableResponseFeature();
        context.Features.Set<IHttpResponseFeature>(response);
        _controller.ControllerContext = new ControllerContext { HttpContext = context };

        var act = () => _controller.Refresh();

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(Constants.Errors.InvalidRefreshToken);
        await response.StartAsync();
        SetCookie(AuthSessionCookies.RefreshCookieName).Should().StartWith($"{AuthSessionCookies.RefreshCookieName}=;").And.Contain("expires=Thu, 01 Jan 1970");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(Constants.Errors.InvalidRefreshToken)]
    [InlineData(Constants.Errors.AccountDisabled)]
    public async Task Refresh_ShouldPropagateException_AndClearBothCookies_WhenTheRefreshFails(string error)
    {
        var context = ContextWithRefreshCookie("stale-token");
        var response = new StartableResponseFeature();
        context.Features.Set<IHttpResponseFeature>(response);
        _controller.ControllerContext = new ControllerContext { HttpContext = context };
        _authServiceMock.Setup(x => x.RefreshAsync("stale-token")).ThrowsAsync(new AuthenticationFailedException(error));

        var act = () => _controller.Refresh();

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(error);
        context.Response.Headers.SetCookie.Should().BeEmpty();
        context.Response.Headers.Clear();
        await response.StartAsync();
        SetCookie(AuthSessionCookies.RefreshCookieName).Should().StartWith($"{AuthSessionCookies.RefreshCookieName}=;").And.Contain("path=/api/auth;");
        SetCookie(ProfileImageCookie.Name).Should().Contain("expires=Thu, 01 Jan 1970");

        _authServiceMock.Verify(x => x.RefreshAsync("stale-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Refresh_ShouldKeepTheCookie_WhenAnotherRequestRotatedTheSameTokenFirst()
    {
        var context = ContextWithRefreshCookie("raced-token");
        var response = new StartableResponseFeature();
        context.Features.Set<IHttpResponseFeature>(response);
        _controller.ControllerContext = new ControllerContext { HttpContext = context };
        _authServiceMock.Setup(x => x.RefreshAsync("raced-token")).ThrowsAsync(new AuthenticationFailedException(Constants.Errors.RefreshTokenSuperseded));

        var act = () => _controller.Refresh();

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(Constants.Errors.RefreshTokenSuperseded);
        await response.StartAsync();
        context.Response.Headers.SetCookie.Should().BeEmpty("the winning request just set the cookie this browser must keep");

        _authServiceMock.Verify(x => x.RefreshAsync("raced-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Refresh_ShouldNotClearTheCookie_WhenTheRefreshFailsForAnotherReason()
    {
        var context = ContextWithRefreshCookie("valid-token");
        var response = new StartableResponseFeature();
        context.Features.Set<IHttpResponseFeature>(response);
        _controller.ControllerContext = new ControllerContext { HttpContext = context };
        _authServiceMock.Setup(x => x.RefreshAsync("valid-token")).ThrowsAsync(new InvalidOperationException("database down"));

        var act = () => _controller.Refresh();

        await act.Should().ThrowAsync<InvalidOperationException>();
        await response.StartAsync();
        context.Response.Headers.SetCookie.Should().BeEmpty();

        _authServiceMock.Verify(x => x.RefreshAsync("valid-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    private sealed class StartableResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

        public override void OnStarting(Func<object, Task> callback, object state) => _onStarting.Add((callback, state));

        public async Task StartAsync()
        {
            foreach (var (callback, state) in _onStarting)
            {
                await callback(state);
            }
        }
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_ShouldRevokeTheCookieToken_AndClearBothCookies()
    {
        SetupAuthenticatedUser("user-id");
        _controller.HttpContext.Request.Headers.Cookie = $"{AuthSessionCookies.RefreshCookieName}=refresh-token";
        _authServiceMock.Setup(x => x.LogoutAsync("user-id", "refresh-token")).Returns(Task.CompletedTask);

        var result = await _controller.Logout();

        result.Should().BeOfType<NoContentResult>();
        VerifyRefreshCookie(null);
        VerifyImageCookie(issued: false);

        _authServiceMock.Verify(x => x.LogoutAsync("user-id", "refresh-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Logout_ShouldStillRevokeTheCookieToken_WhenTheAccessTokenIsGone()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = ContextWithRefreshCookie("refresh-token") };
        _authServiceMock.Setup(x => x.LogoutAsync(null, "refresh-token")).Returns(Task.CompletedTask);

        var result = await _controller.Logout();

        result.Should().BeOfType<NoContentResult>();
        VerifyRefreshCookie(null);

        _authServiceMock.Verify(x => x.LogoutAsync(null, "refresh-token"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Logout_ShouldClearTheCookies_WhenThereIsNoRefreshCookie()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = ContextWithRefreshCookie(null) };
        _authServiceMock.Setup(x => x.LogoutAsync(null, null)).Returns(Task.CompletedTask);

        var result = await _controller.Logout();

        result.Should().BeOfType<NoContentResult>();
        VerifyRefreshCookie(null);
        VerifyImageCookie(issued: false);

        _authServiceMock.Verify(x => x.LogoutAsync(null, null), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Register

    [Fact]
    public async Task Register_ShouldReturnCreated_WhenRequestIsValid()
    {
        SetupAuthenticatedUser("admin-id");
        var request = new RegisterRequest("newuser", "new@test.com", "password", null);
        var expectedDto = new UserDto("user-id", "newuser", "new@test.com", "New User",
            new List<string> { "User" }, DateTime.UtcNow, null, null);

        _authServiceMock.Setup(x => x.RegisterAsync(request)).ReturnsAsync(expectedDto);

        var result = await _controller.Register(request);

        var okResult = result.Should().BeOfType<CreatedResult>().Subject;
        okResult.Value.Should().BeSameAs(expectedDto);

        _authServiceMock.Verify(x => x.RegisterAsync(request), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetProfile

    [Fact]
    public async Task GetProfile_ShouldReturnOk_WhenAuthenticated()
    {
        SetupAuthenticatedUser("user-id");
        var expectedProfile = new ProfileResponse(
            "user-id", "testuser", "test@test.com", "Test",
            new List<string> { "User" }, DateTime.UtcNow, null, null);

        _authServiceMock.Setup(x => x.GetProfileAsync("user-id")).ReturnsAsync(expectedProfile);

        var result = await _controller.GetProfile();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expectedProfile);

        _authServiceMock.Verify(x => x.GetProfileAsync("user-id"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ProfileImage

    [Fact]
    public async Task UploadProfileImage_ShouldStoreTheFileForTheCurrentUser()
    {
        SetupAuthenticatedUser("user-id");
        var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>().Object;
        var expectedProfile = new ProfileResponse(
            "user-id", "testuser", null, null, ["User"], DateTime.UtcNow, null, null, "/images/profile/a.webp");
        _profileImagesMock.Setup(x => x.UploadAsync("user-id", file)).ReturnsAsync(expectedProfile);

        var result = await _controller.UploadProfileImage(new BoardGameTracker.Common.DTOs.Commands.UploadProfileImageCommand { File = file });

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(expectedProfile);
        _profileImagesMock.Verify(x => x.UploadAsync("user-id", file), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveProfileImage_ShouldClearTheImageOfTheCurrentUser()
    {
        SetupAuthenticatedUser("user-id");
        var expectedProfile = new ProfileResponse(
            "user-id", "testuser", null, null, ["User"], DateTime.UtcNow, null, null);
        _profileImagesMock.Setup(x => x.RemoveAsync("user-id")).ReturnsAsync(expectedProfile);

        var result = await _controller.RemoveProfileImage();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(expectedProfile);
        _profileImagesMock.Verify(x => x.RemoveAsync("user-id"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateProfile

    [Fact]
    public async Task UpdateProfile_ShouldReturnOk_WhenAuthenticated()
    {
        SetupAuthenticatedUser("user-id");
        var request = new UpdateProfileRequest("Updated Name", "new@test.com", null);
        var expectedProfile = new ProfileResponse(
            "user-id", "testuser", "new@test.com", "Updated Name",
            new List<string> { "User" }, DateTime.UtcNow, null, null);

        _authServiceMock.Setup(x => x.UpdateProfileAsync("user-id", request)).ReturnsAsync(expectedProfile);

        var result = await _controller.UpdateProfile(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expectedProfile);

        _authServiceMock.Verify(x => x.UpdateProfileAsync("user-id", request), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ChangePassword

    [Fact]
    public async Task ChangePassword_ShouldReturnNoContent_WhenPasswordChanged()
    {
        // Arrange
        SetupAuthenticatedUser("user-id");
        var request = new ChangePasswordRequest("old-password", "new-password");

        _authServiceMock.Setup(x => x.ChangePasswordAsync("user-id", request))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _authServiceMock.Verify(x => x.ChangePasswordAsync("user-id", request), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ResetPassword

    [Fact]
    public async Task ResetPassword_ShouldReturnOk_WithTempPassword()
    {
        SetupAuthenticatedUser("admin-id");
        var expectedResponse = new ResetPasswordResponse("temp-password-123");
        _authServiceMock.Setup(x => x.ResetPasswordAsync("target-user-id"))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.ResetPassword("target-user-id");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expectedResponse);

        _authServiceMock.Verify(x => x.ResetPasswordAsync("target-user-id"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetStatus

    [Fact]
    public void GetStatus_ShouldReturnAuthStatusResponse()
    {
        var expectedStatus = new AuthStatusResponse(AuthEnabled: true);
        _authServiceMock.Setup(x => x.GetStatus()).Returns(expectedStatus);

        var result = _controller.GetStatus();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expectedStatus);

        _authServiceMock.Verify(x => x.GetStatus(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetLinkablePlayers

    [Fact]
    public async Task GetLinkablePlayers_ShouldReturnOk_WhenAuthenticated()
    {
        SetupAuthenticatedUser("user-id");
        var expectedPlayers = new List<PlayerLinkDto> { new(1, "Alice") };
        _authServiceMock.Setup(x => x.GetLinkablePlayersAsync("user-id")).ReturnsAsync(expectedPlayers);

        var result = await _controller.GetLinkablePlayers();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expectedPlayers);

        _authServiceMock.Verify(x => x.GetLinkablePlayersAsync("user-id"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ForgotPassword

    [Fact]
    public async Task ForgotPassword_ShouldReturnNoContent()
    {
        var request = new ForgotPasswordRequest("someuser");
        _authServiceMock.Setup(x => x.ForgotPasswordAsync("someuser")).Returns(Task.CompletedTask);

        var result = await _controller.ForgotPassword(request);

        result.Should().BeOfType<NoContentResult>();

        _authServiceMock.Verify(x => x.ForgotPasswordAsync("someuser"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ResetPasswordWithToken

    [Fact]
    public async Task ResetPasswordWithToken_ShouldReturnNoContent_WhenTokenIsValid()
    {
        var request = new ResetPasswordConfirmRequest("user-id", "token", "new-password");
        _authServiceMock.Setup(x => x.ResetPasswordWithTokenAsync(request)).Returns(Task.CompletedTask);

        var result = await _controller.ResetPasswordWithToken(request);

        result.Should().BeOfType<NoContentResult>();

        _authServiceMock.Verify(x => x.ResetPasswordWithTokenAsync(request), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ExternalLogins

    [Fact]
    public async Task GetExternalLogins_ShouldReturnOk_WhenAuthenticated()
    {
        SetupAuthenticatedUser("user-id");
        var expectedLogins = new List<ExternalLoginDto>
        {
            new(1, "google", "key-1", "Google", DateTime.UtcNow, null)
        };
        _oidcServiceMock.Setup(x => x.GetExternalLoginsAsync("user-id")).ReturnsAsync(expectedLogins);

        var result = await _controller.GetExternalLogins();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expectedLogins);

        _oidcServiceMock.Verify(x => x.GetExternalLoginsAsync("user-id"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnlinkExternalLogin_ShouldReturnNoContent_WhenAuthenticated()
    {
        SetupAuthenticatedUser("user-id");
        _oidcServiceMock.Setup(x => x.UnlinkExternalLoginAsync("user-id", 4)).Returns(Task.CompletedTask);

        var result = await _controller.UnlinkExternalLogin(4);

        result.Should().BeOfType<NoContentResult>();

        _oidcServiceMock.Verify(x => x.UnlinkExternalLoginAsync("user-id", 4), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Unauthenticated

    [Fact]
    public async Task GetProfile_ShouldThrowUnauthorizedAccessException_WhenUserIdClaimIsMissing()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var act = () => _controller.GetProfile();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();

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
