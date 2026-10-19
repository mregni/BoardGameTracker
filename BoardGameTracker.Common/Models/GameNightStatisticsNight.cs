using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Models;

public class GameNightStatisticsNight
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndedAt { get; set; }
    public int HostId { get; set; }
    public int LocationId { get; set; }
    public List<int> RunningPlayerIds { get; set; } = [];
    public List<AttendanceOverride> AttendanceOverrides { get; set; } = [];
    public List<int> PlannedGameIds { get; set; } = [];
    public List<GameNightStatisticsSession> Sessions { get; set; } = [];

    public List<int> GetAttendeeIds()
    {
        return NightAttendanceRule.Derive(
            HostId,
            Sessions.SelectMany(x => x.Results.Select(r => r.PlayerId)).Concat(RunningPlayerIds),
            AttendanceOverrides);
    }
}

public class GameNightStatisticsSession
{
    public int GameId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsCoop { get; set; }
    public List<NightResult> Results { get; set; } = [];
}
