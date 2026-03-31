using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class SoloSpecialistBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.SoloSpecialist;

    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var soloSessionCount = playerSessions.Count(x => x.PlayerSessions.Count == 1);
        switch (badge.Level)
        {
            case BadgeLevel.Green when soloSessionCount >= 5:
            case BadgeLevel.Blue when soloSessionCount >= 10:
            case BadgeLevel.Red when soloSessionCount >= 25:
            case BadgeLevel.Gold when soloSessionCount >= 50:
                return Task.FromResult(true);
            default:
                return Task.FromResult(false);
        }
    }
}