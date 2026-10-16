namespace BoardGameTracker.Common.Entities.Auth;

public class OidcSession
{
    public int Id { get; private set; }
    public string KeyHash { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public ApplicationUser? User { get; private set; }
    public int ProviderId { get; private set; }
    public string IdToken { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private OidcSession() { }

    public OidcSession(string keyHash, string userId, int providerId, string idToken, DateTime expiresAt)
    {
        KeyHash = keyHash;
        UserId = userId;
        ProviderId = providerId;
        IdToken = idToken;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }
}
