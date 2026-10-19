using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Leaderboard;

public sealed record PlayerStanding(string Name, int WinCount, int PlayCount)
{
    public double WinRate => PlayCount == 0 ? 0 : (double)WinCount / PlayCount;
}

public static class PlayerRanking
{
    public static List<T> Rank<T>(IEnumerable<T> players, LeaderboardRankBy rankBy, Func<T, PlayerStanding> standing, Action<T, int> setRank)
    {
        var ranked = Order(players.Select(x => (Player: x, Standing: standing(x))), rankBy).ToList();

        var ranks = new int[ranked.Count];
        for (var i = 0; i < ranked.Count; i++)
        {
            ranks[i] = i > 0 && SameStanding(ranked[i].Standing, ranked[i - 1].Standing) ? ranks[i - 1] : i + 1;
            setRank(ranked[i].Player, ranks[i]);
        }

        return ranked.Select(x => x.Player).ToList();
    }

    private static IEnumerable<(T Player, PlayerStanding Standing)> Order<T>(IEnumerable<(T Player, PlayerStanding Standing)> players, LeaderboardRankBy rankBy)
    {
        var ordered = rankBy switch
        {
            LeaderboardRankBy.WinRate => players
                .OrderByDescending(x => x.Standing.WinRate)
                .ThenByDescending(x => x.Standing.WinCount)
                .ThenByDescending(x => x.Standing.PlayCount),
            LeaderboardRankBy.Sessions => players
                .OrderByDescending(x => x.Standing.PlayCount)
                .ThenByDescending(x => x.Standing.WinCount),
            _ => players
                .OrderByDescending(x => x.Standing.WinCount)
                .ThenByDescending(x => x.Standing.WinRate)
                .ThenByDescending(x => x.Standing.PlayCount),
        };

        return ordered.ThenBy(x => x.Standing.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static bool SameStanding(PlayerStanding a, PlayerStanding b) =>
        a.WinCount == b.WinCount && a.PlayCount == b.PlayCount;
}
