using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class SoloSpecialistBadgeEvaluator : IBadgeEvaluator
{
    private readonly ISessionRepository _sessionRepository;

    public SoloSpecialistBadgeEvaluator(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public BadgeType BadgeType => BadgeType.SoloSpecialist;

    public async Task<bool> CanAwardBadge(int playerId, Badge badge, Session session)
    {
        var sessions = await _sessionRepository.GetByPlayer(playerId);
        var soloSessionCount = sessions.Count(x => x.PlayerSessions.Count == 1);
        switch (badge.Level)
        {
            case BadgeLevel.Green when soloSessionCount >= 5:
            case BadgeLevel.Blue when soloSessionCount >= 10:
            case BadgeLevel.Red when soloSessionCount >= 25:
            case BadgeLevel.Gold when soloSessionCount >= 50:
                return true;
            default:
                return false;
        }
    }
}