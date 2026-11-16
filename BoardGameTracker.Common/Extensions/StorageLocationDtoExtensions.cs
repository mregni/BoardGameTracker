using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Extensions;

public static class StorageLocationDtoExtensions
{
    public static StorageLocationDto ToDto(this StorageLocation storageLocation)
    {
        return new StorageLocationDto
        {
            Id = storageLocation.Id,
            Name = storageLocation.Name,
            LocationId = storageLocation.LocationId,
            ParentId = storageLocation.ParentId,
            Layout = storageLocation.Layout,
            Rows = storageLocation.Rows,
            Columns = storageLocation.Columns,
            Row = storageLocation.Row,
            Column = storageLocation.Column
        };
    }

    public static List<StorageLocationDto> ToListDto(this IEnumerable<StorageLocation> storageLocations)
    {
        return storageLocations.Select(s => s.ToDto()).ToList();
    }
}
