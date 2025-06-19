using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class SocialPlayerBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.SocialPlayer;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var playerCount = playerSessions
            .SelectMany(x => x.PlayerSessions)
            .Select(x => x.PlayerId)
            .Distinct()
            .Count(x => x != playerId);
        
        switch (badge.Level)
        {
            case BadgeLevel.Green when playerCount >= 5:
            case BadgeLevel.Blue when playerCount >= 10:
            case BadgeLevel.Red when playerCount >= 25:
            case BadgeLevel.Gold when playerCount >= 50:
                return Task.FromResult(true);
            default:
                return Task.FromResult(false);
        }
    }
}