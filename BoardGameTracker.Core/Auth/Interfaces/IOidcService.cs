using BoardGameTracker.Common.DTOs.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IOidcService
{
    Task<OidcProviderInfo?> GetEnabledProviderAsync();
    Task<string> GetAuthorizationUrlAsync(string providerName, string redirectUri, string? state = null);
    Task<LoginResponse> HandleCallbackAsync(string providerName, string code, string redirectUri, string? state = null);
    Task<LoginResponse> HandleLinkCallbackAsync(string userId, string providerName, string code, string redirectUri, string? state = null);
    Task<List<ExternalLoginDto>> GetExternalLoginsAsync(string userId);
    Task UnlinkExternalLoginAsync(string userId, int externalLoginId);
    Task<bool> HasEnabledProviderAsync();
}
