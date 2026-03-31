namespace BoardGameTracker.Common.DTOs.Auth;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User);

public record UserInfo(
    string Id,
    string Username,
    string? DisplayName,
    IEnumerable<string> Roles);
