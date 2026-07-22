using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Games;

public class GameStatisticsService : IGameStatisticsService
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameStatisticsRepository _gameStatisticsRepository;
    private readonly ILogger<GameStatisticsService> _logger;

    public GameStatisticsService(
        IGameSessionRepository gameSessionRepository,
        IGameStatisticsRepository gameStatisticsRepository,
        ILogger<GameStatisticsService> logger)
    {
        _gameSessionRepository = gameSessionRepository;
        _gameStatisticsRepository = gameStatisticsRepository;
        _logger = logger;
    }

    public async Task<GameStatistics> CalculateStatisticsAsync(int gameId)
    {
        _logger.LogDebug("Calculating statistics for game {GameId}", gameId);
        var stats = new GameStatistics
        {
            PlayCount = await _gameSessionRepository.GetPlayCount(gameId),
            TotalPlayedTime = await _gameSessionRepository.GetTotalPlayedTime(gameId),
            PricePerPlay = await _gameStatisticsRepository.GetPricePerPlay(gameId),
            HighScore = await _gameStatisticsRepository.GetHighestScore(gameId),
            AveragePlayTime = await _gameStatisticsRepository.GetAveragePlayTime(gameId),
            AverageScore = await _gameStatisticsRepository.GetAverageScore(gameId),
            LastPlayed = await _gameSessionRepository.GetLastPlayedDateTime(gameId),
            ExpansionCount = await _gameStatisticsRepository.GetExpansionCount(gameId),
        };

        var (mostWinPlayer, wins) = await _gameStatisticsRepository.GetMostWins(gameId);
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
