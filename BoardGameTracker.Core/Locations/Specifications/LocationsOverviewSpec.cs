using Ardalis.Specification;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Locations.Specifications;

public sealed class LocationsOverviewSpec : Specification<Location, LocationDto>
{
    public LocationsOverviewSpec()
    {
        Query
            .OrderBy(x => x.Name)
            .AsNoTracking();

        Query.Select(x => new LocationDto
        {
            Id = x.Id,
            Name = x.Name,
            PlayCount = x.Sessions.Count
        });
    }
}
