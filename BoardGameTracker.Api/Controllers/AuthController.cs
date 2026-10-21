using System.Security.Claims;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Common;
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
    private readonly AuthSessionCookies _sessionCookies;
    private readonly IUserProfileImageService _profileImages;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IOidcService oidcService, AuthSessionCookies sessionCookies, IUserProfileImageService profileImages, ILogger<AuthController> logger)
    {
        _authService = authService;
        _sessionCookies = sessionCookies;
        _profileImages = profileImages;
        _oidcService = oidcService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogDebug("Login attempt received");
        var response = await _authService.LoginAsync(request, ClientAddressKey.From(HttpContext.Connection.RemoteIpAddress));
        _sessionCookies.Issue(HttpContext, response.RefreshToken, response.User.Id);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    [RequireSessionHeader]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Refresh()
    {
        try
        {
            var refreshToken = AuthSessionCookies.ReadRefreshToken(Request)
                ?? throw new AuthenticationFailedException(Constants.Errors.InvalidRefreshToken);
            var response = await _authService.RefreshAsync(refreshToken);
            _sessionCookies.Issue(HttpContext, response.RefreshToken, response.User.Id);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex) when (ex.Message != Constants.Errors.RefreshTokenSuperseded)
        {
            var context = HttpContext;
            context.Response.OnStarting(() =>
            {
                AuthSessionCookies.Clear(context);
                return Task.CompletedTask;
            });
            throw;
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [RequireSessionHeader]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        await _authService.LogoutAsync(userId, AuthSessionCookies.ReadRefreshToken(Request));
        AuthSessionCookies.Clear(HttpContext);
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

    [HttpPut("profile/image")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadProfileImage([FromForm] UploadProfileImageCommand command)
    {
        var profile = await _profileImages.UploadAsync(GetCurrentUserId(), command.File);
        return Ok(profile);
    }

    [HttpDelete("profile/image")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveProfileImage()
    {
        var profile = await _profileImages.RemoveAsync(GetCurrentUserId());
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
    [EnableRateLimiting(RateLimitPolicies.Auth)]
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
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request.Username);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
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
