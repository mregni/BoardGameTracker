namespace BoardGameTracker.Common.DTOs.Auth;

public record OidcProviderInfo(
    string Name,
    string DisplayName,
    string? IconUrl,
    string? ButtonColor);

public record OidcCallbackRequest(string Code, string State);

public record CreateOidcProviderRequest(
    string Name,
    string DisplayName,
    string Authority,
    string ClientId,
    string? ClientSecret,
    string Scopes,
    bool AutoProvisionUsers,
    string? AuthorizationEndpoint,
    string? TokenEndpoint,
    string? UserInfoEndpoint,
    string? UsernameClaimType,
    string? EmailClaimType,
    string? DisplayNameClaimType,
    string? IconUrl,
    string? ButtonColor);

public record UpdateOidcProviderRequest(
    int Id,
    string DisplayName,
    string Authority,
    string ClientId,
    string? ClientSecret,
    bool Enabled,
    string Scopes,
    bool AutoProvisionUsers,
    string? AuthorizationEndpoint,
    string? TokenEndpoint,
    string? UserInfoEndpoint,
    string? UsernameClaimType,
    string? EmailClaimType,
    string? DisplayNameClaimType,
    string? IconUrl,
    string? ButtonColor);

public record OidcProviderDto(
    int Id,
    string Name,
    string DisplayName,
    bool Enabled,
    string Authority,
    string ClientId,
    string Scopes,
    bool AutoProvisionUsers,
    string? IconUrl,
    string? ButtonColor);
