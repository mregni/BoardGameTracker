using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Locations.Specifications;

public sealed class LocationsWithSessionsSpec : Specification<Location>
{
    public LocationsWithSessionsSpec()
    {
        Query
            .Include(x => x.Sessions)
            .OrderBy(x => x.Name);
    }
}
