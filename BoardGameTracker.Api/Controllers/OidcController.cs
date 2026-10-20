using System.Security.Claims;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/auth/oidc")]
[ServiceFilter(typeof(AuthDisabledFilter))]
public class OidcController : ControllerBase
{
    public const string StateCookieName = "bgt_oidc_state";
    public const string HandoffCookieName = "bgt_oidc_handoff";
    public const string SessionCookieName = "bgt_oidc_session";
    public const string CallbackPage = "/auth-callback";
    private const string CookiePath = "/api/auth/oidc";

    private readonly IOidcService _oidcService;
    private readonly IPublicUrlBuilder _publicUrlBuilder;
    private readonly AuthSessionCookies _sessionCookies;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<OidcController> _logger;

    public OidcController(IOidcService oidcService, IPublicUrlBuilder publicUrlBuilder, AuthSessionCookies sessionCookies, IOptions<JwtOptions> jwtOptions, ILogger<OidcController> logger)
    {
        _oidcService = oidcService;
        _publicUrlBuilder = publicUrlBuilder;
        _sessionCookies = sessionCookies;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    [HttpGet("provider")]
    [AllowAnonymous]
    [ProducesResponseType<OidcProviderInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvider()
    {
        var provider = await _oidcService.GetEnabledProviderAsync();
        if (provider == null)
        {
            return NotFound();
        }

        return Ok(provider with { HttpsRequired = !await OidcTransportPolicy.IsAllowedAsync(Request, _publicUrlBuilder) });
    }

    [HttpGet("{provider}/login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Oidc)]
    [RequireOidcHttps(OidcHttpsRefusal.LoginPage)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Login(string provider, [FromQuery] string? redirect)
    {
        var configuredBaseUrl = await _publicUrlBuilder.GetConfiguredBaseUrlAsync();
        var publicHostLogin = PublicHostRedirect(Request, configuredBaseUrl);
        if (publicHostLogin != null)
        {
            _logger.LogInformation("OIDC login for provider {Provider} was started on another address than the public URL; continuing on the public URL", provider);
            return Redirect(publicHostLogin);
        }

        _logger.LogInformation("OIDC login initiated for provider {Provider}", provider);
        try
        {
            var request = await _oidcService.StartLoginAsync(provider, configuredBaseUrl ?? $"{Request.Scheme}://{Request.Host}", redirect);
            SetCookie(StateCookieName, request.State, OidcService.PendingAuthorizationLifetime);
            return Redirect(request.Url);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "OIDC login via {Provider} could not start", provider);
            return Fail(ex is EntityNotFoundException ? Constants.Errors.OidcProviderUnavailable : ErrorKey(ex));
        }
    }

    [HttpGet("{provider}/callback")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Oidc)]
    [RequireOidcHttps(OidcHttpsRefusal.CallbackPage)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, [FromQuery] string? iss)
    {
        var browserState = Request.Cookies[StateCookieName];
        DeleteCookie(StateCookieName);

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        {
            _logger.LogWarning("OIDC provider {Provider} returned to the callback with error {Error}", provider, error ?? "missing code");
            return Fail(Constants.Errors.OidcProviderRejected);
        }

        try
        {
            var result = await _oidcService.CompleteLoginAsync(provider, code, state, browserState, iss);
            SetCookie(HandoffCookieName, _oidcService.CreateHandoff(result.Login), OidcService.HandoffLifetime);
            SetSessionCookie(result.SessionKey);
            return LocalRedirect($"{CallbackPage}?redirect={Uri.EscapeDataString(result.RedirectPath)}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "OIDC login via {Provider} failed", provider);
            return Fail(ErrorKey(ex));
        }
    }

    [HttpPost("adopt")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Oidc)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public IActionResult Adopt()
    {
        var login = _oidcService.TakeHandoff(Request.Cookies[HandoffCookieName]);
        DeleteCookie(HandoffCookieName);
        if (login == null)
        {
            throw new AuthenticationFailedException(Constants.Errors.OidcHandoffExpired);
        }

        _sessionCookies.Issue(HttpContext, login.RefreshToken, login.User.Id);
        return Ok(login);
    }

    [HttpGet("{provider}/link")]
    [Authorize]
    [RequireOidcHttps(OidcHttpsRefusal.BadRequest)]
    [ProducesResponseType<AuthorizationUrlResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> LinkLogin(string provider)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("OIDC link initiated for provider {Provider} by user {UserId}", provider, userId);
        var request = await _oidcService.StartLinkAsync(userId, provider, await PublicBaseUrlAsync());
        SetCookie(StateCookieName, request.State, OidcService.PendingAuthorizationLifetime);
        return Ok(new AuthorizationUrlResponse(request.Url));
    }

    [HttpGet("{provider}/link-callback")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Oidc)]
    [RequireOidcHttps(OidcHttpsRefusal.CallbackPage)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> LinkCallback(string provider, [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, [FromQuery] string? iss)
    {
        var browserState = Request.Cookies[StateCookieName];
        DeleteCookie(StateCookieName);

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        {
            _logger.LogWarning("OIDC provider {Provider} returned to the link callback with error {Error}", provider, error ?? "missing code");
            return Fail(Constants.Errors.OidcProviderRejected);
        }

        try
        {
            var result = await _oidcService.CompleteLinkAsync(provider, code, state, browserState, iss);
            return LocalRedirect($"{CallbackPage}?linked={Uri.EscapeDataString(result.ProviderName)}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "OIDC link via {Provider} failed", provider);
            return Fail(ErrorKey(ex));
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType<OidcLogoutResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var sessionKey = Request.Cookies[SessionCookieName];
        DeleteCookie(SessionCookieName);
        var url = await _oidcService.EndSessionAsync(sessionKey, await PublicBaseUrlAsync());
        return Ok(new OidcLogoutResponse(url));
    }

    private void SetSessionCookie(string? sessionKey)
    {
        if (sessionKey == null)
        {
            DeleteCookie(SessionCookieName);
            return;
        }

        SetCookie(SessionCookieName, sessionKey, TimeSpan.FromDays(_jwtOptions.RefreshTokenExpiryDays));
    }

    private LocalRedirectResult Fail(string errorKey)
    {
        return LocalRedirect($"{CallbackPage}?error={Uri.EscapeDataString(errorKey)}");
    }

    internal static string? PublicHostRedirect(HttpRequest request, string? configuredBaseUrl)
    {
        if (configuredBaseUrl == null
            || !Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var publicUri)
            || !Uri.TryCreate(request.Headers.Referer.ToString(), UriKind.Absolute, out var referer)
            || OriginEquals(publicUri, referer))
        {
            return null;
        }

        return $"{configuredBaseUrl.TrimEnd('/')}{request.Path}{request.QueryString}";
    }

    private static bool OriginEquals(Uri left, Uri right) =>
        string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.IdnHost, right.IdnHost, StringComparison.OrdinalIgnoreCase)
        && left.Port == right.Port;

    internal static string ErrorKey(Exception exception)
    {
        return exception switch
        {
            DomainException or ValidationException or AuthenticationFailedException
                when exception.Message.StartsWith("error.", StringComparison.Ordinal) => exception.Message,
            EntityNotFoundException => Constants.Errors.InvalidAuthSession,
            _ => Constants.Errors.OidcFailed,
        };
    }

    private async Task<string> PublicBaseUrlAsync()
    {
        return (await PublicBaseUrl.ResolveAsync(_publicUrlBuilder, Request)).BaseUrl;
    }

    private void SetCookie(string name, string value, TimeSpan lifetime)
    {
        Response.Cookies.Append(name, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = CookiePath,
            MaxAge = lifetime,
            IsEssential = true,
        });
    }

    private void DeleteCookie(string name)
    {
        Response.Cookies.Delete(name, new CookieOptions { Path = CookiePath, HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AuthenticationFailedException(Constants.Errors.NotAuthenticated);
}
