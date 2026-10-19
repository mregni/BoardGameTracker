namespace BoardGameTracker.Common.Entities.Helpers;

public sealed record AttendanceOverride(int PlayerId, bool Present);

public static class NightAttendanceRule
{
    public static List<int> Derive(int hostId, IEnumerable<int> playedPlayerIds, IEnumerable<AttendanceOverride> overrides)
    {
        var played = playedPlayerIds.ToHashSet();
        var present = new HashSet<int>(played) { hostId };

        foreach (var entry in overrides)
        {
            if (entry.Present)
            {
                present.Add(entry.PlayerId);
            }
            else if (!played.Contains(entry.PlayerId))
            {
                present.Remove(entry.PlayerId);
            }
        }

        return present
            .OrderBy(x => x == hostId ? 0 : 1)
            .ThenBy(x => x)
            .ToList();
    }

    public static List<AttendanceOverride> OverridesFor(int hostId, IEnumerable<int> playedPlayerIds, IEnumerable<int> presentPlayerIds)
    {
        var played = playedPlayerIds.ToHashSet();
        var present = presentPlayerIds.ToHashSet();

        var overrides = present
            .Where(x => !played.Contains(x) && x != hostId)
            .Order()
            .Select(x => new AttendanceOverride(x, true))
            .ToList();

        if (!present.Contains(hostId) && !played.Contains(hostId))
        {
            overrides.Insert(0, new AttendanceOverride(hostId, false));
        }

        return overrides;
    }
}
