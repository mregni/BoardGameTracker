using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class SessionWinEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.Wins;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var sessionCount = playerSessions.Count(x => x.PlayerSessions.Single(y => y.PlayerId == playerId).Won);
        switch (badge.Level)
        {
            case BadgeLevel.Green when sessionCount >= 3:
            case BadgeLevel.Blue when sessionCount >= 10:
            case BadgeLevel.Red when sessionCount >= 25:
            case BadgeLevel.Gold when sessionCount >= 50:
                return Task.FromResult(true);
            default:
                return Task.FromResult(false);
        }
    }
}