using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;

namespace BoardGameTracker.Common.Extensions;

public static class AuthDtoExtensions
{
    public static UserDto ToDto(this ApplicationUser user, IEnumerable<string> roles) =>
        new(user.Id, user.UserName ?? "", user.Email, user.DisplayName,
            roles, user.CreatedAt, user.LastLoginAt, user.PlayerId);

    public static OidcProviderDto ToDto(this OidcProvider provider) =>
        new(provider.Id, provider.Name, provider.DisplayName, provider.Enabled,
            provider.Authority, provider.ClientId, provider.ClientSecret != null,
            provider.Scopes, provider.AutoProvisionUsers,
            provider.AuthorizationEndpoint, provider.TokenEndpoint, provider.UserInfoEndpoint,
            provider.UsernameClaimType, provider.EmailClaimType, provider.DisplayNameClaimType,
            provider.RolesClaimType, provider.AdminGroupValue,
            provider.IconUrl, provider.ButtonColor);

    public static ExternalLoginDto ToDto(this ExternalLogin login) =>
        new(login.Id, login.Provider, login.ProviderKey, login.ProviderDisplayName,
            login.LinkedAt, login.LastUsedAt);

    public static ProfileResponse ToProfileDto(this ApplicationUser user, IEnumerable<string>  roles) =>
        new(user.Id, user.UserName ?? "", user.Email, user.DisplayName,
            roles, user.CreatedAt, user.LastLoginAt, user.PlayerId);

    public static UserInfo ToUserInfo(this ApplicationUser user, IEnumerable<string> roles) =>
        new(user.Id, user.UserName ?? "", user.DisplayName, roles);

    public static OidcProviderListDto ToListDto(this OidcProvider provider) =>
        new(provider.Id, provider.Name, provider.DisplayName, provider.Enabled);
}
