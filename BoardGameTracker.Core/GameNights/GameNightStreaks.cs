namespace BoardGameTracker.Core.GameNights;

public sealed record StreakStep(DateTime Date, IReadOnlyCollection<int> Keys);

public sealed record StreakRun(int Key, int Length, DateTime From, DateTime To, bool IsCurrent);

public sealed record StreakResult(List<StreakRun> Runs, List<int> LengthPerStep);

public static class GameNightStreaks
{
    public static StreakResult Find(IReadOnlyList<StreakStep> steps)
    {
        var runs = new List<StreakRun>();
        var lengths = new List<int>();
        var open = new Dictionary<int, (int Length, DateTime From)>();
        DateTime? previous = null;

        foreach (var step in steps)
        {
            var next = new Dictionary<int, (int Length, DateTime From)>();
            foreach (var key in step.Keys.Distinct())
            {
                next[key] = open.TryGetValue(key, out var run) ? (run.Length + 1, run.From) : (1, step.Date);
            }

            runs.AddRange(open
                .Where(x => !next.ContainsKey(x.Key))
                .Select(x => new StreakRun(x.Key, x.Value.Length, x.Value.From, previous!.Value, false)));
            lengths.Add(next.Count == 0 ? 0 : next.Values.Max(x => x.Length));
            open = next;
            previous = step.Date;
        }

        runs.AddRange(open.Select(x => new StreakRun(x.Key, x.Value.Length, x.Value.From, previous!.Value, true)));
        return new StreakResult(runs, lengths);
    }
}
