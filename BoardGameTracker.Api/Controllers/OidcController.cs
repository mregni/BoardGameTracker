using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/auth/oidc")]
public class OidcController : ControllerBase
{
    private readonly IOidcService _oidcService;

    public OidcController(IOidcService oidcService)
    {
        _oidcService = oidcService;
    }

    [HttpGet("providers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _oidcService.GetEnabledProvidersAsync();
        return Ok(providers);
    }

    [HttpGet("{provider}/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string provider, [FromQuery] string redirectUri, [FromQuery] string? state = null)
    {
        var url = await _oidcService.GetAuthorizationUrlAsync(provider, redirectUri, state);
        return Ok(new { url });
    }

    [HttpGet("{provider}/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string code, [FromQuery] string redirectUri)
    {
        var response = await _oidcService.HandleCallbackAsync(provider, code, redirectUri);
        return Ok(response);
    }
}
