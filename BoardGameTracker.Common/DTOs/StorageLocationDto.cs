using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class StorageLocationDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int LocationId { get; set; }
    public int? ParentId { get; set; }
    public StorageLayout Layout { get; set; }
    public int? Rows { get; set; }
    public int? Columns { get; set; }
    public int? Row { get; set; }
    public int? Column { get; set; }
}
