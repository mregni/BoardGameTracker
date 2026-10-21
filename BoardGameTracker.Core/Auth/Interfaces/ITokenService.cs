using BoardGameTracker.Common.Entities.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    Task<RefreshToken> GenerateRefreshTokenAsync(string userId);
    Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken current);
    Task<RefreshToken> SupersedeRefreshTokenAsync(RefreshToken unused);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken?> GetSuccessorAsync(RefreshToken token);
    Task RevokeRefreshTokenAsync(RefreshToken token, string? reason = null);
    Task RevokeAllUserTokensAsync(string userId, string? reason = null);
    DateTime GetAccessTokenExpiry();
    Task CleanupExpiredTokensAsync();
}
