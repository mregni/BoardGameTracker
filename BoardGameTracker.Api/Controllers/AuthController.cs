using System.Security.Claims;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
[ServiceFilter(typeof(AuthDisabledFilter))]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOidcService _oidcService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IOidcService oidcService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _oidcService = oidcService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user {Username}", request.Username);
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshAsync(request.RefreshToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var userId = GetCurrentUserId();
        await _authService.LogoutAsync(userId, request.RefreshToken);
        return Ok();
    }

    [HttpPost("register")]
    [Authorize(Roles = Constants.AuthRoles.Admin)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        _logger.LogDebug("Admin {AdminId} registering new user {Username}", GetCurrentUserId(), request.Username);
        var user = await _authService.RegisterAsync(request);
        return Ok(user);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _authService.GetProfileAsync(GetCurrentUserId());
        return Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var profile = await _authService.UpdateProfileAsync(GetCurrentUserId(), request);
        return Ok(profile);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await _authService.ChangePasswordAsync(GetCurrentUserId(), request);
        return Ok();
    }

    [HttpPost("reset-password/{userId}")]
    [Authorize(Roles = Constants.AuthRoles.Admin)]
    public async Task<IActionResult> ResetPassword(string userId)
    {
        _logger.LogInformation("Admin {AdminId} resetting password for user {UserId}", GetCurrentUserId(), userId);
        var response = await _authService.ResetPasswordAsync(userId);
        return Ok(response);
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        var status = _authService.GetStatus();
        return Ok(status);
    }

    [HttpGet("external-logins")]
    public async Task<IActionResult> GetExternalLogins()
    {
        var logins = await _oidcService.GetExternalLoginsAsync(GetCurrentUserId());
        return Ok(logins);
    }

    [HttpDelete("external-logins/{id:int}")]
    public async Task<IActionResult> UnlinkExternalLogin(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} unlinking external login {LoginId}", userId, id);
        await _oidcService.UnlinkExternalLoginAsync(userId, id);
        return NoContent();
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User not authenticated");
}
