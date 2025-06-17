using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class SessionsBadgeEvaluator : IBadgeEvaluator
{
    private readonly ISessionRepository _sessionRepository;

    public SessionsBadgeEvaluator(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public BadgeType BadgeType => BadgeType.Sessions;

    public async Task<bool> CanAwardBadge(int playerId, Badge badge, Session session)
    {
        var sessionCount = await _sessionRepository.CountByPlayer(playerId);
        switch (badge.Level)
        {
            case BadgeLevel.Green when sessionCount >= 5:
            case BadgeLevel.Blue when sessionCount >= 10:
            case BadgeLevel.Red when sessionCount >= 50:
            case BadgeLevel.Gold when sessionCount >= 100:
                return true;
            default:
                return false;
        }
    }
}