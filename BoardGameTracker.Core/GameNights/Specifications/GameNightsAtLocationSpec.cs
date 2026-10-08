using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class GameNightsAtLocationSpec : Specification<GameNight>
{
    public GameNightsAtLocationSpec(int locationId)
    {
        Query
            .Where(x => x.LocationId == locationId)
            .AsNoTracking();
    }
}
