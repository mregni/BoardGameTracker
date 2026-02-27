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
    private readonly MainDbContext _context;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        UserManager<ApplicationUser> userManager,
        MainDbContext context,
        ILogger<UserAdminService> logger)
    {
        _userManager = userManager;
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

        return usersWithRoles.Select(x => x.User.ToDto(x.Roles)).ToList();
    }

    public async Task<UserDto> GetByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        var roles = await _userManager.GetRolesAsync(user);
        return user.ToDto(roles);
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

        _logger.LogInformation("Admin {AdminId} deleted user {UserId} ({Username})",
            currentUserId, userId, user.UserName);
    }

    public async Task<UserDto> UpdateRoleAsync(string userId, string role, string currentUserId)
    {
        if (role != Constants.AuthRoles.Admin && role != Constants.AuthRoles.User)
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

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);

        _logger.LogInformation("Admin {AdminId} updated role for user {UserId} ({Username}) to {Role}",
            currentUserId, userId, user.UserName, role);

        var updatedRoles = await _userManager.GetRolesAsync(user);
        return user.ToDto(updatedRoles);
    }
}
