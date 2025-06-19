using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class MonthlyGoalBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.MonthlyGoal;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var monthlySessions = playerSessions.Count(s => s.Start > DateTime.UtcNow.AddMonths(-1));
        return Task.FromResult(monthlySessions >= 20);
    }
}