using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class GameNightStatisticsSpec : Specification<GameNight, GameNightStatisticsNight>
{
    public GameNightStatisticsSpec(DateTime? from, DateTime until)
    {
        var latestStart = until + GameNight.PlayOpensBefore;
        Query.Where(x => x.StartDate <= latestStart);
        if (from.HasValue)
        {
            Query.Where(x => x.StartDate >= from.Value);
        }

        Query
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.Id)
            .AsNoTracking();

        Query.Select(x => new GameNightStatisticsNight
        {
            Id = x.Id,
            Title = x.Title,
            StartDate = x.StartDate,
            EndedAt = x.EndedAt,
            HostId = x.HostId,
            LocationId = x.LocationId,
            RunningPlayerIds = x.RunningGames
                .SelectMany(r => r.Players.Select(p => p.PlayerId))
                .ToList(),
            AttendanceOverrides = x.Attendance
                .Select(a => new AttendanceOverride(a.PlayerId, a.Present))
                .ToList(),
            PlannedGameIds = x.SuggestedGames.Select(g => g.Id).ToList(),
            Sessions = x.Sessions
                .Select(s => new GameNightStatisticsSession
                {
                    GameId = s.GameId,
                    Start = s.Start,
                    End = s.End,
                    IsCoop = s.IsCoop,
                    Results = s.PlayerSessions.Select(p => new NightResult(p.PlayerId, p.Won)).ToList()
                })
                .ToList()
        });
    }
}
