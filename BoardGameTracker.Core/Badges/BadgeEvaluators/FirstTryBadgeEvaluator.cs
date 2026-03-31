using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class FirstTryBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.FirstTry;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var sessionCount = playerSessions.Count(x => x.GameId == session.GameId);
        var playerSession = session.PlayerSessions.Single(x => x.PlayerId == playerId);
        return Task.FromResult(sessionCount == 1 && playerSession.Won);
    }
}