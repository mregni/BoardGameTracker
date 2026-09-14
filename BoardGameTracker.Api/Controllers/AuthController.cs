using System.Security.Claims;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    private readonly IProfileImageTicketService _imageTickets;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IOidcService oidcService, IProfileImageTicketService imageTickets, ILogger<AuthController> logger)
    {
        _authService = authService;
        _imageTickets = imageTickets;
        _oidcService = oidcService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogDebug("Login attempt received");
        var response = await _authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        ProfileImageCookie.Issue(HttpContext, _imageTickets.Issue(), _imageTickets.Lifetime);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshAsync(request.RefreshToken);
        ProfileImageCookie.Issue(HttpContext, _imageTickets.Issue(), _imageTickets.Lifetime);
        return Ok(response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var userId = GetCurrentUserId();
        await _authService.LogoutAsync(userId, request.RefreshToken);
        ProfileImageCookie.Clear(HttpContext);
        return NoContent();
    }

    [HttpPost("register")]
    [Authorize(Roles = Constants.AuthRoles.Admin)]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        _logger.LogDebug("Admin {AdminId} registering new user {Username}", GetCurrentUserId(), request.Username);
        var user = await _authService.RegisterAsync(request);
        return Created((string?)null, user);
    }

    [HttpGet("profile")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _authService.GetProfileAsync(GetCurrentUserId());
        return Ok(profile);
    }

    [HttpPut("profile")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var profile = await _authService.UpdateProfileAsync(GetCurrentUserId(), request);
        return Ok(profile);
    }

    [HttpGet("linkable-players")]
    [ProducesResponseType<List<PlayerLinkDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLinkablePlayers()
    {
        var players = await _authService.GetLinkablePlayersAsync(GetCurrentUserId());
        return Ok(players);
    }

    [HttpPost("change-password")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await _authService.ChangePasswordAsync(GetCurrentUserId(), request);
        return NoContent();
    }

    [HttpPost("reset-password/{userId}")]
    [Authorize(Roles = Constants.AuthRoles.Admin)]
    [ProducesResponseType<ResetPasswordResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(string userId)
    {
        _logger.LogInformation("Admin {AdminId} resetting password for user {UserId}", GetCurrentUserId(), userId);
        var response = await _authService.ResetPasswordAsync(userId);
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request.Username);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPasswordWithToken([FromBody] ResetPasswordConfirmRequest request)
    {
        await _authService.ResetPasswordWithTokenAsync(request);
        return NoContent();
    }

    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType<AuthStatusResponse>(StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        var status = _authService.GetStatus();
        return Ok(status);
    }

    [HttpGet("external-logins")]
    [ProducesResponseType<List<ExternalLoginDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExternalLogins()
    {
        var logins = await _oidcService.GetExternalLoginsAsync(GetCurrentUserId());
        return Ok(logins);
    }

    [HttpDelete("external-logins/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnlinkExternalLogin(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} unlinking external login {LoginId}", userId, id);
        await _oidcService.UnlinkExternalLoginAsync(userId, id);
        return NoContent();
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AuthenticationFailedException(Constants.Errors.NotAuthenticated);
}
