using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Leaderboard.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;

namespace BoardGameTracker.Core.Leaderboard;

public class LeaderboardService : ILeaderboardService
{
    public const int MinimumPlaysForWinRate = 5;

    private readonly IPlayerRepository _playerRepository;

    public LeaderboardService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<LeaderboardDto> GetLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _playerRepository.GetLeaderboardRows(cancellationToken);
        var entries = rows
            .Select(ToEntry)
            .OrderByDescending(x => x.WinCount)
            .ThenByDescending(x => x.WinPercentage)
            .ThenByDescending(x => x.PlayCount)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < entries.Count; i++)
        {
            entries[i].Rank = i + 1;
        }

        return new LeaderboardDto
        {
            Players = entries,
            MinimumPlaysForWinRate = MinimumPlaysForWinRate,
            MostPlays = entries.OrderByDescending(x => x.PlayCount).ThenBy(x => x.Rank).FirstOrDefault(x => x.PlayCount > 0),
            MostWins = entries.FirstOrDefault(x => x.WinCount > 0),
            BestWinRate = entries.Where(x => x.PlayCount >= MinimumPlaysForWinRate).OrderByDescending(x => x.WinPercentage).ThenBy(x => x.Rank).FirstOrDefault(),
            MostTimePlayed = entries.OrderByDescending(x => x.MinutesPlayed).ThenBy(x => x.Rank).FirstOrDefault(x => x.MinutesPlayed > 0),
        };
    }

    private static LeaderboardEntryDto ToEntry(LeaderboardRow row) => new()
    {
        PlayerId = row.PlayerId,
        Name = row.Name,
        Image = row.Image,
        PlayCount = row.PlayCount,
        WinCount = row.WinCount,
        PodiumCount = row.PodiumCount,
        WinPercentage = row.PlayCount == 0 ? 0 : Math.Round(100.0 * row.WinCount / row.PlayCount, 1),
        MinutesPlayed = Math.Round(row.MinutesPlayed),
    };
}
