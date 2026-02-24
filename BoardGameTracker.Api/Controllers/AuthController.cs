using System.Security.Claims;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        user.UpdateLastLogin();
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

        _logger.LogInformation("User {Username} logged in", user.UserName);

        return Ok(new LoginResponse(
            accessToken,
            refreshToken.Token,
            _tokenService.GetAccessTokenExpiry(),
            new UserInfo(user.Id, user.UserName!, user.DisplayName, roles)));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var existingToken = await _tokenService.GetRefreshTokenAsync(request.RefreshToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        var user = existingToken.User!;
        var roles = await _userManager.GetRolesAsync(user);

        // Rotate refresh token
        var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);
        await _tokenService.RevokeRefreshTokenAsync(existingToken, "Replaced by new token", newRefreshToken.Token);

        var accessToken = _tokenService.GenerateAccessToken(user, roles);

        return Ok(new LoginResponse(
            accessToken,
            newRefreshToken.Token,
            _tokenService.GetAccessTokenExpiry(),
            new UserInfo(user.Id, user.UserName!, user.DisplayName, roles)));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            var token = await _tokenService.GetRefreshTokenAsync(request.RefreshToken);
            if (token != null && token.UserId == userId)
            {
                await _tokenService.RevokeRefreshTokenAsync(token, "Logged out");
            }
        }
        else
        {
            await _tokenService.RevokeAllUserTokensAsync(userId, "Logged out");
        }

        return Ok();
    }

    [HttpPost("register")]
    [Authorize(Roles = Constants.AuthRoles.Admin)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Username already exists" });
        }

        var user = new ApplicationUser(request.Username, request.Email, request.DisplayName);
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        await _userManager.AddToRoleAsync(user, Constants.AuthRoles.Reader);

        _logger.LogInformation("Admin created new user: {Username}", request.Username);

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto(
            user.Id, user.UserName!, user.Email, user.DisplayName,
            roles, user.CreatedAt, user.LastLoginAt, user.PlayerId));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserInfo(user.Id, user.UserName!, user.DisplayName, roles));
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new ProfileResponse(
            user.Id, user.UserName!, user.Email, user.DisplayName,
            roles, user.CreatedAt, user.LastLoginAt, user.PlayerId));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.UpdateDisplayName(request.DisplayName);
        if (request.Email != null)
        {
            user.Email = request.Email;
        }

        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new ProfileResponse(
            user.Id, user.UserName!, user.Email, user.DisplayName,
            roles, user.CreatedAt, user.LastLoginAt, user.PlayerId));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        return Ok();
    }

    [HttpPost("reset-password/{userId}")]
    [Authorize(Roles = Constants.AuthRoles.Admin)]
    public async Task<IActionResult> ResetPassword(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var tempPassword = Guid.NewGuid().ToString("N")[..12] + "A1!";
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        _logger.LogInformation("Admin reset password for user {Username}. Temp password: {TempPassword}",
            user.UserName, tempPassword);

        return Ok(new { tempPassword });
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        var authBypass = Environment.GetEnvironmentVariable("AUTH_BYPASS")?.ToLower() == "true";
        return Ok(new AuthStatusResponse(AuthEnabled: true, BypassEnabled: authBypass));
    }
}
