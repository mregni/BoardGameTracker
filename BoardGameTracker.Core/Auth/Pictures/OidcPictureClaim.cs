using BoardGameTracker.Common.Entities.Auth;

namespace BoardGameTracker.Core.Auth.Pictures;

public static class OidcPictureClaim
{
    public static string? Read(OidcProvider provider, OidcClaims claims)
    {
        var value = claims.GetValue(OidcClaimTypes.Picture(provider))?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
