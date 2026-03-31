using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Players;

public class PlayerStatisticsService : IPlayerStatisticsService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly ILogger<PlayerStatisticsService> _logger;

    public PlayerStatisticsService(IPlayerRepository playerRepository, ILogger<PlayerStatisticsService> logger)
    {
        _playerRepository = playerRepository;
        _logger = logger;
    }

    public async Task<PlayerStatistics> CalculateStatisticsAsync(int playerId)
    {
        _logger.LogDebug("Calculating statistics for player {PlayerId}", playerId);
        var stats = new PlayerStatistics
        {
            PlayCount = await _playerRepository.GetTotalPlayCount(playerId),
            WinCount = await _playerRepository.GetTotalWinCount(playerId),
            TotalPlayedTime = await _playerRepository.GetPlayLengthInMinutes(playerId),
            DistinctGameCount = await _playerRepository.GetDistinctGameCount(playerId)
        };

        stats.MostPlayedGames = await _playerRepository.GetMostPlayedGames(playerId, 5);

        return stats;
    }
}
