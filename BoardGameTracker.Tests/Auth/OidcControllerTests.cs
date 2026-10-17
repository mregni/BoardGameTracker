using System.Net.Http;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcControllerTests
{
    public static TheoryData<Exception, string> Errors => new()
    {
        { new DomainException(Constants.Errors.OidcAlreadyLinked), Constants.Errors.OidcAlreadyLinked },
        { new ValidationException(Constants.Errors.InvalidAuthSession), Constants.Errors.InvalidAuthSession },
        { new AuthenticationFailedException(Constants.Errors.AccountLockedOut), Constants.Errors.AccountLockedOut },
        { new EntityNotFoundException("ApplicationUser", "42"), Constants.Errors.InvalidAuthSession },
        { new ValidationException("Invalid BGG API key. Please check your API key in settings."), Constants.Errors.OidcFailed },
        { new DomainException("Provider returned an unexpected token response"), Constants.Errors.OidcFailed },
        { new InvalidOperationException("error.looks-like-a-key"), Constants.Errors.OidcFailed },
        { new HttpRequestException("Connection refused (idp.internal:443)"), Constants.Errors.OidcFailed },
    };

    [Theory]
    [MemberData(nameof(Errors))]
    public void ErrorKey_ShouldOnlyForwardTranslationKeysFromExpectedExceptions(Exception exception, string expected)
    {
        OidcController.ErrorKey(exception).Should().Be(expected);
    }

    private readonly Mock<IOidcService> _oidcServiceMock = new();
    private readonly Mock<IPublicUrlBuilder> _publicUrlBuilderMock = new();

    private OidcController CreateController(string? sessionCookie = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("games.example.com");
        if (sessionCookie != null)
        {
            context.Request.Headers.Cookie = $"{OidcController.SessionCookieName}={sessionCookie}";
        }

        return new OidcController(_oidcServiceMock.Object, _publicUrlBuilderMock.Object, new AuthSessionCookies(Mock.Of<IProfileImageTicketService>(), Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 })),
            Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 }), Mock.Of<ILogger<OidcController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = context },
        };
    }

    private void VerifyNoOtherCalls()
    {
        _oidcServiceMock.VerifyNoOtherCalls();
        _publicUrlBuilderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Logout_ShouldReturnTheSignOutUrl_AndClearTheSessionCookie()
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync("https://games.example.com");
        _oidcServiceMock.Setup(x => x.EndSessionAsync("session-key", "https://games.example.com")).ReturnsAsync("https://sso.example.com/logout?x=1");
        var controller = CreateController("session-key");

        var result = await controller.Logout();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(new OidcLogoutResponse("https://sso.example.com/logout?x=1"));
        controller.Response.Headers.SetCookie.ToString().Should().Contain($"{OidcController.SessionCookieName}=;").And.Contain("path=/api/auth/oidc");
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.EndSessionAsync("session-key", "https://games.example.com"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Logout_ShouldReturnNoUrl_WithoutAnOidcSession()
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);
        var controller = CreateController();

        var result = await controller.Logout();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(new OidcLogoutResponse(null));
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.EndSessionAsync(null, "https://games.example.com"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("session-key", true)]
    [InlineData(null, false)]
    public async Task Callback_ShouldPassTheIssParameter_AndSetTheSessionCookieOnlyForAnOidcSession(string? sessionKey, bool expectCookie)
    {
        var login = new LoginResponse("a", "r", DateTime.UtcNow, new UserInfo("1", "jane", null, ["User"]));
        _oidcServiceMock.Setup(x => x.CompleteLoginAsync("kc", "code", "state", null, "https://sso.example.com"))
            .ReturnsAsync(new OidcLoginResult(login, "/games", sessionKey));
        _oidcServiceMock.Setup(x => x.CreateHandoff(login)).Returns("handoff");
        var controller = CreateController();

        var result = await controller.Callback("kc", "code", "state", null, "https://sso.example.com");

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be("/auth-callback?redirect=%2Fgames");
        var cookies = controller.Response.Headers.SetCookie.ToString();
        if (expectCookie)
        {
            cookies.Should().Contain($"{OidcController.SessionCookieName}=session-key").And.Contain("httponly");
        }
        else
        {
            cookies.Should().NotContain($"{OidcController.SessionCookieName}=session-key");
        }

        _oidcServiceMock.Verify(x => x.CompleteLoginAsync("kc", "code", "state", null, "https://sso.example.com"), Times.Once);
        _oidcServiceMock.Verify(x => x.CreateHandoff(login), Times.Once);
        VerifyNoOtherCalls();
    }
}
