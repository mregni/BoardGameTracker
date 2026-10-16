namespace BoardGameTracker.Common.Entities.Helpers;

public sealed record NightResult(int PlayerId, bool Won);

public sealed record NightWinners(int MostWins, List<int> WinnerIds);

public static class NightWinnerRule
{
    public static NightWinners Decide(IEnumerable<NightResult> results)
    {
        var wins = results
            .Where(x => x.Won)
            .GroupBy(x => x.PlayerId)
            .ToDictionary(x => x.Key, x => x.Count());
        if (wins.Count == 0)
        {
            return new NightWinners(0, []);
        }

        var mostWins = wins.Values.Max();
        var winnerIds = wins
            .Where(x => x.Value == mostWins)
            .Select(x => x.Key)
            .Order()
            .ToList();
        return new NightWinners(mostWins, winnerIds);
    }
}
