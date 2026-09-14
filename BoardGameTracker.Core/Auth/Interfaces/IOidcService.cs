using BoardGameTracker.Common.DTOs.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IOidcService
{
    Task<OidcProviderInfo?> GetEnabledProviderAsync();
    Task<OidcAuthorizationRequest> StartLoginAsync(string providerName, string publicBaseUrl, string? redirectPath);
    Task<OidcAuthorizationRequest> StartLinkAsync(string userId, string providerName, string publicBaseUrl);
    Task<OidcLoginResult> CompleteLoginAsync(string providerName, string code, string? state, string? browserState);
    Task<OidcLinkResult> CompleteLinkAsync(string providerName, string code, string? state, string? browserState);
    Task<OidcDiscoveryResultDto> TestDiscoveryAsync(string authority);
    string CreateHandoff(LoginResponse login);
    LoginResponse? TakeHandoff(string? handoffKey);
    Task<List<ExternalLoginDto>> GetExternalLoginsAsync(string userId);
    Task UnlinkExternalLoginAsync(string userId, int externalLoginId);
    Task<bool> HasEnabledProviderAsync();
}

public record OidcAuthorizationRequest(string Url, string State);

public record OidcLoginResult(LoginResponse Login, string RedirectPath);

public record OidcLinkResult(string UserId, string ProviderName);
