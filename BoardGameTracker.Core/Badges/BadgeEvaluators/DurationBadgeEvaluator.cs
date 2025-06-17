using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class DurationBadgeEvaluator : IBadgeEvaluator
{
    private readonly ISessionRepository _sessionRepository;

    public DurationBadgeEvaluator(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public BadgeType BadgeType => BadgeType.Duration;
    public async Task<bool> CanAwardBadge(int playerId, Badge badge, Session session)
    {
        var sessions = await _sessionRepository.GetByPlayer(playerId, true);
        var duration = sessions
            .Select(x => x.End - x.Start)
            .Sum(x => x.TotalMinutes);
        
        switch (badge.Level)
        {
            case BadgeLevel.Green when duration >= 300:
            case BadgeLevel.Blue when duration >= 600:
            case BadgeLevel.Red when duration >= 3000:
            case BadgeLevel.Gold when duration >= 6000:
                return true;
            default:
                return false;
        }
    }
}