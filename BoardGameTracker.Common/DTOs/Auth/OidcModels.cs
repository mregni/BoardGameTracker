using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Auth;

public record OidcProviderInfo(
    string Name,
    string DisplayName,
    string? IconUrl,
    string? ButtonColor);

public record OidcSetupDto(
    string PublicBaseUrl,
    bool PublicUrlConfigured,
    string CallbackUriTemplate,
    string LinkCallbackUriTemplate);

public record TestOidcDiscoveryRequest([Required, Url] string Authority);

public record OidcDiscoveryResultDto(
    string Issuer,
    string AuthorizationEndpoint,
    string TokenEndpoint,
    string UserInfoEndpoint,
    bool IssuerMatchesAuthority);

public record CreateOidcProviderRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(200)] string DisplayName,
    [Required, Url] string Authority,
    [Required] string ClientId,
    string? ClientSecret,
    [Required] string Scopes,
    bool AutoProvisionUsers,
    [Url] string? AuthorizationEndpoint,
    [Url] string? TokenEndpoint,
    [Url] string? UserInfoEndpoint,
    string? UsernameClaimType,
    string? EmailClaimType,
    string? DisplayNameClaimType,
    string? RolesClaimType,
    string? AdminGroupValue,
    [Url] string? IconUrl,
    string? ButtonColor);

public record UpdateOidcProviderRequest(
    [Range(1, int.MaxValue)] int Id,
    [Required, StringLength(200)] string DisplayName,
    [Required, Url] string Authority,
    [Required] string ClientId,
    string? ClientSecret,
    bool Enabled,
    [Required] string Scopes,
    bool AutoProvisionUsers,
    [Url] string? AuthorizationEndpoint,
    [Url] string? TokenEndpoint,
    [Url] string? UserInfoEndpoint,
    string? UsernameClaimType,
    string? EmailClaimType,
    string? DisplayNameClaimType,
    string? RolesClaimType,
    string? AdminGroupValue,
    [Url] string? IconUrl,
    string? ButtonColor);

public record OidcProviderListDto(
    int Id,
    string Name,
    string DisplayName,
    bool Enabled);

public record OidcProviderDto(
    int Id,
    string Name,
    string DisplayName,
    bool Enabled,
    string Authority,
    string ClientId,
    bool HasClientSecret,
    string Scopes,
    bool AutoProvisionUsers,
    string? AuthorizationEndpoint,
    string? TokenEndpoint,
    string? UserInfoEndpoint,
    string? UsernameClaimType,
    string? EmailClaimType,
    string? DisplayNameClaimType,
    string? RolesClaimType,
    string? AdminGroupValue,
    string? IconUrl,
    string? ButtonColor);

public record ExternalLoginDto(
    int Id,
    string Provider,
    string ProviderKey,
    string? ProviderDisplayName,
    DateTime LinkedAt,
    DateTime? LastUsedAt);

public record UpdateUserRoleRequest(string Role);

public record AdminUpdateUserRequest(string Username, string? Email, string Role, int? PlayerId = null);
