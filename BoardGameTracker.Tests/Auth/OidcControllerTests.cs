using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
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

    private OidcController CreateController(string? sessionCookie = null, string scheme = "https", string host = "games.example.com")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "test"));
        if (sessionCookie != null)
        {
            context.Request.Headers.Cookie = $"{OidcController.SessionCookieName}={sessionCookie}";
        }

        return new OidcController(_oidcServiceMock.Object, _publicUrlBuilderMock.Object, Mock.Of<IProfileImageTicketService>(),
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

    private static string[] SetCookies(OidcController controller) => controller.Response.Headers.SetCookie.ToArray()!;

    [Fact]
    public async Task Login_ShouldRedirectToTheProvider_AndSetASecureStateCookie_OverHttps()
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);
        _oidcServiceMock.Setup(x => x.StartLoginAsync("kc", "https://games.example.com", "/games"))
            .ReturnsAsync(new OidcAuthorizationRequest("https://sso.example.com/authorize?x=1", "state-1"));
        var controller = CreateController();

        var result = await controller.Login("kc", "/games");

        result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("https://sso.example.com/authorize?x=1");
        SetCookies(controller).Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=state-1", StringComparison.Ordinal))
            .Which.Should().Contain("secure").And.Contain("httponly");
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.StartLoginAsync("kc", "https://games.example.com", "/games"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("localhost:5444")]
    [InlineData("127.0.0.1:5444")]
    [InlineData("[::1]:5444")]
    public async Task Login_ShouldBeAllowedOverPlainHttp_OnALoopbackHost_WithASecureCookie(string host)
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);
        _oidcServiceMock.Setup(x => x.StartLoginAsync("kc", $"http://{host}", null))
            .ReturnsAsync(new OidcAuthorizationRequest("https://sso.example.com/authorize", "state-1"));
        var controller = CreateController(scheme: "http", host: host);

        var result = await controller.Login("kc", null);

        result.Should().BeOfType<RedirectResult>();
        SetCookies(controller).Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=", StringComparison.Ordinal))
            .Which.Should().Contain("secure");
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.StartLoginAsync("kc", $"http://{host}", null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("http", "192.168.1.20:5444", null)]
    [InlineData("http", "games.example.com", null)]
    [InlineData("https", "games.example.com", "http://games.example.com")]
    public async Task Login_ShouldSendTheBrowserBackToTheLoginPage_WhenTheConnectionIsNotHttps(string scheme, string host, string? publicUrl)
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync(publicUrl);
        var controller = CreateController(scheme: scheme, host: host);

        var result = await controller.Login("kc", "/games");

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be("/login?error=error.auth.oidc-https-required");
        SetCookies(controller).Should().BeEmpty();
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Callback_ShouldRefusePlainHttp_WithoutTouchingTheProvider()
    {
        var controller = CreateController(scheme: "http", host: "192.168.1.20:5444");

        var result = await controller.Callback("kc", "code", "state", null, null);

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be("/auth-callback?error=error.auth.oidc-https-required");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkCallback_ShouldRefusePlainHttp_WithoutTouchingTheProvider()
    {
        var controller = CreateController(scheme: "http", host: "games.example.com");

        var result = await controller.LinkCallback("kc", "code", "state", null, null);

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be("/auth-callback?error=error.auth.oidc-https-required");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkCallback_ShouldRedirectWithTheLinkedProvider_OverHttps()
    {
        _oidcServiceMock.Setup(x => x.CompleteLinkAsync("kc", "code", "state", null, null)).ReturnsAsync(new OidcLinkResult("user-1", "kc"));
        var controller = CreateController();

        var result = await controller.LinkCallback("kc", "code", "state", null, null);

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be("/auth-callback?linked=kc");
        SetCookies(controller).Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=;", StringComparison.Ordinal))
            .Which.Should().Contain("secure");
        _oidcServiceMock.Verify(x => x.CompleteLinkAsync("kc", "code", "state", null, null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkLogin_ShouldReturnTheAuthorizationUrl_AndSetASecureStateCookie_OverHttps()
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync("https://games.example.com");
        _oidcServiceMock.Setup(x => x.StartLinkAsync("user-1", "kc", "https://games.example.com"))
            .ReturnsAsync(new OidcAuthorizationRequest("https://sso.example.com/authorize", "state-2"));
        var controller = CreateController();

        var result = await controller.LinkLogin("kc");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(new AuthorizationUrlResponse("https://sso.example.com/authorize"));
        SetCookies(controller).Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=state-2", StringComparison.Ordinal))
            .Which.Should().Contain("secure");
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.StartLinkAsync("user-1", "kc", "https://games.example.com"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("http", "192.168.1.20:5444", null)]
    [InlineData("https", "games.example.com", "http://games.example.com")]
    public async Task LinkLogin_ShouldRefuse_WhenTheConnectionIsNotHttps(string scheme, string host, string? publicUrl)
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync(publicUrl);
        var controller = CreateController(scheme: scheme, host: host);

        var act = () => controller.LinkLogin("kc");

        (await act.Should().ThrowAsync<ValidationException>()).Which.Message.Should().Be(Constants.Errors.OidcHttpsRequired);
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("https", "games.example.com", null, false)]
    [InlineData("http", "localhost:5444", null, false)]
    [InlineData("http", "192.168.1.20:5444", null, true)]
    [InlineData("http", "192.168.1.20:5444", "https://games.example.com", true)]
    [InlineData("https", "games.example.com", "http://games.example.com", true)]
    public async Task GetProvider_ShouldReportWhetherHttpsIsRequired(string scheme, string host, string? publicUrl, bool expected)
    {
        _oidcServiceMock.Setup(x => x.GetEnabledProviderAsync()).ReturnsAsync(new OidcProviderInfo("kc", "Keycloak", null, null));
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync(publicUrl);
        var controller = CreateController(scheme: scheme, host: host);

        var result = await controller.GetProvider();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(new OidcProviderInfo("kc", "Keycloak", null, null, expected));
        _oidcServiceMock.Verify(x => x.GetEnabledProviderAsync(), Times.Once);
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.AtMostOnce);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetProvider_ShouldReturn404_WithoutAnEnabledProvider()
    {
        _oidcServiceMock.Setup(x => x.GetEnabledProviderAsync()).ReturnsAsync((OidcProviderInfo?)null);
        var controller = CreateController();

        var result = await controller.GetProvider();

        result.Should().BeOfType<NotFoundResult>();
        _oidcServiceMock.Verify(x => x.GetEnabledProviderAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Callback_ShouldMarkTheHandoffAndSessionCookiesSecure()
    {
        var login = new LoginResponse("a", "r", DateTime.UtcNow, new UserInfo("1", "jane", null, ["User"]));
        _oidcServiceMock.Setup(x => x.CompleteLoginAsync("kc", "code", "state", null, null))
            .ReturnsAsync(new OidcLoginResult(login, "/", "session-key"));
        _oidcServiceMock.Setup(x => x.CreateHandoff(login)).Returns("handoff");
        var controller = CreateController(scheme: "http", host: "localhost:5444");

        await controller.Callback("kc", "code", "state", null, null);

        var cookies = SetCookies(controller);
        cookies.Should().HaveCount(3).And.OnlyContain(c => c.Contains("secure") && c.Contains("httponly") && c.Contains("path=/api/auth/oidc"));
        cookies.Should().Contain(c => c.StartsWith($"{OidcController.HandoffCookieName}=handoff", StringComparison.Ordinal));
        cookies.Should().Contain(c => c.StartsWith($"{OidcController.SessionCookieName}=session-key", StringComparison.Ordinal));
        _oidcServiceMock.Verify(x => x.CompleteLoginAsync("kc", "code", "state", null, null), Times.Once);
        _oidcServiceMock.Verify(x => x.CreateHandoff(login), Times.Once);
        VerifyNoOtherCalls();
    }
}
