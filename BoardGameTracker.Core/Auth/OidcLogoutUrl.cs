namespace BoardGameTracker.Core.Auth;

public static class OidcLogoutUrl
{
    public const string LoginPage = "/login";

    public static string PostLogoutRedirectUri(string publicBaseUrl)
    {
        return publicBaseUrl.TrimEnd('/') + LoginPage;
    }

    public static string Build(string endSessionEndpoint, string idToken, string clientId, string publicBaseUrl)
    {
        var query = new Dictionary<string, string>
        {
            ["id_token_hint"] = idToken,
            ["post_logout_redirect_uri"] = PostLogoutRedirectUri(publicBaseUrl),
            ["client_id"] = clientId,
        };

        var separator = endSessionEndpoint.Contains('?') ? "&" : "?";
        return endSessionEndpoint + separator + string.Join("&", query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
    }
}
