using BoardGameTracker.Common.DTOs.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IOidcService
{
    Task<IList<OidcProviderInfo>> GetEnabledProvidersAsync();
    Task<string> GetAuthorizationUrlAsync(string providerName, string redirectUri, string? state = null);
    Task<LoginResponse> HandleCallbackAsync(string providerName, string code, string redirectUri);
    Task LinkExternalLoginAsync(string userId, string providerName, string providerKey, string? displayName = null);
    Task UnlinkExternalLoginAsync(string userId, int externalLoginId);
}
