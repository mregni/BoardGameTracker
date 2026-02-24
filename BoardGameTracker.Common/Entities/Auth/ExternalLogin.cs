namespace BoardGameTracker.Common.Entities.Auth;

public class ExternalLogin
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public ApplicationUser? User { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public string? ProviderDisplayName { get; private set; }
    public DateTime LinkedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private ExternalLogin() { }

    public ExternalLogin(string userId, string provider, string providerKey, string? providerDisplayName = null)
    {
        UserId = userId;
        Provider = provider;
        ProviderKey = providerKey;
        ProviderDisplayName = providerDisplayName;
        LinkedAt = DateTime.UtcNow;
    }

    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }
}
