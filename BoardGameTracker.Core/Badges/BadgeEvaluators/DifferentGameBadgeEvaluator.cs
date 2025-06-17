using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class DifferentGameBadgeEvaluator : IBadgeEvaluator
{
    private readonly ISessionRepository _sessionRepository;

    public DifferentGameBadgeEvaluator(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public BadgeType BadgeType => BadgeType.DifferentGames;
    public async Task<bool> CanAwardBadge(int playerId, Badge badge, Session session)
    {
        var sessions = await _sessionRepository.GetByPlayer(playerId);
        var gameCount = sessions
            .DistinctBy(x => x.GameId)
            .Count();
        
        switch (badge.Level)
        {
            case BadgeLevel.Green when gameCount >= 3:
            case BadgeLevel.Blue when gameCount >= 10:
            case BadgeLevel.Red when gameCount >= 20:
            case BadgeLevel.Gold when gameCount >= 50:
                return true;
            default:
                return false;
        }
    }
}