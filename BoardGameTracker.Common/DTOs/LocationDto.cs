using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.DTOs;

public class LocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public static class LocationDtoExtensions
{
    public static LocationDto ToDto(this Location location)
    {
        return new LocationDto
        {
            Id = location.Id,
            Name = location.Name
        };
    }

    public static List<LocationDto> ToListDto(this IEnumerable<Location> locations)
    {
        return locations.Select(l => l.ToDto()).ToList();
    }

    public static Location ToEntity(this LocationDto dto)
    {
        return new Location(dto.Name);
    }
}
