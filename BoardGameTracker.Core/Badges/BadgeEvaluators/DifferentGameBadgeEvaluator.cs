using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class DifferentGameBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.DifferentGames;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var gameCount = playerSessions
            .DistinctBy(x => x.GameId)
            .Count();
        
        switch (badge.Level)
        {
            case BadgeLevel.Green when gameCount >= 3:
            case BadgeLevel.Blue when gameCount >= 10:
            case BadgeLevel.Red when gameCount >= 20:
            case BadgeLevel.Gold when gameCount >= 50:
                return Task.FromResult(true);
            default:
                return Task.FromResult(false);
        }
    }
}