namespace BoardGameTracker.Common.Entities.Auth;

public class RefreshToken
{
    public int Id { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public ApplicationUser? User { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    public static RefreshToken Create(string userId, int expiryDays)
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
                    Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
        };
    }

    public void Revoke(string? reason = null, string? replacedByToken = null)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        ReplacedByToken = replacedByToken;
    }
}
