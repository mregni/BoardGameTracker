using BoardGameTracker.Common.DTOs.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IUserAdminService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto> GetByIdAsync(string userId);
    Task DeleteAsync(string userId, string currentUserId);
    Task<UserDto> UpdateRoleAsync(string userId, string role, string currentUserId);
    Task<UserDto> UpdateUserAsync(string userId, string username, string? email, string role, string currentUserId);
}
