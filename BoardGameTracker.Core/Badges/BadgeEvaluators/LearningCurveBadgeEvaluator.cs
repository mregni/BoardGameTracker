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

        if (scores.Contains(null))
        {
            return Task.FromResult(false);
        }

        var result = sessions.Any(LowestWins)
            ? scores[0] < scores[1] && scores[1] < scores[2]
            : scores[0] > scores[1] && scores[1] > scores[2];
        return Task.FromResult(result);
    }

    private static bool LowestWins(Session session)
    {
        var winners = session.PlayerSessions.Where(x => x.Won && x.Score != null).ToList();
        var losers = session.PlayerSessions.Where(x => !x.Won && x.Score != null).ToList();
        return winners.Count > 0 && losers.Count > 0 && winners.Max(x => x.Score) < losers.Min(x => x.Score);
    }
}