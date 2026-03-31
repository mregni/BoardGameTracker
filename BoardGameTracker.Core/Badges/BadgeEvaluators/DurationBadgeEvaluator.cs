using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class DurationBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.Duration;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var duration = playerSessions
            .Where(x => x.PlayerSessions.Single(y => y.PlayerId == playerId).Won)
            .Select(x => x.End - x.Start)
            .Sum(x => x.TotalMinutes);
        
        switch (badge.Level)
        {
            case BadgeLevel.Green when duration >= 300:
            case BadgeLevel.Blue when duration >= 600:
            case BadgeLevel.Red when duration >= 3000:
            case BadgeLevel.Gold when duration >= 6000:
                return Task.FromResult(true);
            default:
                return Task.FromResult(false);
        }
    }
}