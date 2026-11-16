using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateStorageLocationCommand
{
    [Required(AllowEmptyStrings = true)]
    [StringLength(200)]
    public required string Name { get; set; }

    [Range(1, int.MaxValue)]
    public int LocationId { get; set; }

    [Range(1, int.MaxValue)]
    public int? ParentId { get; set; }

    public StorageLayout Layout { get; set; }

    [Range(1, StorageLocation.MaxSize)]
    public int? Rows { get; set; }

    [Range(1, StorageLocation.MaxSize)]
    public int? Columns { get; set; }
}

public class UpdateStorageLocationCommand
{
    [StringLength(200)]
    public string? Name { get; set; }

    [Range(1, StorageLocation.MaxSize)]
    public int? Rows { get; set; }

    [Range(1, StorageLocation.MaxSize)]
    public int? Columns { get; set; }
}

public class MoveStorageLocationCommand
{
    [Range(1, int.MaxValue)]
    public int LocationId { get; set; }

    [Range(1, int.MaxValue)]
    public int? ParentId { get; set; }
}

public class StorageItemReference
{
    public StorageItemType Type { get; set; }

    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}

public class AssignStorageCommand
{
    [Required]
    [MinLength(1)]
    public List<StorageItemReference> Items { get; set; } = [];

    [Range(1, int.MaxValue)]
    public int? LocationId { get; set; }

    [Range(1, int.MaxValue)]
    public int? StorageLocationId { get; set; }
}

public class ArrangeStorageCommand
{
    [Range(1, int.MaxValue)]
    public int StorageLocationId { get; set; }

    [Required]
    [MinLength(1)]
    public List<StorageItemReference> Items { get; set; } = [];
}

public class UpdateBoxOrientationCommand
{
    [Required]
    public required StorageItemReference Item { get; set; }

    public BoxOrientation Orientation { get; set; }
}
