using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class LearningCurveBadgeEvaluator : IBadgeEvaluator
{
    public BadgeType BadgeType => BadgeType.LearningCurve;
    public Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        var sessions = playerSessions
            .Where(x => x.GameId == session.GameId)
            .ToList();

        if (sessions.Count < 3)
        {
            return Task.FromResult(false);
        }

        var scores = sessions
            .OrderByDescending(x => x.Start)
            .Take(3)
            .Select(x => x.PlayerSessions.Single(x => x.PlayerId == playerId).Score)
            .ToList();

        if (scores.Any(score => score == null))
        {
            return Task.FromResult(false);
        }

        var result = scores[0] > scores[1] && scores[1] > scores[2];
        return Task.FromResult(result);
    }
}