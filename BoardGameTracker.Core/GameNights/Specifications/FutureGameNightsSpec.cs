using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class FutureGameNightsSpec : Specification<GameNight>
{
    public FutureGameNightsSpec(DateTime now)
    {
        Query
            .Where(x => x.StartDate >= now)
            .AsNoTracking();
    }
}
