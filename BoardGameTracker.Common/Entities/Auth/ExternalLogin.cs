namespace BoardGameTracker.Common.Entities.Auth;

public class ExternalLogin
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public ApplicationUser? User { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string? Issuer { get; private set; }
    public string ProviderKey { get; private set; } = string.Empty;
    public string? ProviderDisplayName { get; private set; }
    public DateTime LinkedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private ExternalLogin() { }

    public ExternalLogin(string userId, string provider, string? issuer, string providerKey, string? providerDisplayName = null)
    {
        UserId = userId;
        Provider = provider;
        Issuer = issuer;
        ProviderKey = providerKey;
        ProviderDisplayName = providerDisplayName;
        LinkedAt = DateTime.UtcNow;
    }

    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public void UpdateProvider(string provider)
    {
        Provider = provider;
    }

    public void UpdateIssuer(string issuer)
    {
        Issuer = issuer;
    }
}
