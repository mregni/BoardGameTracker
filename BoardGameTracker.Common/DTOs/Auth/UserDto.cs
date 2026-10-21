namespace BoardGameTracker.Common.DTOs.Auth;

public record UserDto(
    string Id,
    string Username,
    string? Email,
    string? DisplayName,
    IEnumerable<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    int? PlayerId,
    bool IsDisabled = false,
    bool RoleManagedByIdentityProvider = false,
    int ExternalLoginCount = 0,
    string? ProfileImage = null);
