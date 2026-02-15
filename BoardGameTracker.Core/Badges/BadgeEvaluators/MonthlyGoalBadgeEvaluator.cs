using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Common;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class MonthlyGoalBadgeEvaluator : IBadgeEvaluator
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public MonthlyGoalBadgeEvaluator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public BadgeType BadgeType => BadgeType.MonthlyGoal;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var monthlySessions = playerSessions.Count(s => s.Start > _dateTimeProvider.UtcNow.AddMonths(-1));
        return Task.FromResult(monthlySessions >= 20);
    }
}