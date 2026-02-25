using BoardGameTracker.Common.Entities.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    Task<RefreshToken> GenerateRefreshTokenAsync(string userId);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(RefreshToken token, string? reason = null, string? replacedByToken = null);
    Task RevokeAllUserTokensAsync(string userId, string? reason = null);
    DateTime GetAccessTokenExpiry();
    Task CleanupExpiredTokensAsync();
}
