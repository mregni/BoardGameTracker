using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class WinPercentageBadgeEvaluator : IBadgeEvaluator
{
    private readonly ISessionRepository _sessionRepository;

    public WinPercentageBadgeEvaluator(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }
    
    public BadgeType BadgeType => BadgeType.WinPercentage;
    public async Task<bool> CanAwardBadge(int playerId, Badge badge, Session session)
    {
        var sessionsLost = await _sessionRepository.GetByPlayer(playerId, false);
        var sessionsWon = await _sessionRepository.GetByPlayer(playerId, true);
        
        var totalSessions = sessionsLost.Count + sessionsWon.Count;
        if (totalSessions < 5)
        {
            return false;
        }

        var winPercentage = (double) sessionsWon.Count / totalSessions * 100;
        switch (badge.Level)
        {
            case BadgeLevel.Green when winPercentage >= 30:
            case BadgeLevel.Blue when winPercentage >= 50:
            case BadgeLevel.Red when winPercentage >= 65:
            case BadgeLevel.Gold when winPercentage >= 80:
                return true;
            default:
                return false;
        }
    }
}