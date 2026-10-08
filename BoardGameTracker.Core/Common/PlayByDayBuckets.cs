using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Core.Common;

public static class PlayByDayBuckets
{
    public static Dictionary<DayOfWeek, int> Count(IEnumerable<DateTime> utcStartTimes, IDateTimeProvider dateTimeProvider)
    {
        return utcStartTimes
            .Select(start => dateTimeProvider.ConvertToLocalTime(DateTime.SpecifyKind(start, DateTimeKind.Utc)).DayOfWeek)
            .GroupBy(day => day)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    public static List<PlayByDay> WeekStartingMonday(IReadOnlyDictionary<DayOfWeek, int> counts)
    {
        return Enum.GetValues<DayOfWeek>()
            .OrderBy(day => ((int)day + 6) % 7)
            .Select(day => new PlayByDay { DayOfWeek = day, PlayCount = counts.GetValueOrDefault(day) })
            .ToList();
    }
}
