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
        var sessionMonth = MonthOf(session.Start);
        var monthlySessions = playerSessions.Count(s => MonthOf(s.Start) == sessionMonth);
        return Task.FromResult(monthlySessions >= BadgeEvaluatorConstants.MonthlyGoalSessionsRequired);
    }

    private (int Year, int Month) MonthOf(DateTime utcStart)
    {
        var local = _dateTimeProvider.ConvertToLocalTime(utcStart);
        return (local.Year, local.Month);
    }
}