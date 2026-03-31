using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class SessionsBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.Sessions;

    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var sessionCount = playerSessions.Count;
        switch (badge.Level)
        {
            case BadgeLevel.Green when sessionCount >= 5:
            case BadgeLevel.Blue when sessionCount >= 10:
            case BadgeLevel.Red when sessionCount >= 50:
            case BadgeLevel.Gold when sessionCount >= 100:
                return Task.FromResult(true);
            default:
                return Task.FromResult(false);
        }
    }
}