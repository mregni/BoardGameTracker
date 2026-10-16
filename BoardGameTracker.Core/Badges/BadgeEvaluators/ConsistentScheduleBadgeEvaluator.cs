using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Common;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class ConsistentScheduleBadgeEvaluator : IBadgeEvaluator
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConsistentScheduleBadgeEvaluator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public BadgeType BadgeType => BadgeType.ConsistentSchedule;

    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var sessionDate = LocalDate(session);
        if (sessionDate.DayOfWeek != DayOfWeek.Saturday)
        {
            return Task.FromResult(false);
        }

        var saturdays = playerSessions
            .Select(LocalDate)
            .Where(x => x.DayOfWeek == DayOfWeek.Saturday)
            .ToHashSet();

        for (var i = 0; i < BadgeEvaluatorConstants.ConsistentWeeksRequired; i++)
        {
            if (!saturdays.Contains(sessionDate.AddDays(-BadgeEvaluatorConstants.DaysInWeek * i)))
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    private DateTime LocalDate(Session session)
    {
        return _dateTimeProvider.ConvertToLocalTime(DateTime.SpecifyKind(session.Start, DateTimeKind.Utc)).Date;
    }
}
