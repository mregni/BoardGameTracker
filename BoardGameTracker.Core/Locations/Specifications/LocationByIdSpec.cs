using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Locations.Specifications;

public sealed class LocationByIdSpec : Specification<Location>
{
    public LocationByIdSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .AsNoTracking();
    }
}
