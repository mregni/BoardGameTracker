using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class UpcomingGameNightsSpec : Specification<GameNight, UpcomingGameNight>
{
    public UpcomingGameNightsSpec(DateTime now)
    {
        var playingSince = now - GameNight.PlayWindow;
        Query
            .Where(x => x.EndedAt == null && x.StartDate > playingSince)
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.Id)
            .AsNoTracking();

        Query.Select(x => new UpcomingGameNight
        {
            Id = x.Id,
            Title = x.Title,
            StartDate = x.StartDate,
            SuggestedGameIds = x.SuggestedGames.Select(g => g.Id).ToList()
        });
    }
}
