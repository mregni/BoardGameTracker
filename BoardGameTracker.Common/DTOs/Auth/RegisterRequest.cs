namespace BoardGameTracker.Common.DTOs.Auth;

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? DisplayName);
