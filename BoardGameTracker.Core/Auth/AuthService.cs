using System.Security.Cryptography;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly MainDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IEnvironmentProvider environmentProvider,
        MainDbContext context,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _environmentProvider = environmentProvider;
        _context = context;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            _logger.LogWarning("Failed login attempt for unknown username {Username}", request.Username);
            throw new UnauthorizedAccessException(Constants.Errors.InvalidCredentials);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed login attempt for user {Username}: invalid password", request.Username);
            throw new UnauthorizedAccessException(Constants.Errors.InvalidCredentials);
        }

        user.UpdateLastLogin();
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

        _logger.LogInformation("User {Username} logged in", user.UserName);

        return new LoginResponse(
            accessToken,
            refreshToken.Token,
            _tokenService.GetAccessTokenExpiry(),
            user.ToUserInfo(roles));
    }

    public async Task<LoginResponse> RefreshAsync(string refreshToken)
    {
        var existingToken = await _tokenService.GetRefreshTokenAsync(refreshToken);
        if (existingToken == null || !existingToken.IsActive)
        {
            throw new UnauthorizedAccessException(Constants.Errors.InvalidRefreshToken);
        }

        var user = existingToken.User!;
        var roles = await _userManager.GetRolesAsync(user);

        var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);
        await _tokenService.RevokeRefreshTokenAsync(existingToken, "Replaced by new token", newRefreshToken.Token);

        var accessToken = _tokenService.GenerateAccessToken(user, roles);

        return new LoginResponse(
            accessToken,
            newRefreshToken.Token,
            _tokenService.GetAccessTokenExpiry(),
            user.ToUserInfo(roles));
    }

    public async Task LogoutAsync(string userId, string? refreshToken)
    {
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var token = await _tokenService.GetRefreshTokenAsync(refreshToken);
            if (token != null && token.UserId == userId)
            {
                await _tokenService.RevokeRefreshTokenAsync(token, "Logged out");
            }
        }
        else
        {
            await _tokenService.RevokeAllUserTokensAsync(userId, "Logged out");
        }

        _logger.LogInformation("User {UserId} logged out", userId);
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        var hasOidcProvider = await _context.OidcProviders.AnyAsync(p => p.Enabled);
        if (hasOidcProvider)
        {
            throw new DomainException(Constants.Errors.OidcNoLocalUsers);
        }

        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            throw new DomainException(Constants.Errors.UsernameAlreadyExists);
        }

        var role = request.Role ?? Constants.AuthRoles.User;
        if (!Constants.AuthRoles.AllRoles.Contains(role))
        {
            throw new ValidationException(Constants.Errors.InvalidRole);
        }

        var user = new ApplicationUser(request.Username, request.Email, request.Username);
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ValidationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, role);

        _logger.LogInformation("Admin created new user: {Username} with role {Role}", request.Username, role);

        var roles = await _userManager.GetRolesAsync(user);
        return user.ToDto(roles);
    }

    public async Task<ProfileResponse> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        var roles = await _userManager.GetRolesAsync(user);
        return user.ToProfileDto(roles);
    }

    public async Task<ProfileResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        user.UpdateDisplayName(request.DisplayName);
        if (request.Email != null)
        {
            user.UpdateEmail(request.Email);
        }

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {UserId} updated their profile", userId);

        var roles = await _userManager.GetRolesAsync(user);
        return user.ToProfileDto(roles);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        if (!await _userManager.HasPasswordAsync(user))
        {
            throw new DomainException(Constants.Errors.CannotChangeOidcPassword);
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed password change attempt for user {UserId}", userId);
            throw new ValidationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        _logger.LogDebug("User {UserId} changed their password", userId);
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        var tempPassword = GenerateTempPassword();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);
        if (!result.Succeeded)
        {
            throw new ValidationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("Admin reset password for user {Username}", user.UserName);

        return new ResetPasswordResponse(tempPassword);
    }

    public AuthStatusResponse GetStatus()
    {
        return new AuthStatusResponse(AuthEnabled: _environmentProvider.AuthEnabled);
    }

    private static string GenerateTempPassword(int length = 16)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%";
        return string.Create(length, chars, static (span, chars) =>
        {
            var bytes = RandomNumberGenerator.GetBytes(span.Length);
            for (var i = 0; i < span.Length; i++)
                span[i] = chars[bytes[i] % chars.Length];
        });
    }
}
