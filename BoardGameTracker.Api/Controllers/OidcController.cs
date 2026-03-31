using System.Security.Claims;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/auth/oidc")]
[ServiceFilter(typeof(AuthDisabledFilter))]
public class OidcController : ControllerBase
{
    private readonly IOidcService _oidcService;
    private readonly ILogger<OidcController> _logger;

    public OidcController(IOidcService oidcService, ILogger<OidcController> logger)
    {
        _oidcService = oidcService;
        _logger = logger;
    }

    [HttpGet("provider")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProvider()
    {
        var provider = await _oidcService.GetEnabledProviderAsync();
        if (provider == null)
        {
            return Ok();
        }

        return Ok(provider);
    }

    [HttpGet("{provider}/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string provider, [FromQuery] string redirectUri, [FromQuery] string? state = null)
    {
        _logger.LogInformation("OIDC login initiated for provider {Provider}", provider);
        var url = await _oidcService.GetAuthorizationUrlAsync(provider, redirectUri, state);
        return Ok(new AuthorizationUrlResponse(url));
    }

    [HttpGet("{provider}/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string code, [FromQuery] string redirectUri, [FromQuery] string? state = null)
    {
        _logger.LogInformation("OIDC callback received for provider {Provider}", provider);
        var response = await _oidcService.HandleCallbackAsync(provider, code, redirectUri, state);
        return Ok(response);
    }

    [HttpGet("{provider}/link")]
    [Authorize]
    public async Task<IActionResult> LinkLogin(string provider, [FromQuery] string redirectUri, [FromQuery] string? state = null)
    {
        _logger.LogInformation("OIDC link initiated for provider {Provider} by user {UserId}", provider, GetCurrentUserId());
        var url = await _oidcService.GetAuthorizationUrlAsync(provider, redirectUri, state);
        return Ok(new AuthorizationUrlResponse(url));
    }

    [HttpGet("{provider}/link-callback")]
    [Authorize]
    public async Task<IActionResult> LinkCallback(string provider, [FromQuery] string code, [FromQuery] string redirectUri, [FromQuery] string? state = null)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("OIDC link callback received for provider {Provider} by user {UserId}", provider, userId);
        var response = await _oidcService.HandleLinkCallbackAsync(userId, provider, code, redirectUri, state);
        return Ok(response);
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User not authenticated");
}
