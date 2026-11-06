using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;
using BoardGameTracker.Tests.Support;
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
        VerifyNoOtherCalls();
    }

    private readonly Mock<IOidcService> _oidcServiceMock = new();
    private readonly Mock<IPublicUrlBuilder> _publicUrlBuilderMock = new();
    private readonly Mock<IProfileImageTicketService> _imageTicketsMock = new();
    private readonly Mock<ILogger<OidcController>> _loggerMock = new();

    private OidcController CreateController(string? sessionCookie = null, string scheme = "https", string host = "games.example.com", string? stateCookie = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "test"));
        if (sessionCookie != null)
        {
            context.Request.Headers.Cookie = $"{OidcController.SessionCookieName}={sessionCookie}";
        }

        if (stateCookie != null)
        {
            context.Request.Headers.Cookie = $"{OidcController.StateCookieName}={stateCookie}";
        }

        return new OidcController(_oidcServiceMock.Object, _publicUrlBuilderMock.Object, new AuthSessionCookies(_imageTicketsMock.Object, Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 })),
            Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 }), _loggerMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
        };
    }

    private void VerifyNoOtherCalls()
    {
        _oidcServiceMock.VerifyNoOtherCalls();
        _publicUrlBuilderMock.VerifyNoOtherCalls();
        _imageTicketsMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
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
        _oidcServiceMock.Setup(x => x.CompleteLoginAsync("code", "state", null, "https://sso.example.com"))
            .ReturnsAsync(new OidcLoginResult(login, "/games", sessionKey));
        _oidcServiceMock.Setup(x => x.CreateHandoff(login)).Returns("handoff");
        var controller = CreateController();

        var result = await controller.Callback("code", "state", null, "https://sso.example.com");

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be("/auth-callback?redirect=%2Fgames");
        var cookies = controller.Response.Headers.SetCookie.ToString();
        if (expectCookie)
        {
            cookies.Should().Contain($"{OidcController.SessionCookieName}=session-key").And.Contain("httponly");
        }
        else
        {
            cookies.Should().Contain($"{OidcController.SessionCookieName}=;");
        }

        _oidcServiceMock.Verify(x => x.CompleteLoginAsync("code", "state", null, "https://sso.example.com"), Times.Once);
        _oidcServiceMock.Verify(x => x.CreateHandoff(login), Times.Once);
        VerifyNoOtherCalls();
    }

    private static string[] SetCookies(OidcController controller) => controller.Response.Headers.SetCookie.ToArray()!;

    [Fact]
    public async Task Login_ShouldRedirectToTheProvider_AndSetASecureStateCookie_OverHttps()
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);
        _oidcServiceMock.Setup(x => x.StartLoginAsync("https://games.example.com", "/games"))
            .ReturnsAsync(new OidcAuthorizationRequest("https://sso.example.com/authorize?x=1", "state-1"));
        var controller = CreateController();

        var result = await controller.Login("/games");

        result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("https://sso.example.com/authorize?x=1");
        SetCookies(controller).Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=state-1", StringComparison.Ordinal))
            .Which.Should().Contain("secure").And.Contain("httponly").And.Contain("max-age=600").And.Contain("path=/api/auth/oidc").And.Contain("samesite=lax");
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.StartLoginAsync("https://games.example.com", "/games"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "OIDC login initiated");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("localhost:5444")]
    [InlineData("127.0.0.1:5444")]
    [InlineData("[::1]:5444")]
    public async Task Login_ShouldBeAllowedOverPlainHttp_OnALoopbackHost_WithASecureCookie(string host)
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);
        _oidcServiceMock.Setup(x => x.StartLoginAsync($"http://{host}", null))
            .ReturnsAsync(new OidcAuthorizationRequest("https://sso.example.com/authorize", "state-1"));
        var controller = CreateController(scheme: "http", host: host);

        var result = await controller.Login(null);

        result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("https://sso.example.com/authorize");
        SetCookies(controller).Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=", StringComparison.Ordinal))
            .Which.Should().Contain("secure");
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.StartLoginAsync($"http://{host}", null), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "OIDC login initiated");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_ShouldSendTheBrowserToTheErrorPage_WhenNoProviderIsEnabled()
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);
        var failure = new EntityNotFoundException("OidcProvider", "enabled");
        _oidcServiceMock.Setup(x => x.StartLoginAsync("https://games.example.com", null)).ThrowsAsync(failure);
        var controller = CreateController();

        var result = await controller.Login(null);

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcProviderUnavailable)}");
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.StartLoginAsync("https://games.example.com", null), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "OIDC login initiated");
        _loggerMock.VerifyLog(LogLevel.Warning, "OIDC login at the identity provider could not start", failure);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("access_denied", "code")]
    [InlineData(null, null)]
    public async Task Callbacks_ShouldReportARejection_WhenTheProviderReturnsAnErrorOrNoCode(string? error, string? code)
    {
        var controller = CreateController();
        var expected = $"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcProviderRejected)}";

        var login = await controller.Callback(code, "state", error, null);
        var link = await controller.LinkCallback(code, "state", error, null);

        login.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be(expected);
        link.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be(expected);
        _loggerMock.VerifyLog(LogLevel.Warning, $"The identity provider returned to the callback with error {error ?? "missing code"}");
        _loggerMock.VerifyLog(LogLevel.Warning, $"The identity provider returned to the link callback with error {error ?? "missing code"}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Callbacks_ShouldReportTheErrorKey_WhenCompletingTheFlowFails()
    {
        var loginFailure = new DomainException(Constants.Errors.AccountDisabled);
        var linkFailure = new DomainException(Constants.Errors.OidcAlreadyLinked);
        _oidcServiceMock.Setup(x => x.CompleteLoginAsync("code", "state", null, null)).ThrowsAsync(loginFailure);
        _oidcServiceMock.Setup(x => x.CompleteLinkAsync("code", "state", null, null)).ThrowsAsync(linkFailure);
        var controller = CreateController();

        var login = await controller.Callback("code", "state", null, null);
        var link = await controller.LinkCallback("code", "state", null, null);

        login.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.AccountDisabled)}");
        link.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcAlreadyLinked)}");
        _oidcServiceMock.Verify(x => x.CompleteLoginAsync("code", "state", null, null), Times.Once);
        _oidcServiceMock.Verify(x => x.CompleteLinkAsync("code", "state", null, null), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "Completing the OIDC login failed", loginFailure);
        _loggerMock.VerifyLog(LogLevel.Warning, "Completing the OIDC link failed", linkFailure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void LegacyLogin_ShouldRedirectToTheFixedLoginRoute()
    {
        var controller = CreateController();

        var result = controller.LegacyLogin();

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be("/api/auth/oidc/login");
        controller.Response.Headers.SetCookie.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(nameof(OidcController.Login), "login")]
    [InlineData(nameof(OidcController.Callback), "callback")]
    [InlineData(nameof(OidcController.LinkLogin), "link")]
    [InlineData(nameof(OidcController.LinkCallback), "link-callback")]
    [InlineData(nameof(OidcController.LegacyLogin), "{provider}/login")]
    public void OidcFlowActions_ShouldUseFixedRoutes(string action, string template)
    {
        typeof(OidcController).GetMethod(action)!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .Should().ContainSingle().Which.Should().BeOfType<HttpGetAttribute>().Which.Template.Should().Be(template);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(nameof(OidcController.Login), OidcHttpsRefusal.LoginPage)]
    [InlineData(nameof(OidcController.Callback), OidcHttpsRefusal.CallbackPage)]
    [InlineData(nameof(OidcController.LinkLogin), OidcHttpsRefusal.BadRequest)]
    [InlineData(nameof(OidcController.LinkCallback), OidcHttpsRefusal.CallbackPage)]
    public void OidcFlowActions_ShouldRequireHttps(string action, OidcHttpsRefusal refusal)
    {
        var attribute = typeof(OidcController).GetMethod(action)!.GetCustomAttributes(typeof(RequireOidcHttpsAttribute), false)
            .Should().ContainSingle().Which.Should().BeOfType<RequireOidcHttpsAttribute>().Subject;

        attribute.Arguments.Should().Equal(refusal);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkCallback_ShouldRedirectWithTheLinkedProvider_OverHttps()
    {
        _oidcServiceMock.Setup(x => x.CompleteLinkAsync("code", "state", null, null)).ReturnsAsync(new OidcLinkResult("user-1"));
        var controller = CreateController();

        var result = await controller.LinkCallback("code", "state", null, null);

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be("/auth-callback?linked=true");
        SetCookies(controller).Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=;", StringComparison.Ordinal))
            .Which.Should().Contain("secure");
        _oidcServiceMock.Verify(x => x.CompleteLinkAsync("code", "state", null, null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkLogin_ShouldReturnTheAuthorizationUrl_AndSetASecureStateCookie_OverHttps()
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync("https://games.example.com");
        _oidcServiceMock.Setup(x => x.StartLinkAsync("user-1", "https://games.example.com"))
            .ReturnsAsync(new OidcAuthorizationRequest("https://sso.example.com/authorize", "state-2"));
        var controller = CreateController();

        var result = await controller.LinkLogin();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(new AuthorizationUrlResponse("https://sso.example.com/authorize"));
        SetCookies(controller).Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=state-2", StringComparison.Ordinal))
            .Which.Should().Contain("secure");
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.StartLinkAsync("user-1", "https://games.example.com"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "OIDC link initiated by user user-1");
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
        _oidcServiceMock.Setup(x => x.GetEnabledProviderAsync()).ReturnsAsync(new OidcProviderInfo("Keycloak", null, null));
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync(publicUrl);
        var controller = CreateController(scheme: scheme, host: host);

        var result = await controller.GetProvider();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(new OidcProviderInfo("Keycloak", null, null, expected));
        _oidcServiceMock.Verify(x => x.GetEnabledProviderAsync(), Times.Once);
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Exactly(scheme == "https" || host.StartsWith("localhost", StringComparison.Ordinal) ? 1 : 0));
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
        _oidcServiceMock.Setup(x => x.CompleteLoginAsync("code", "state", null, null))
            .ReturnsAsync(new OidcLoginResult(login, "/", "session-key"));
        _oidcServiceMock.Setup(x => x.CreateHandoff(login)).Returns("handoff");
        var controller = CreateController(scheme: "http", host: "localhost:5444");

        await controller.Callback("code", "state", null, null);

        var cookies = SetCookies(controller);
        cookies.Should().HaveCount(3).And.OnlyContain(c => c.Contains("secure") && c.Contains("httponly") && c.Contains("path=/api/auth/oidc"));
        cookies.Should().ContainSingle(c => c.StartsWith($"{OidcController.HandoffCookieName}=handoff", StringComparison.Ordinal)).Which.Should().Contain("max-age=60;");
        cookies.Should().ContainSingle(c => c.StartsWith($"{OidcController.SessionCookieName}=session-key", StringComparison.Ordinal)).Which.Should().Contain("max-age=604800");
        _oidcServiceMock.Verify(x => x.CompleteLoginAsync("code", "state", null, null), Times.Once);
        _oidcServiceMock.Verify(x => x.CreateHandoff(login), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Callback_ShouldPassTheBrowserState_AndDeleteItsCookie()
    {
        var login = new LoginResponse("a", "r", DateTime.UtcNow, new UserInfo("1", "jane", null, ["User"]));
        _oidcServiceMock.Setup(x => x.CompleteLoginAsync("code", "state", "browser-state", null)).ReturnsAsync(new OidcLoginResult(login, "/", null));
        _oidcServiceMock.Setup(x => x.CreateHandoff(login)).Returns("handoff");
        var controller = CreateController(stateCookie: "browser-state");

        await controller.Callback("code", "state", null, null);

        SetCookies(controller).Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=;", StringComparison.Ordinal));
        _oidcServiceMock.Verify(x => x.CompleteLoginAsync("code", "state", "browser-state", null), Times.Once);
        _oidcServiceMock.Verify(x => x.CreateHandoff(login), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkCallback_ShouldPassTheBrowserStateAndIssuer_AndDeleteTheStateCookie()
    {
        _oidcServiceMock.Setup(x => x.CompleteLinkAsync("code", "state", "browser-state", "https://sso.example.com")).ReturnsAsync(new OidcLinkResult("user-1"));
        var controller = CreateController(stateCookie: "browser-state");

        await controller.LinkCallback("code", "state", null, "https://sso.example.com");

        SetCookies(controller).Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=;", StringComparison.Ordinal));
        _oidcServiceMock.Verify(x => x.CompleteLinkAsync("code", "state", "browser-state", "https://sso.example.com"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("access_denied")]
    public async Task Callbacks_ShouldDeleteTheStateCookie_AndNotCallTheService_WhenTheProviderRefuses(string error)
    {
        var controller = CreateController(stateCookie: "browser-state");

        var result = await controller.Callback(string.IsNullOrEmpty(error) ? "" : "code", "state", error, null);

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcProviderRejected)}");
        SetCookies(controller).Should().ContainSingle(c => c.StartsWith($"{OidcController.StateCookieName}=;", StringComparison.Ordinal));
        _loggerMock.VerifyLog(LogLevel.Warning, $"The identity provider returned to the callback with error {error}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_ShouldReportTheGenericError_ForAnUnexpectedFailure()
    {
        var failure = new InvalidOperationException("error.looks-like-a-key");
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);
        _oidcServiceMock.Setup(x => x.StartLoginAsync("https://games.example.com", null)).ThrowsAsync(failure);
        var controller = CreateController();

        var result = await controller.Login(null);

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcFailed)}");
        SetCookies(controller).Should().BeEmpty();
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.StartLoginAsync("https://games.example.com", null), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "OIDC login initiated");
        _loggerMock.VerifyLog(LogLevel.Warning, "OIDC login at the identity provider could not start", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_ShouldReportAFailure_WhenACancellationDidNotComeFromTheBrowser()
    {
        var failure = new OperationCanceledException();
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);
        _oidcServiceMock.Setup(x => x.StartLoginAsync("https://games.example.com", null)).ThrowsAsync(failure);
        var controller = CreateController();

        var result = await controller.Login(null);

        result.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be($"/auth-callback?error={Uri.EscapeDataString(Constants.Errors.OidcFailed)}");
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.StartLoginAsync("https://games.example.com", null), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "OIDC login initiated");
        _loggerMock.VerifyLog(LogLevel.Warning, "OIDC login at the identity provider could not start", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Flows_ShouldPassOnCancellation_WhenTheBrowserWentAway()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);
        _oidcServiceMock.Setup(x => x.StartLoginAsync("https://games.example.com", null)).ThrowsAsync(new OperationCanceledException());
        _oidcServiceMock.Setup(x => x.CompleteLoginAsync("code", "state", null, null)).ThrowsAsync(new OperationCanceledException());
        _oidcServiceMock.Setup(x => x.CompleteLinkAsync("code", "state", null, null)).ThrowsAsync(new OperationCanceledException());
        var controller = CreateController();
        controller.HttpContext.RequestAborted = aborted.Token;

        var login = () => controller.Login(null);
        var callback = () => controller.Callback("code", "state", null, null);
        var link = () => controller.LinkCallback("code", "state", null, null);

        await login.Should().ThrowAsync<OperationCanceledException>();
        await callback.Should().ThrowAsync<OperationCanceledException>();
        await link.Should().ThrowAsync<OperationCanceledException>();
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.StartLoginAsync("https://games.example.com", null), Times.Once);
        _oidcServiceMock.Verify(x => x.CompleteLoginAsync("code", "state", null, null), Times.Once);
        _oidcServiceMock.Verify(x => x.CompleteLinkAsync("code", "state", null, null), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "OIDC login initiated");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkLogin_ShouldRefuse_WithoutAUserId()
    {
        var controller = CreateController();
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var act = () => controller.LinkLogin();

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(Constants.Errors.NotAuthenticated);
        VerifyNoOtherCalls();
    }
}
