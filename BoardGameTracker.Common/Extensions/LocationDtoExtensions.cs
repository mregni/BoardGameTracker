using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Extensions;

public static class LocationDtoExtensions
{
    public static LocationDto? ToDto(this Location? location)
    {
        if (location == null)
        {
            return null;
        }

        return new LocationDto
        {
            Id = location.Id,
            Name = location.Name
        };
    }

    public static List<LocationDto> ToListDto(this IEnumerable<Location> locations)
    {
        return locations
            .Select(l => l.ToDto())
            .OfType<LocationDto>()
            .ToList();
    }
}
