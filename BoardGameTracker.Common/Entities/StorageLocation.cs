using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;

namespace BoardGameTracker.Common.Entities;

public class StorageLocation : HasId
{
    public const int MaxSize = 20;

    public string? Name { get; private set; }
    public StorageLayout Layout { get; private set; }
    public int? Rows { get; private set; }
    public int? Columns { get; private set; }
    public int? Row { get; private set; }
    public int? Column { get; private set; }
    public int LocationId { get; private set; }
    public Location Location { get; private set; } = null!;
    public int? ParentId { get; private set; }
    public StorageLocation? Parent { get; private set; }
    public ICollection<StorageLocation> Children { get; private set; } = [];

    private StorageLocation()
    {
        // For EF Core
    }

    public StorageLocation(string name, int locationId, int? parentId, StorageLayout layout, int? rows = null, int? columns = null)
    {
        Name = RequireName(name);
        LocationId = Guard.Against.NegativeOrZero(locationId);
        ParentId = parentId;
        Layout = Guard.Against.EnumOutOfRange(layout);
        if (layout == StorageLayout.Slot)
        {
            throw new DomainException(Constants.Errors.StorageInvalidLayout);
        }

        if (IsCabinet)
        {
            Resize(rows ?? 0, columns ?? 0);
        }
    }

    private StorageLocation(int locationId, int row, int column)
    {
        LocationId = locationId;
        Layout = StorageLayout.Slot;
        Row = row;
        Column = column;
    }

    public bool IsCabinet => Layout is StorageLayout.Shelves or StorageLayout.Grid;

    public static bool CanStore(GameState state) => state is GameState.Owned or GameState.ForTrade;

    public void Rename(string? name)
    {
        Name = Layout == StorageLayout.Slot && string.IsNullOrWhiteSpace(name) ? null : RequireName(name);
    }

    public IReadOnlyList<StorageLocation> Resize(int rows, int columns)
    {
        if (!IsCabinet)
        {
            throw new DomainException(Constants.Errors.StorageNotACabinet);
        }

        if (Layout == StorageLayout.Shelves)
        {
            columns = 1;
        }

        if (rows is < 1 or > MaxSize || columns is < 1 or > MaxSize)
        {
            throw new DomainException(Constants.Errors.StorageInvalidSize);
        }

        Rows = rows;
        Columns = columns;
        var removed = Children.Where(slot => slot.Row > rows || slot.Column > columns).ToList();
        for (var row = 1; row <= rows; row++)
        {
            for (var column = 1; column <= columns; column++)
            {
                if (!Children.Any(slot => slot.Row == row && slot.Column == column))
                {
                    Children.Add(new StorageLocation(LocationId, row, column));
                }
            }
        }

        return removed;
    }

    public void MoveTo(int locationId, StorageLocation? parent)
    {
        if (parent != null && (parent.Layout != StorageLayout.Free || parent.LocationId != locationId || parent.IsWithin(this)))
        {
            throw new DomainException(Constants.Errors.StorageInvalidParent);
        }

        ParentId = parent?.Id;
        Parent = parent;
        SetLocation(Guard.Against.NegativeOrZero(locationId));
    }

    public bool IsWithin(StorageLocation other)
    {
        for (var node = this; node != null; node = node.Parent)
        {
            if (node == other)
            {
                return true;
            }
        }

        return false;
    }

    private void SetLocation(int locationId)
    {
        LocationId = locationId;
        foreach (var child in Children)
        {
            child.SetLocation(locationId);
        }
    }

    private static string RequireName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(nameof(Name), Constants.Errors.StorageNameRequired);
        }

        return name.Trim();
    }
}
