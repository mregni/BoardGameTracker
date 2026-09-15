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

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/auth/oidc")]
[ServiceFilter(typeof(AuthDisabledFilter))]
public class OidcController : ControllerBase
{
    public const string StateCookieName = "bgt_oidc_state";
    public const string HandoffCookieName = "bgt_oidc_handoff";
    public const string CallbackPage = "/auth-callback";
    private const string CookiePath = "/api/auth/oidc";

    private readonly IOidcService _oidcService;
    private readonly IPublicUrlBuilder _publicUrlBuilder;
    private readonly IProfileImageTicketService _imageTickets;
    private readonly ILogger<OidcController> _logger;

    public OidcController(IOidcService oidcService, IPublicUrlBuilder publicUrlBuilder, IProfileImageTicketService imageTickets, ILogger<OidcController> logger)
    {
        _oidcService = oidcService;
        _publicUrlBuilder = publicUrlBuilder;
        _imageTickets = imageTickets;
        _logger = logger;
    }

    [HttpGet("provider")]
    [AllowAnonymous]
    [ProducesResponseType<OidcProviderInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvider()
    {
        var provider = await _oidcService.GetEnabledProviderAsync();
        return provider == null ? NotFound() : Ok(provider);
    }

    [HttpGet("{provider}/login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Login(string provider, [FromQuery] string? redirect)
    {
        _logger.LogInformation("OIDC login initiated for provider {Provider}", provider);
        var request = await _oidcService.StartLoginAsync(provider, await PublicBaseUrlAsync(), redirect);
        SetCookie(StateCookieName, request.State, OidcService.PendingAuthorizationLifetime);
        return Redirect(request.Url);
    }

    [HttpGet("{provider}/callback")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        var browserState = Request.Cookies[StateCookieName];
        DeleteCookie(StateCookieName);

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        {
            _logger.LogWarning("OIDC provider {Provider} returned to the callback with error {Error}", provider, error ?? "missing code");
            return await FailAsync(Constants.Errors.OidcProviderRejected);
        }

        try
        {
            var result = await _oidcService.CompleteLoginAsync(provider, code, state, browserState);
            SetCookie(HandoffCookieName, _oidcService.CreateHandoff(result.Login), OidcService.HandoffLifetime);
            return Redirect(await SpaUrlAsync($"{CallbackPage}?redirect={Uri.EscapeDataString(result.RedirectPath)}"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OIDC login via {Provider} failed", provider);
            return await FailAsync(ErrorKey(ex));
        }
    }

    [HttpPost("adopt")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public IActionResult Adopt()
    {
        var login = _oidcService.TakeHandoff(Request.Cookies[HandoffCookieName]);
        DeleteCookie(HandoffCookieName);
        if (login == null)
        {
            throw new AuthenticationFailedException(Constants.Errors.OidcHandoffExpired);
        }

        ProfileImageCookie.Issue(HttpContext, _imageTickets.Issue(), _imageTickets.Lifetime);
        return Ok(login);
    }

    [HttpGet("{provider}/link")]
    [Authorize]
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
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> LinkCallback(string provider, [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        var browserState = Request.Cookies[StateCookieName];
        DeleteCookie(StateCookieName);

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        {
            _logger.LogWarning("OIDC provider {Provider} returned to the link callback with error {Error}", provider, error ?? "missing code");
            return await FailAsync(Constants.Errors.OidcProviderRejected);
        }

        try
        {
            var result = await _oidcService.CompleteLinkAsync(provider, code, state, browserState);
            return Redirect(await SpaUrlAsync($"{CallbackPage}?linked={Uri.EscapeDataString(result.ProviderName)}"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OIDC link via {Provider} failed", provider);
            return await FailAsync(ErrorKey(ex));
        }
    }

    private async Task<IActionResult> FailAsync(string errorKey)
    {
        return Redirect(await SpaUrlAsync($"{CallbackPage}?error={Uri.EscapeDataString(errorKey)}"));
    }

    private static string ErrorKey(Exception exception)
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

    private async Task<string> SpaUrlAsync(string pathAndQuery)
    {
        return $"{await PublicBaseUrlAsync()}{pathAndQuery}";
    }

    private void SetCookie(string name, string value, TimeSpan lifetime)
    {
        Response.Cookies.Append(name, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = CookiePath,
            MaxAge = lifetime,
            IsEssential = true,
        });
    }

    private void DeleteCookie(string name)
    {
        Response.Cookies.Delete(name, new CookieOptions { Path = CookiePath, HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax });
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AuthenticationFailedException(Constants.Errors.NotAuthenticated);
}
