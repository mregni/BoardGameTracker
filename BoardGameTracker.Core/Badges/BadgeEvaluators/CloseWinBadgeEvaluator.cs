using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class CloseWinBadgeEvaluator : IBadgeEvaluator
{
    private readonly IGameRepository _gameRepository;

    private const int MaxDifference = 2;
    private const double Tolerance = 0.01;

    public CloseWinBadgeEvaluator(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public BadgeType BadgeType => BadgeType.CloseWin;

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
        if (!player.Won)
        {
            return false;
        }

        var game = await _gameRepository.GetByIdAsync(session.GameId);
        if (game is not {HasScoring: true})
        {
            return false;
        }

        return IsCloseWin(session, player);
    }

    private static bool IsCloseWin(Session session, PlayerSession player)
    {
        var playerScore = player.Score!.Value;
        var allScores = session.PlayerSessions.Select(ps => ps.Score!.Value).ToList();
        var minScore = allScores.Min();
        var maxScore = allScores.Max();

        var playerWonWithLowestScore = Math.Abs(playerScore - minScore) < Tolerance;
        var playerWonWithHighestScore = Math.Abs(playerScore - maxScore) < Tolerance;

        if (playerWonWithLowestScore == playerWonWithHighestScore)
        {
            return false;
        }

        var otherScores = session.PlayerSessions
            .Where(x => x.PlayerId != player.PlayerId)
            .Where(x => !x.Won)
            .Select(ps => ps.Score!.Value)
            .ToList();

        if (otherScores.Count == 0)
        {
            return false;
        }

        var secondScore = otherScores.Max();
        if (playerWonWithLowestScore)
        {
            secondScore = otherScores.Min();
        }
        
        return Math.Abs(playerScore - secondScore) <= MaxDifference;
    }
}