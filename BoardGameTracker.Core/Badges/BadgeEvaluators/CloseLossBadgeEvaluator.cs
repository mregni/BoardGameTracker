using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Games.Specifications;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class CloseLossBadgeEvaluator : IBadgeEvaluator
{
    private readonly IGameRepository _gameRepository;

    private const int MaxDifference = 2;

    public CloseLossBadgeEvaluator(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public BadgeType BadgeType => BadgeType.CloseLoss;
    public async Task<bool> CanAwardBadge(int playerId, Badge badge, Session session, List<Session> playerSessions)
    {
        if (session.PlayerSessions.Count == 1)
        {
            return false;
        }

        if (session.PlayerSessions.Any(x => x.Score == null))
        {
            return false;
        }
        
        var player = session.PlayerSessions.Single(x => x.PlayerId == playerId);
        if (player.Won)
        {
            return false;
        }

        var hasScoring = await _gameRepository.FirstOrDefaultAsync(new GameHasScoringSpec(session.GameId));
        if (hasScoring != true)
        {
            return false;
        }

        return IsCloseLoss(session, player);
    }

    private static bool IsCloseLoss(Session session, PlayerSession player)
    {
        var playerScore = player.Score!.Value;
        var winnerScores = session.PlayerSessions
            .Where(x => x.Won && x.PlayerId != player.PlayerId)
            .Select(ps => ps.Score!.Value)
            .ToList();
        if (winnerScores.Count == 0)
        {
            return false;
        }

        var difference = winnerScores.Min(score => Math.Abs(score - playerScore));
        return difference > 0 && difference <= MaxDifference;
    }
}