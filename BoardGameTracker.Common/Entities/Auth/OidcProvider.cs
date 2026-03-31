namespace BoardGameTracker.Common.Entities.Auth;

public class OidcProvider
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool Enabled { get; private set; }
    public string Authority { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public string? ClientSecret { get; private set; }

    // Endpoint overrides
    public string? AuthorizationEndpoint { get; private set; }
    public string? TokenEndpoint { get; private set; }
    public string? UserInfoEndpoint { get; private set; }

    // Behavior settings
    public string Scopes { get; private set; } = "openid profile email";
    public bool AutoProvisionUsers { get; private set; } = true;

    // Claim mappings
    public string? UsernameClaimType { get; private set; }
    public string? EmailClaimType { get; private set; }
    public string? DisplayNameClaimType { get; private set; }
    public string? RolesClaimType { get; private set; }
    public string? AdminGroupValue { get; private set; }

    // UI customization
    public string? IconUrl { get; private set; }
    public string? ButtonColor { get; private set; }

    private OidcProvider() { }

    public OidcProvider(string name, string displayName, string authority, string clientId)
    {
        Name = name;
        DisplayName = displayName;
        Authority = authority;
        ClientId = clientId;
        Enabled = true;
    }

    public void Update(
        string displayName,
        string authority,
        string clientId,
        string? clientSecret,
        bool enabled,
        string scopes,
        bool autoProvisionUsers,
        string? authorizationEndpoint,
        string? tokenEndpoint,
        string? userInfoEndpoint,
        string? usernameClaimType,
        string? emailClaimType,
        string? displayNameClaimType,
        string? rolesClaimType,
        string? adminGroupValue,
        string? iconUrl,
        string? buttonColor)
    {
        DisplayName = displayName;
        Authority = authority;
        ClientId = clientId;
        if (clientSecret != null)
        {
            ClientSecret = clientSecret;
        }
        Enabled = enabled;
        Scopes = scopes;
        AutoProvisionUsers = autoProvisionUsers;
        AuthorizationEndpoint = authorizationEndpoint;
        TokenEndpoint = tokenEndpoint;
        UserInfoEndpoint = userInfoEndpoint;
        UsernameClaimType = usernameClaimType;
        EmailClaimType = emailClaimType;
        DisplayNameClaimType = displayNameClaimType;
        RolesClaimType = rolesClaimType;
        AdminGroupValue = adminGroupValue;
        IconUrl = iconUrl;
        ButtonColor = buttonColor;
    }
}
