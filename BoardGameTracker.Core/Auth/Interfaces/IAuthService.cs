using BoardGameTracker.Common.DTOs.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshAsync(string refreshToken);
    Task LogoutAsync(string userId, string? refreshToken);
    Task<UserDto> RegisterAsync(RegisterRequest request);
    Task<ProfileResponse> GetProfileAsync(string userId);
    Task<ProfileResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<ResetPasswordResponse> ResetPasswordAsync(string userId);
    AuthStatusResponse GetStatus();
}
