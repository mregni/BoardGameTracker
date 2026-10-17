using BoardGameTracker.Common.DTOs.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IUserAdminService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto> GetByIdAsync(string userId);
    Task DeleteAsync(string userId, string currentUserId);
    Task<UserDto> UpdateRoleAsync(string userId, string role, string currentUserId);
    Task<UserDto> UpdateUserAsync(string userId, string username, string? email, string role, int? playerId, string currentUserId);
    Task<UserDto> DisableAsync(string userId, string currentUserId);
    Task<UserDto> EnableAsync(string userId, string currentUserId);
    Task<List<ExternalLoginDto>> GetExternalLoginsAsync(string userId);
    Task UnlinkExternalLoginAsync(string userId, int externalLoginId, string currentUserId);
    Task<UserDto> ManageRoleByIdentityProviderAsync(string userId, string currentUserId);
}
