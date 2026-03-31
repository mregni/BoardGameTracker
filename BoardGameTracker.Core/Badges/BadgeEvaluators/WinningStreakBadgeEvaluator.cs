using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class WinningStreakBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.WinningStreak;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var winStreakCount = playerSessions
            .OrderByDescending(x => x.Start)
            .Select(x => x.PlayerSessions.Single(y => y.PlayerId == playerId).Won)
            .TakeWhile(x => x)
            .Count();
        
        switch (badge.Level)
        {
            case BadgeLevel.Green when winStreakCount >= 5:
            case BadgeLevel.Blue when winStreakCount >= 10:
            case BadgeLevel.Red when winStreakCount >= 15:
            case BadgeLevel.Gold when winStreakCount >= 25:
                return Task.FromResult(true);
            default:
                return Task.FromResult(false);
        }
    }
}