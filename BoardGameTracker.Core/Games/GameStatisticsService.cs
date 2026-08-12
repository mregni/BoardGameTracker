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

    public async Task<GameStatistics> CalculateStatisticsAsync(int gameId)
    {
        _logger.LogDebug("Calculating statistics for game {GameId}", gameId);
        var stats = new GameStatistics
        {
            PlayCount = await _sessionRepository.CountAsync(new SessionsByGameSpec(gameId)),
            TotalPlayedTime = await _gameStatisticsRepository.GetTotalPlayedTime(gameId),
            PricePerPlay = await _gameStatisticsRepository.GetPricePerPlay(gameId),
            HighScore = await _gameStatisticsRepository.GetHighestScore(gameId),
            AveragePlayTime = await _gameStatisticsRepository.GetAveragePlayTime(gameId),
            AverageScore = await _gameStatisticsRepository.GetAverageScore(gameId),
            LastPlayed = await _sessionRepository.FirstOrDefaultAsync(new LastPlayedDateSpec(gameId)),
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
