using BoardGameTracker.Common.DTOs.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IOidcService
{
    Task<OidcProviderInfo?> GetEnabledProviderAsync();
    Task<OidcAuthorizationRequest> StartLoginAsync(string publicBaseUrl, string? redirectPath);
    Task<OidcAuthorizationRequest> StartLinkAsync(string userId, string publicBaseUrl);
    Task<OidcLoginResult> CompleteLoginAsync(string code, string? state, string? browserState, string? issuer);
    Task<OidcLinkResult> CompleteLinkAsync(string code, string? state, string? browserState, string? issuer);
    Task<OidcDiscoveryResultDto> TestDiscoveryAsync(string authority);
    string CreateHandoff(LoginResponse login);
    LoginResponse? TakeHandoff(string? handoffKey);
    Task<List<ExternalLoginDto>> GetExternalLoginsAsync(string userId);
    Task UnlinkExternalLoginAsync(string userId, int externalLoginId);
    Task<string?> EndSessionAsync(string? sessionKey, string publicBaseUrl);
    Task CleanupExpiredSessionsAsync();
}

public record OidcAuthorizationRequest(string Url, string State);

public record OidcLoginResult(LoginResponse Login, string RedirectPath, string? SessionKey = null);

public record OidcLinkResult(string UserId);
