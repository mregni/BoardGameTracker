using System.Text.Json.Serialization;

namespace BoardGameTracker.Common.DTOs.Auth;

public record LoginResponse(
    string AccessToken,
    [property: JsonIgnore] string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User);

public record UserInfo(
    string Id,
    string Username,
    string? DisplayName,
    IEnumerable<string> Roles,
    string? ProfileImage = null);
