using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Games;

public class GameStatisticsService : IGameStatisticsService
{
    private readonly IReadRepository<Session> _sessionRepository;
    private readonly IGameStatisticsRepository _gameStatisticsRepository;
    private readonly ILogger<GameStatisticsService> _logger;

    public GameStatisticsService(
        IReadRepository<Session> sessionRepository,
        IGameStatisticsRepository gameStatisticsRepository,
        ILogger<GameStatisticsService> logger)
    {
        _sessionRepository = sessionRepository;
        _gameStatisticsRepository = gameStatisticsRepository;
        _logger = logger;
    }

    public async Task<GameStatistics> CalculateStatisticsAsync(int gameId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calculating statistics for game {GameId}", gameId);
        var stats = new GameStatistics
        {
            PlayCount = await _sessionRepository.CountAsync(new SessionsByGameSpec(gameId), cancellationToken),
            TotalPlayedTime = await _gameStatisticsRepository.GetTotalPlayedTime(gameId, cancellationToken),
            PricePerPlay = await _gameStatisticsRepository.GetPricePerPlay(gameId, cancellationToken),
            HighScore = await _gameStatisticsRepository.GetHighestScore(gameId, cancellationToken),
            AveragePlayTime = await _gameStatisticsRepository.GetAveragePlayTime(gameId, cancellationToken),
            AverageScore = await _gameStatisticsRepository.GetAverageScore(gameId, cancellationToken),
            LastPlayed = await _sessionRepository.FirstOrDefaultAsync(new LastPlayedDateSpec(gameId), cancellationToken),
            ExpansionCount = await _gameStatisticsRepository.GetExpansionCount(gameId, cancellationToken),
        };

        var (mostWinPlayer, wins) = await _gameStatisticsRepository.GetMostWins(gameId, cancellationToken);
        if (mostWinPlayer != null)
        {
            stats.MostWinsPlayer = new MostWinningPlayer
            {
                Id = mostWinPlayer.Id,
                Image = mostWinPlayer.Image ?? string.Empty,
                Name = mostWinPlayer.Name,
                TotalWins = wins
            };
        }

        return stats;
    }
}
