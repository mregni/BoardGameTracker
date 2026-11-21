using System;
using System.Linq;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Storage;

public class StorageLocationTests
{
    [Fact]
    public void Constructor_ShouldCreateNoSlots_ForAFreeStorageLocation()
    {
        var room = new StorageLocation(" Living room ", 1, null, StorageLayout.Free, 3, 3);

        room.Name.Should().Be("Living room");
        room.LocationId.Should().Be(1);
        room.IsCabinet.Should().BeFalse();
        room.Rows.Should().BeNull();
        room.Children.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldCreateOneSlotPerShelf_IgnoringColumns()
    {
        var bookcase = new StorageLocation("Billy", 1, 4, StorageLayout.Shelves, 3, 5);

        bookcase.Rows.Should().Be(3);
        bookcase.Columns.Should().Be(1);
        bookcase.ParentId.Should().Be(4);
        bookcase.Children.Select(x => (x.Row, x.Column)).Should().Equal((1, 1), (2, 1), (3, 1));
        bookcase.Children.Should().AllSatisfy(slot =>
        {
            slot.Layout.Should().Be(StorageLayout.Slot);
            slot.Name.Should().BeNull();
            slot.LocationId.Should().Be(1);
        });
    }

    [Fact]
    public void Constructor_ShouldCreateACellPerRowAndColumn_ForAGrid()
    {
        var kallax = new StorageLocation("Kallax", 1, null, StorageLayout.Grid, 2, 3);

        kallax.Children.Should().HaveCount(6);
        kallax.Children.Should().Contain(x => x.Row == 2 && x.Column == 3);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 0)]
    [InlineData(21, 1)]
    public void Constructor_ShouldRejectAGridOutOfRange(int rows, int columns)
    {
        var act = () => new StorageLocation("Kallax", 1, null, StorageLayout.Grid, rows, columns);

        act.Should().Throw<DomainException>().WithMessage(Constants.Errors.StorageInvalidSize);
    }

    [Fact]
    public void Constructor_ShouldRejectASlot()
    {
        var act = () => new StorageLocation("Shelf", 1, null, StorageLayout.Slot);

        act.Should().Throw<DomainException>().WithMessage(Constants.Errors.StorageInvalidLayout);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_ShouldRequireAName(string name)
    {
        var act = () => new StorageLocation(name, 1, null, StorageLayout.Free);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Resize_ShouldAddMissingSlots_AndReturnTheOnesOutside()
    {
        var kallax = new StorageLocation("Kallax", 1, null, StorageLayout.Grid, 2, 2);
        var corner = kallax.Children.Single(x => x.Row == 2 && x.Column == 2);

        var removed = kallax.Resize(3, 1);

        removed.Should().HaveCount(2).And.Contain(corner);
        kallax.Rows.Should().Be(3);
        kallax.Columns.Should().Be(1);
        kallax.Children.Should().Contain(x => x.Row == 3 && x.Column == 1);
        kallax.Children.Count(x => x.Row == 1 && x.Column == 1).Should().Be(1);
    }

    [Fact]
    public void Resize_ShouldOnlyWorkForACabinet()
    {
        var room = new StorageLocation("Attic", 1, null, StorageLayout.Free);

        var act = () => room.Resize(2, 2);

        act.Should().Throw<DomainException>().WithMessage(Constants.Errors.StorageNotACabinet);
    }

    [Fact]
    public void Rename_ShouldResetASlotToItsDefaultName_WhenLeftEmpty()
    {
        var bookcase = new StorageLocation("Billy", 1, null, StorageLayout.Shelves, 1);
        var shelf = bookcase.Children.Single();

        shelf.Rename("Top");
        shelf.Name.Should().Be("Top");

        shelf.Rename(" ");
        shelf.Name.Should().BeNull();
    }

    [Fact]
    public void Rename_ShouldRequireANameForAnythingButASlot()
    {
        var room = new StorageLocation("Attic", 1, null, StorageLayout.Free);

        var act = () => room.Rename("");

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void MoveTo_ShouldTakeTheWholeTreeToTheNewLocation()
    {
        var room = new StorageLocation("Living room", 1, null, StorageLayout.Free) { Id = 1 };
        var bookcase = new StorageLocation("Billy", 1, 1, StorageLayout.Shelves, 2) { Id = 2 };
        room.Children.Add(bookcase);
        var trailer = new StorageLocation("Bench", 2, null, StorageLayout.Free) { Id = 3 };

        bookcase.MoveTo(2, trailer);

        bookcase.ParentId.Should().Be(3);
        bookcase.LocationId.Should().Be(2);
        bookcase.Children.Should().OnlyContain(x => x.LocationId == 2);
    }

    [Fact]
    public void MoveTo_ShouldRefuseAParentInsideItself()
    {
        var room = new StorageLocation("Living room", 1, null, StorageLayout.Free) { Id = 1 };
        var corner = new StorageLocation("Corner", 1, 1, StorageLayout.Free) { Id = 2 };
        SetParent(corner, room);

        var act = () => room.MoveTo(1, corner);

        act.Should().Throw<DomainException>().WithMessage(Constants.Errors.StorageInvalidParent);
    }

    [Fact]
    public void MoveTo_ShouldRefuseACabinetOrAParentAtAnotherLocation()
    {
        var bookcase = new StorageLocation("Billy", 1, null, StorageLayout.Shelves, 2) { Id = 1 };
        var room = new StorageLocation("Office", 2, null, StorageLayout.Free) { Id = 2 };
        var box = new StorageLocation("Box", 1, null, StorageLayout.Free) { Id = 3 };

        ((Action)(() => box.MoveTo(1, bookcase))).Should().Throw<DomainException>();
        ((Action)(() => box.MoveTo(1, room))).Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(GameState.Owned, true)]
    [InlineData(GameState.ForTrade, true)]
    [InlineData(GameState.Wanted, false)]
    [InlineData(GameState.Sold, false)]
    [InlineData(GameState.PreviouslyOwned, false)]
    public void CanStore_ShouldOnlyAllowGamesYouHave(GameState state, bool expected)
    {
        StorageLocation.CanStore(state).Should().Be(expected);
    }

    private static void SetParent(StorageLocation child, StorageLocation parent)
    {
        parent.Children.Add(child);
        typeof(StorageLocation).GetProperty(nameof(StorageLocation.Parent))!.SetValue(child, parent);
    }
}
