using BoardGameTracker.Common.Entities.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IOidcProviderService
{
    Task<List<OidcProvider>> GetAllAsync();
    Task<OidcProvider> GetByIdAsync(int id);
    Task<OidcProvider> CreateAsync(string name, string displayName, string authority, string clientId,
        string? clientSecret, string scopes, bool autoProvisionUsers,
        string? authorizationEndpoint, string? tokenEndpoint, string? userInfoEndpoint,
        string? usernameClaimType, string? emailClaimType, string? displayNameClaimType,
        string? rolesClaimType, string? adminGroupValue,
        string? iconUrl, string? buttonColor);
    Task<OidcProvider> UpdateAsync(int id, string displayName, string authority, string clientId,
        string? clientSecret, bool enabled, string scopes, bool autoProvisionUsers,
        string? authorizationEndpoint, string? tokenEndpoint, string? userInfoEndpoint,
        string? usernameClaimType, string? emailClaimType, string? displayNameClaimType,
        string? rolesClaimType, string? adminGroupValue,
        string? iconUrl, string? buttonColor);
    Task DeleteAsync(int id);
}
