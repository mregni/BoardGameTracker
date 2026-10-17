namespace BoardGameTracker.Core.Auth;

public static class OidcIssuer
{
    public static string Normalize(string issuer)
    {
        return issuer.Trim().TrimEnd('/');
    }

    public static bool Matches(string issuer, string authority)
    {
        return string.Equals(Normalize(issuer), Normalize(authority), StringComparison.OrdinalIgnoreCase);
    }
}
