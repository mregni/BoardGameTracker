using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class MarathonRunnerBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.MarathonRunner;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session)
    {
        var result = (session.End - session.Start).TotalMinutes >= 240;
        return Task.FromResult(result);
    }
}