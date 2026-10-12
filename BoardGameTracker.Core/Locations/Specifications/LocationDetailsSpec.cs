using Ardalis.Specification;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Locations.Specifications;

public sealed class LocationDetailsSpec : Specification<Location, LocationDto>
{
    public LocationDetailsSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .AsNoTracking();

        Query.Select(x => new LocationDto
        {
            Id = x.Id,
            Name = x.Name,
            PlayCount = x.Sessions.Count
        });
    }
}
