using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class SessionWinEvaluator : IBadgeEvaluator
{
    private readonly ISessionRepository _sessionRepository;

    public SessionWinEvaluator(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public BadgeType BadgeType => BadgeType.Wins;
    public async Task<bool> CanAwardBadge(int playerId, Badge badge, Session session)
    {
        var sessions = await _sessionRepository.GetByPlayer(playerId, true);
        var sessionCount = sessions.Count;
        switch (badge.Level)
        {
            case BadgeLevel.Green when sessionCount >= 3:
            case BadgeLevel.Blue when sessionCount >= 10:
            case BadgeLevel.Red when sessionCount >= 25:
            case BadgeLevel.Gold when sessionCount >= 50:
                return true;
            default:
                return false;
        }
    }
}