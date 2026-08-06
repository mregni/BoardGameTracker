using System.Net;
using System.Security.Cryptography;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Email.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
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
    private readonly IPlayerService _playerService;
    private readonly IEmailService _emailService;
    private readonly IPublicUrlBuilder _publicUrlBuilder;
    private readonly MainDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IEnvironmentProvider environmentProvider,
        IPlayerService playerService,
        IEmailService emailService,
        IPublicUrlBuilder publicUrlBuilder,
        MainDbContext context,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _environmentProvider = environmentProvider;
        _playerService = playerService;
        _emailService = emailService;
        _publicUrlBuilder = publicUrlBuilder;
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

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Login attempt for locked out user {Username}", request.Username);
            throw new UnauthorizedAccessException(Constants.Errors.AccountLockedOut);
        }

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

        if (!request.CreatePlayer && request.PlayerId.HasValue)
        {
            await PlayerLinkGuard.EnsureLinkableAsync(_context, request.PlayerId.Value, null);
        }

        var user = new ApplicationUser(request.Username, request.Email, request.Username);
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ValidationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, role);

        if (request.CreatePlayer)
        {
            var player = await _playerService.Create(new CreatePlayerCommand
            {
                Name = user.DisplayName ?? request.Username,
                Email = request.Email
            });
            user.LinkPlayer(player.Id);
            await _userManager.UpdateAsync(user);
        }
        else if (request.PlayerId.HasValue)
        {
            user.LinkPlayer(request.PlayerId.Value);
            await _userManager.UpdateAsync(user);
        }

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

        if (request.PlayerId != user.PlayerId)
        {
            if (request.PlayerId.HasValue)
            {
                await PlayerLinkGuard.EnsureLinkableAsync(_context, request.PlayerId.Value, userId);
                user.LinkPlayer(request.PlayerId.Value);
            }
            else
            {
                user.UnlinkPlayer();
            }
        }

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {UserId} updated their profile", userId);

        var roles = await _userManager.GetRolesAsync(user);
        return user.ToProfileDto(roles);
    }

    public async Task<List<PlayerLinkDto>> GetLinkablePlayersAsync(string currentUserId)
    {
        var linkedByOthers = await _context.Users
            .Where(u => u.PlayerId != null && u.Id != currentUserId)
            .Select(u => u.PlayerId!.Value)
            .ToListAsync();

        return await _context.Players
            .Where(p => !linkedByOthers.Contains(p.Id))
            .OrderBy(p => p.Name)
            .Select(p => new PlayerLinkDto(p.Id, p.Name))
            .ToListAsync();
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

        await _tokenService.RevokeAllUserTokensAsync(userId, "Password changed");
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

        await _tokenService.RevokeAllUserTokensAsync(userId, "Password reset by admin");
        _logger.LogInformation("Admin reset password for user {Username}", user.UserName);

        return new ResetPasswordResponse(tempPassword);
    }

    public async Task ForgotPasswordAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            _logger.LogInformation("Forgot-password requested for an unknown account");
            return;
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("Forgot-password requested for user {UserId} but no email address is set", user.Id);
            return;
        }

        if (!_emailService.IsConfigured)
        {
            _logger.LogWarning("Forgot-password requested but email is not configured");
            return;
        }

        try
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = await _publicUrlBuilder.BuildResetUrlAsync(user.Id, token);
            const string subject = "Reset your BoardGameTracker password";
            var htmlUrl = WebUtility.HtmlEncode(resetUrl);
            var body = $"<p>A password reset was requested for your account.</p><p><a href=\"{htmlUrl}\">Reset your password</a></p><p>If you didn't request this, you can safely ignore this email.</p>";
            await _emailService.SendAsync(user.Email, subject, body);
            _logger.LogInformation("Sent password reset email to user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset email");
        }
    }

    public async Task ResetPasswordWithTokenAsync(ResetPasswordConfirmRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            throw new ValidationException(Constants.Errors.InvalidResetToken);
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed password reset attempt for user {UserId}", request.UserId);
            throw new ValidationException(Constants.Errors.InvalidResetToken);
        }

        await _tokenService.RevokeAllUserTokensAsync(user.Id, "Password reset");
        _logger.LogInformation("User {UserId} reset their password via emailed token", user.Id);
    }

    public AuthStatusResponse GetStatus()
    {
        return new AuthStatusResponse(AuthEnabled: _environmentProvider.AuthEnabled);
    }

    private static string GenerateTempPassword(int length = 16)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%";
        return new string(RandomNumberGenerator.GetItems<char>(chars, length));
    }
}
