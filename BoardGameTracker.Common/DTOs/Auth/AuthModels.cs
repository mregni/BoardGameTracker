namespace BoardGameTracker.Common.DTOs.Auth;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record RefreshTokenRequest(string RefreshToken);

public record LogoutRequest(string? RefreshToken);

public record UpdateProfileRequest(string? DisplayName, string? Email, int? PlayerId);

public record ProfileResponse(
    string Id,
    string Username,
    string? Email,
    string? DisplayName,
    IEnumerable<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    int? PlayerId);

public record AuthStatusResponse(bool AuthEnabled);

public record ForgotPasswordRequest(string Username);

public record ResetPasswordConfirmRequest(string UserId, string Token, string NewPassword);
