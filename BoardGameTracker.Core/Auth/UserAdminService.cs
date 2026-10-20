using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Datastore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IUserAccessCheck _accessCheck;
    private readonly MainDbContext _context;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IUserAccessCheck accessCheck,
        MainDbContext context,
        ILogger<UserAdminService> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _accessCheck = accessCheck;
        _context = context;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var usersWithRoles = await _context.Users
            .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { User = u, ur.RoleId })
            .Join(_context.Roles, x => x.RoleId, r => r.Id, (x, r) => new { x.User, RoleName = r.Name })
            .GroupBy(x => x.User)
            .Select(g => new { User = g.Key, Roles = g.Select(x => x.RoleName!).ToList() })
            .ToListAsync();

        var linkCounts = await _context.ExternalLogins
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        return usersWithRoles.Select(x => x.User.ToDto(x.Roles, linkCounts.GetValueOrDefault(x.User.Id))).ToList();
    }

    public async Task<UserDto> GetByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        var roles = await _userManager.GetRolesAsync(user);
        return user.ToDto(roles, await CountLinksAsync(userId));
    }

    public async Task DeleteAsync(string userId, string currentUserId)
    {
        if (userId == currentUserId)
        {
            throw new DomainException(Constants.Errors.CannotDeleteSelf);
        }

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        var admins = await _userManager.GetUsersInRoleAsync(Constants.AuthRoles.Admin);
        if (admins.Count == 1 && admins[0].Id == userId)
        {
            throw new DomainException(Constants.Errors.CannotDeleteLastAdmin);
        }

        await _userManager.DeleteAsync(user);
        _accessCheck.Forget(userId);

        _logger.LogInformation("Admin {AdminId} deleted user {UserId} ({Username})",
            currentUserId, userId, user.UserName);
    }

    public async Task<UserDto> UpdateRoleAsync(string userId, string role, string currentUserId)
    {
        if (!Constants.AuthRoles.AllRoles.Contains(role))
        {
            throw new ValidationException(Constants.Errors.InvalidRole);
        }

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        if (userId == currentUserId && role != Constants.AuthRoles.Admin)
        {
            var admins = await _userManager.GetUsersInRoleAsync(Constants.AuthRoles.Admin);
            if (admins.Count == 1)
            {
                throw new DomainException(Constants.Errors.CannotRemoveLastAdmin);
            }
        }

        await ReplaceRoleAsync(user, role);

        _logger.LogInformation("Admin {AdminId} updated role for user {UserId} ({Username}) to {Role}",
            currentUserId, userId, user.UserName, role);

        var updatedRoles = await _userManager.GetRolesAsync(user);
        return user.ToDto(updatedRoles);
    }

    public async Task<UserDto> UpdateUserAsync(string userId, string username, string? email, string role, int? playerId, string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ValidationException(Constants.Errors.UsernameRequired);
        }

        if (!Constants.AuthRoles.AllRoles.Contains(role))
        {
            throw new ValidationException(Constants.Errors.InvalidRole);
        }

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        if (userId == currentUserId && role != Constants.AuthRoles.Admin)
        {
            var admins = await _userManager.GetUsersInRoleAsync(Constants.AuthRoles.Admin);
            if (admins.Count == 1)
            {
                throw new DomainException(Constants.Errors.CannotRemoveLastAdmin);
            }
        }

        if (!string.Equals(user.UserName, username, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _userManager.FindByNameAsync(username);
            if (existingUser != null)
            {
                throw new ValidationException(Constants.Errors.UsernameAlreadyExists);
            }

            await _userManager.SetUserNameAsync(user, username);
        }

        user.UpdateEmail(email);

        if (playerId != user.PlayerId)
        {
            if (playerId.HasValue)
            {
                await PlayerLinkGuard.EnsureLinkableAsync(_context, playerId.Value, userId);
                user.LinkPlayer(playerId.Value);
            }
            else
            {
                user.UnlinkPlayer();
            }
        }

        await _userManager.UpdateAsync(user);

        await ReplaceRoleAsync(user, role);

        _logger.LogInformation("Admin {AdminId} updated user {UserId} (username: {Username}, role: {Role})",
            currentUserId, userId, username, role);

        var updatedRoles = await _userManager.GetRolesAsync(user);
        return user.ToDto(updatedRoles);
    }

    public async Task<UserDto> DisableAsync(string userId, string currentUserId)
    {
        if (userId == currentUserId)
        {
            throw new DomainException(Constants.Errors.CannotDisableSelf);
        }

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        await AdminRoleGate.Lock.WaitAsync();
        try
        {
            if (await _userManager.IsInRoleAsync(user, Constants.AuthRoles.Admin))
            {
                var admins = await _userManager.GetUsersInRoleAsync(Constants.AuthRoles.Admin);
                if (!admins.Any(a => a.Id != userId && !a.IsDisabled))
                {
                    throw new DomainException(Constants.Errors.CannotDisableLastAdmin);
                }
            }

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            _accessCheck.Forget(userId);
        }
        finally
        {
            AdminRoleGate.Lock.Release();
        }

        await _tokenService.RevokeAllUserTokensAsync(userId, "Account disabled");
        var sessions = await _context.OidcSessions.Where(x => x.UserId == userId).ToListAsync();
        if (sessions.Count > 0)
        {
            _context.OidcSessions.RemoveRange(sessions);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Admin {AdminId} disabled user {UserId} ({Username})", currentUserId, userId, user.UserName);
        return await ToDtoAsync(user);
    }

    public async Task<UserDto> EnableAsync(string userId, string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        await _userManager.SetLockoutEndDateAsync(user, null);
        _accessCheck.Forget(userId);

        _logger.LogInformation("Admin {AdminId} enabled user {UserId} ({Username})", currentUserId, userId, user.UserName);
        return await ToDtoAsync(user);
    }

    public async Task<List<ExternalLoginDto>> GetExternalLoginsAsync(string userId)
    {
        await EnsureUserExistsAsync(userId);
        return await _context.ExternalLogins
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.LinkedAt)
            .Select(x => x.ToDto())
            .ToListAsync();
    }

    public async Task UnlinkExternalLoginAsync(string userId, int externalLoginId, string currentUserId)
    {
        var externalLogin = await _context.ExternalLogins
            .FirstOrDefaultAsync(x => x.Id == externalLoginId && x.UserId == userId)
            ?? throw new EntityNotFoundException(nameof(ExternalLogin), externalLoginId);

        _context.ExternalLogins.Remove(externalLogin);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin {AdminId} unlinked external login {LoginId} (provider: {Provider}) from user {UserId}",
            currentUserId, externalLoginId, externalLogin.Provider, userId);
    }

    public async Task<UserDto> ManageRoleByIdentityProviderAsync(string userId, string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        if (await CountLinksAsync(userId) == 0)
        {
            throw new ValidationException(Constants.Errors.OidcRoleNotManaged);
        }

        user.ManageRoleByIdentityProvider();
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Admin {AdminId} handed the role of user {UserId} back to the identity provider", currentUserId, userId);
        return await ToDtoAsync(user);
    }

    private async Task EnsureUserExistsAsync(string userId)
    {
        if (!await _context.Users.AnyAsync(u => u.Id == userId))
        {
            throw new EntityNotFoundException(nameof(ApplicationUser), userId);
        }
    }

    private Task<int> CountLinksAsync(string userId)
    {
        return _context.ExternalLogins.CountAsync(x => x.UserId == userId);
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return user.ToDto(roles, await CountLinksAsync(user.Id));
    }

    private async Task ReplaceRoleAsync(ApplicationUser user, string role)
    {
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count != 1 || currentRoles[0] != role)
        {
            user.ManageRoleLocally();
        }

        if (!currentRoles.Contains(role))
        {
            await _userManager.AddToRoleAsync(user, role);
        }

        var obsoleteRoles = currentRoles.Where(r => r != role).ToList();
        if (obsoleteRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, obsoleteRoles);
        }
    }
}
