using BoardGameTracker.Common.Entities.Auth;

namespace BoardGameTracker.Core.Auth;

public static class OidcClaimTypes
{
    public const string DefaultUsername = "preferred_username";
    public const string DefaultEmail = "email";
    public const string DefaultDisplayName = "name";
    public const string DefaultRoles = "groups";
    public const string DefaultPicture = "picture";
    public const string DefaultAdminGroup = "bgt-admins";

    public static string Username(OidcProvider provider) => Configured(provider.UsernameClaimType) ?? DefaultUsername;

    public static string Email(OidcProvider provider) => Configured(provider.EmailClaimType) ?? DefaultEmail;

    public static string DisplayName(OidcProvider provider) => Configured(provider.DisplayNameClaimType) ?? DefaultDisplayName;

    public static string Roles(OidcProvider provider) => Configured(provider.RolesClaimType) ?? DefaultRoles;

    public static string Picture(OidcProvider provider) => Configured(provider.PictureClaimType) ?? DefaultPicture;

    public static string AdminGroup(OidcProvider provider) => Configured(provider.AdminGroupValue) ?? DefaultAdminGroup;

    private static string? Configured(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
