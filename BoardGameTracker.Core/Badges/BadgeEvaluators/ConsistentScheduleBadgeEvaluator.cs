using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class ConsistentScheduleBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.ConsistentSchedule;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        if (session.Start.DayOfWeek != DayOfWeek.Saturday)
        {
            return Task.FromResult(false);
        }

        var saturdayGames = playerSessions.Where(x => x.Start.DayOfWeek == DayOfWeek.Saturday).ToList();
        
        var requiredSaturdays = new List<DateTime>();
        for (var i = 0; i < 10; i++)
        {
            requiredSaturdays.Add(session.Start.AddDays(-BadgeEvaluatorConstants.DaysInWeek * i));
        }
        
        foreach (var requiredSaturday in requiredSaturdays)
        {
            var hasSessionOnSaturday = saturdayGames.Any(s => s.Start.Date == requiredSaturday.Date);
            if (!hasSessionOnSaturday)
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }
}