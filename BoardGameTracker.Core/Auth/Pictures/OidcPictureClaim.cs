using BoardGameTracker.Common.Entities.Auth;

namespace BoardGameTracker.Core.Auth.Pictures;

public static class OidcPictureClaim
{
    public const string DefaultClaimType = "picture";

    public static string? Read(OidcProvider provider, OidcClaims claims)
    {
        var claimType = string.IsNullOrWhiteSpace(provider.PictureClaimType) ? DefaultClaimType : provider.PictureClaimType;
        var value = claims.GetValue(claimType)?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
