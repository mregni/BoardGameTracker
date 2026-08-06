namespace BoardGameTracker.Common.DTOs.Auth;

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? Role,
    bool CreatePlayer = false,
    int? PlayerId = null);
