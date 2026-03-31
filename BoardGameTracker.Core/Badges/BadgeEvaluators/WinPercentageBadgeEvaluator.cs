using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class WinPercentageBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.WinPercentage;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var sessionsLost = playerSessions
            .Where(x => !x.PlayerSessions.Single(y => y.PlayerId == playerId).Won)
            .ToList();
        var sessionsWon = playerSessions
            .Where(x => x.PlayerSessions.Single(y => y.PlayerId == playerId).Won)
            .ToList();
        
        var totalSessions = sessionsLost.Count + sessionsWon.Count;
        if (totalSessions < 5)
        {
            return Task.FromResult(false);
        }

        var winPercentage = (double) sessionsWon.Count / totalSessions * 100;
        switch (badge.Level)
        {
            case BadgeLevel.Green when winPercentage >= 30:
            case BadgeLevel.Blue when winPercentage >= 50:
            case BadgeLevel.Red when winPercentage >= 65:
            case BadgeLevel.Gold when winPercentage >= 80:
                return Task.FromResult(true);
            default:
                return Task.FromResult(false);
        }
    }
}