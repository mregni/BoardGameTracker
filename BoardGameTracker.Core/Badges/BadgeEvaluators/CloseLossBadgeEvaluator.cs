using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;

namespace BoardGameTracker.Core.Badges.BadgeEvaluators;

public class CloseLossBadgeEvaluator : IBadgeEvaluator
{
    private readonly IGameRepository _gameRepository;

    private const int MaxDifference = 2;

    public CloseLossBadgeEvaluator(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public BadgeType BadgeType => BadgeType.CLoseLoss;
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

        var game = await _gameRepository.GetByIdAsync(session.GameId);
        if (game is not {HasScoring: true})
        {
            return false;
        }

        return IsCloseLoss(session, player);
    }

    private static bool IsCloseLoss(Session session, PlayerSession player)
    {
        var playerScore = player.Score!.Value;
        var otherScores = session.PlayerSessions
            .Where(x => x.PlayerId != player.PlayerId)
            .Select(ps => ps.Score!.Value)
            .ToList();
    
        var bestOtherScore = otherScores.Max(); 
        var worstOtherScore = otherScores.Min();
    
        var closeLossToHighestScorer = bestOtherScore > playerScore && 
                                       bestOtherScore - playerScore <= MaxDifference;
    
        var closeLossToLowestScorer = worstOtherScore < playerScore && 
                                      playerScore - worstOtherScore <= MaxDifference;
    
        return closeLossToHighestScorer || closeLossToLowestScorer;
    }
}