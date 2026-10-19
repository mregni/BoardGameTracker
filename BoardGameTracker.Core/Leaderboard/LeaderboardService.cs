using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Leaderboard.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;

namespace BoardGameTracker.Core.Leaderboard;

public class LeaderboardService : ILeaderboardService
{
    public const int MinimumPlayersForPodium = 4;

    private readonly IPlayerRepository _playerRepository;

    public LeaderboardService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<LeaderboardDto> GetLeaderboardAsync(LeaderboardFilter filter, CancellationToken cancellationToken = default)
    {
        var minimum = Math.Max(1, filter.MinSessions);
        var rows = await _playerRepository.GetLeaderboardRows(filter, cancellationToken);
        var entries = rows.Select(ToEntry).ToList();

        var ranked = PlayerRanking.Rank(
            entries.Where(x => x.PlayCount >= minimum),
            filter.RankBy,
            x => new PlayerStanding(x.Name, x.WinCount, x.PlayCount),
            (x, rank) =>
            {
                x.IsRanked = true;
                x.Rank = rank;
            });

        var unranked = entries
            .Where(x => x.PlayCount < minimum)
            .OrderByDescending(x => x.PlayCount)
            .ThenByDescending(x => x.WinCount)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var entry in unranked)
        {
            entry.SessionsToRank = minimum - entry.PlayCount;
        }

        return new LeaderboardDto
        {
            Players = [.. ranked, .. unranked],
            MinimumSessions = minimum,
            MinimumPlayersForPodium = MinimumPlayersForPodium,
            RankBy = filter.RankBy,
            RankedCount = ranked.Count,
            WinCount = rows.Sum(x => x.WinCount),
            CoopWinCount = rows.Sum(x => x.CoopWinCount),
            ShowPodium = ranked.Count >= MinimumPlayersForPodium,
            MostWins = entries
                .Where(x => x.WinCount > 0)
                .OrderByDescending(x => x.WinCount)
                .ThenByDescending(WinRate)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(),
            MostPlays = entries
                .OrderByDescending(x => x.PlayCount)
                .ThenByDescending(x => x.WinCount)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(),
            BestWinRate = ranked
                .Where(x => x.WinCount > 0)
                .OrderByDescending(WinRate)
                .ThenByDescending(x => x.WinCount)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(),
            MostTimePlayed = entries
                .Where(x => x.MinutesPlayed > 0)
                .OrderByDescending(x => x.MinutesPlayed)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(),
        };
    }

    private static double WinRate(LeaderboardEntryDto entry) =>
        entry.PlayCount == 0 ? 0 : (double)entry.WinCount / entry.PlayCount;

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
