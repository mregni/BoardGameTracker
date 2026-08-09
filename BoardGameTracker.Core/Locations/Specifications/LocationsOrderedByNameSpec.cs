using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Locations.Specifications;

public sealed class LocationsOrderedByNameSpec : Specification<Location>
{
    public LocationsOrderedByNameSpec()
    {
        Query
            .OrderBy(x => x.Name)
            .AsNoTracking();
    }
}
