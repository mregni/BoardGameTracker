namespace BoardGameTracker.Common.Entities;

public class PlayerBadge
{
    public int PlayerId { get; private set; }
    public int BadgeId { get; private set; }
    public DateTime? AwardedAt { get; private set; }
    public Player Player { get; private set; } = null!;
    public Badge Badge { get; private set; } = null!;

    public PlayerBadge(int playerId, int badgeId, DateTime? awardedAt)
    {
        PlayerId = playerId;
        BadgeId = badgeId;
        AwardedAt = awardedAt;
    }
}
