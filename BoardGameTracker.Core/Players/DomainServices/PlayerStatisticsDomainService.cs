using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Players.Interfaces;

namespace BoardGameTracker.Core.Players.DomainServices;

public class PlayerStatisticsDomainService : IPlayerStatisticsDomainService
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerStatisticsDomainService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<PlayerStatistics> CalculateStatisticsAsync(int playerId)
    {
        var stats = new PlayerStatistics
        {
            PlayCount = await _playerRepository.GetTotalPlayCount(playerId),
            WinCount = await _playerRepository.GetTotalWinCount(playerId),
            TotalPlayedTime = await _playerRepository.GetPlayLengthInMinutes(playerId),
            DistinctGameCount = await _playerRepository.GetDistinctGameCount(playerId)
        };

        var mostPlayedGames = await _playerRepository.GetMostPlayedGames(playerId, 5);
        stats.MostPlayedGames = [];

        foreach (var game in mostPlayedGames)
        {
            var wins = await _playerRepository.GetWinCount(playerId, game.Id);
            var totalPlays = await _playerRepository.GetPlayCount(playerId, game.Id);
            var winningPercentage = totalPlays > 0 ? (double)wins / totalPlays * 100 : 0;

            stats.MostPlayedGames.Add(new MostPlayedGame
            {
                Id = game.Id,
                Image = game.Image ?? string.Empty,
                Title = game.Title,
                TotalWins = wins,
                TotalSessions = totalPlays,
                WinningPercentage = winningPercentage
            });
        }

        return stats;
    }
}
