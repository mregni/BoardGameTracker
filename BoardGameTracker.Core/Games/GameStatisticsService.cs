using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;

namespace BoardGameTracker.Core.Games;

public class GameStatisticsService : IGameStatisticsService
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameStatisticsRepository _gameStatisticsRepository;
    private readonly IPlayerRepository _playerRepository;

    public GameStatisticsService(
        IGameSessionRepository gameSessionRepository,
        IGameStatisticsRepository gameStatisticsRepository,
        IPlayerRepository playerRepository)
    {
        _gameSessionRepository = gameSessionRepository;
        _gameStatisticsRepository = gameStatisticsRepository;
        _playerRepository = playerRepository;
    }

    public async Task<GameStatistics> CalculateStatisticsAsync(int gameId)
    {
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

        var mostWinPlayer = await _gameStatisticsRepository.GetMostWins(gameId);
        if (mostWinPlayer != null)
        {
            var wins = await _playerRepository.GetWinCount(mostWinPlayer.Id, gameId);
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
