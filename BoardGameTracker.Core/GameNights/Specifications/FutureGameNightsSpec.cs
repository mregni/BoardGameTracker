using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class FutureGameNightsSpec : Specification<GameNight>
{
    public FutureGameNightsSpec(DateTime now)
    {
        var playingSince = now - GameNight.PlayWindow;
        Query
            .Where(x => x.EndedAt == null && x.StartDate > playingSince)
            .AsNoTracking();
    }
}
