using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Locations.Specifications;
using BoardGameTracker.Core.Storage;
using BoardGameTracker.Core.Storage.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Storage;

public class StorageServiceTests
{
    private readonly Mock<IStorageRepository> _storageRepositoryMock = new();
    private readonly Mock<ILocationRepository> _locationRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly StorageService _service;

    public StorageServiceTests()
    {
        _service = new StorageService(
            _storageRepositoryMock.Object,
            _locationRepositoryMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<StorageService>.Instance);
    }

    private void VerifyNoOtherCalls()
    {
        _storageRepositoryMock.VerifyNoOtherCalls();
        _locationRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    private static LocationByIdSpec LocationSpec(int id) =>
        It.Is<LocationByIdSpec>(s => s.IsSatisfiedBy(new Location("Home") { Id = id }) && !s.IsSatisfiedBy(new Location("Home") { Id = id + 1 }));

    private void SetupLocation(int id, bool exists = true)
    {
        _locationRepositoryMock.Setup(x => x.AnyAsync(LocationSpec(id), CancellationToken.None)).ReturnsAsync(exists);
    }

    private void SetupTree(params StorageLocation[] nodes)
    {
        _storageRepositoryMock.Setup(x => x.GetAllTrackedAsync()).ReturnsAsync(nodes.ToList());
    }

    private void SetupStored(List<int> ids, List<Game> games, List<Expansion>? expansions = null)
    {
        _storageRepositoryMock.Setup(x => x.GetGamesStoredInAsync(It.Is<IEnumerable<int>>(i => i.OrderBy(v => v).SequenceEqual(ids.OrderBy(v => v)))))
            .ReturnsAsync(games);
        _storageRepositoryMock.Setup(x => x.GetExpansionsStoredInAsync(It.Is<IEnumerable<int>>(i => i.OrderBy(v => v).SequenceEqual(ids.OrderBy(v => v)))))
            .ReturnsAsync(expansions ?? []);
    }

    private void SetupItems(List<Game> games, List<Expansion> expansions)
    {
        _storageRepositoryMock.Setup(x => x.GetGamesAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(games);
        _storageRepositoryMock.Setup(x => x.GetExpansionsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(expansions);
    }

    private static Game StoredGame(int id, int locationId, int? storageLocationId, int position = 0)
    {
        var game = new Game($"Game {id}") { Id = id };
        game.Store(locationId, storageLocationId, position);
        return game;
    }

    private static StorageLocation Room(int id, int locationId, StorageLocation? parent = null)
    {
        var room = new StorageLocation($"Room {id}", locationId, parent?.Id, StorageLayout.Free) { Id = id };
        if (parent != null)
        {
            parent.Children.Add(room);
            typeof(StorageLocation).GetProperty(nameof(StorageLocation.Parent))!.SetValue(room, parent);
        }

        return room;
    }

    [Fact]
    public async Task Create_ShouldThrow_WhenTheLocationDoesNotExist()
    {
        SetupLocation(3, exists: false);

        var act = () => _service.Create(new CreateStorageLocationCommand { Name = "Attic", LocationId = 3, Layout = StorageLayout.Free });

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _locationRepositoryMock.Verify(x => x.AnyAsync(LocationSpec(3), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldRefuseACabinetAsParent()
    {
        SetupLocation(1);
        var bookcase = new StorageLocation("Billy", 1, null, StorageLayout.Shelves, 2) { Id = 4 };
        _storageRepositoryMock.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(bookcase);

        var act = () => _service.Create(new CreateStorageLocationCommand { Name = "Box", LocationId = 1, ParentId = 4, Layout = StorageLayout.Free });

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.StorageInvalidParent);
        _locationRepositoryMock.Verify(x => x.AnyAsync(LocationSpec(1), CancellationToken.None), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetByIdAsync(4), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldSaveAGridWithItsCells()
    {
        SetupLocation(1);
        var room = Room(2, 1);
        _storageRepositoryMock.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(room);
        _storageRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<StorageLocation>())).ReturnsAsync((StorageLocation s) => s);

        var result = await _service.Create(new CreateStorageLocationCommand
        {
            Name = "Kallax", LocationId = 1, ParentId = 2, Layout = StorageLayout.Grid, Rows = 4, Columns = 4
        });

        result.ParentId.Should().Be(2);
        result.Children.Should().HaveCount(16);
        _locationRepositoryMock.Verify(x => x.AnyAsync(LocationSpec(1), CancellationToken.None), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetByIdAsync(2), Times.Once);
        _storageRepositoryMock.Verify(x => x.CreateAsync(result), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldPutGamesFromRemovedShelvesInTheCabinet()
    {
        var bookcase = new StorageLocation("Billy", 1, null, StorageLayout.Shelves, 3) { Id = 10 };
        var shelves = bookcase.Children.OrderBy(x => x.Row).ToList();
        for (var i = 0; i < shelves.Count; i++)
        {
            shelves[i].Id = 11 + i;
        }

        SetupTree([bookcase, .. shelves]);
        var onBottomShelf = StoredGame(1, 1, 13, 2);
        SetupStored([13], [onBottomShelf]);

        var result = await _service.Update(10, new UpdateStorageLocationCommand { Name = "Billy bookcase", Rows = 2 });

        result.Name.Should().Be("Billy bookcase");
        result.Rows.Should().Be(2);
        onBottomShelf.StorageLocationId.Should().Be(10);
        onBottomShelf.StoragePosition.Should().Be(2);
        _storageRepositoryMock.Verify(x => x.GetAllTrackedAsync(), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetGamesStoredInAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetExpansionsStoredInAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
        _storageRepositoryMock.Verify(x => x.DeleteAsync(13), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Move_ShouldTakeTheGamesInsideToTheNewLocation()
    {
        SetupLocation(2);
        var room = Room(1, 1);
        var box = Room(2, 1, room);
        var bench = Room(3, 2);
        SetupTree(room, box, bench);
        var inBox = StoredGame(1, 1, 2, 5);
        var expansion = new Expansion("Oceania", 1, 9) { Id = 4 };
        expansion.Store(1, 1, 0);
        SetupStored([1, 2], [inBox], [expansion]);

        var result = await _service.Move(1, new MoveStorageLocationCommand { LocationId = 2, ParentId = 3 });

        result.ParentId.Should().Be(3);
        box.LocationId.Should().Be(2);
        inBox.StoredAtLocationId.Should().Be(2);
        inBox.StorageLocationId.Should().Be(2);
        inBox.StoragePosition.Should().Be(5);
        expansion.StoredAtLocationId.Should().Be(2);
        expansion.StorageLocationId.Should().Be(1);
        _locationRepositoryMock.Verify(x => x.AnyAsync(LocationSpec(2), CancellationToken.None), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetAllTrackedAsync(), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetGamesStoredInAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetExpansionsStoredInAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Move_ShouldLeaveTheGamesAlone_WithinTheSameLocation()
    {
        SetupLocation(1);
        var room = Room(1, 1);
        var other = Room(2, 1);
        SetupTree(room, other);

        await _service.Move(1, new MoveStorageLocationCommand { LocationId = 1, ParentId = 2 });

        room.ParentId.Should().Be(2);
        _locationRepositoryMock.Verify(x => x.AnyAsync(LocationSpec(1), CancellationToken.None), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetAllTrackedAsync(), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Move_ShouldRefuseAShelf()
    {
        SetupLocation(1);
        var bookcase = new StorageLocation("Billy", 1, null, StorageLayout.Shelves, 1) { Id = 1 };
        var shelf = bookcase.Children.Single();
        shelf.Id = 2;
        var room = Room(3, 1);
        SetupTree(bookcase, shelf, room);

        var act = () => _service.Move(2, new MoveStorageLocationCommand { LocationId = 1, ParentId = 3 });

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.StorageInvalidParent);
        _locationRepositoryMock.Verify(x => x.AnyAsync(LocationSpec(1), CancellationToken.None), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetAllTrackedAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldMoveEverythingInsideOneLevelUp()
    {
        var room = Room(1, 1);
        var box = Room(2, 1, room);
        var inner = Room(3, 1, box);
        SetupTree(room, box, inner);
        var game = StoredGame(1, 1, 3, 4);
        SetupStored([2, 3], [game]);

        await _service.Delete(2);

        game.StorageLocationId.Should().Be(1);
        game.StoredAtLocationId.Should().Be(1);
        game.StoragePosition.Should().Be(4);
        _storageRepositoryMock.Verify(x => x.GetAllTrackedAsync(), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetGamesStoredInAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetExpansionsStoredInAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
        _storageRepositoryMock.Verify(x => x.DeleteAsync(2), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldLeaveGamesDirectlyInTheLocation_WhenItWasAtTheTop()
    {
        var room = Room(1, 1);
        SetupTree(room);
        var game = StoredGame(1, 1, 1);
        SetupStored([1], [game]);

        await _service.Delete(1);

        game.StoredAtLocationId.Should().Be(1);
        game.StorageLocationId.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldThrow_WhenTheStorageLocationDoesNotExist()
    {
        SetupTree();

        var act = () => _service.Delete(5);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task Assign_ShouldPutTheItemsBehindWhatIsAlreadyThere()
    {
        var shelf = new StorageLocation("Billy", 2, null, StorageLayout.Shelves, 1).Children.Single();
        shelf.Id = 8;
        _storageRepositoryMock.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(shelf);
        _storageRepositoryMock.Setup(x => x.GetNextPositionAsync(2, 8)).ReturnsAsync(3);
        var game = new Game("Wingspan") { Id = 1 };
        var expansion = new Expansion("Oceania", 5, 1) { Id = 7 };
        SetupItems([game], [expansion]);

        await _service.Assign(new AssignStorageCommand
        {
            StorageLocationId = 8,
            Items = [new StorageItemReference { Type = StorageItemType.Expansion, Id = 7 }, new StorageItemReference { Type = StorageItemType.Game, Id = 1 }]
        });

        expansion.StoragePosition.Should().Be(3);
        game.StoragePosition.Should().Be(4);
        game.StoredAtLocationId.Should().Be(2);
        game.StorageLocationId.Should().Be(8);
        _storageRepositoryMock.Verify(x => x.GetGamesAsync(It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1 }))), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetExpansionsAsync(It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 7 }))), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetByIdAsync(8), Times.Once);
        _storageRepositoryMock.Verify(x => x.GetNextPositionAsync(2, 8), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Assign_ShouldStoreDirectlyInALocation()
    {
        SetupLocation(4);
        _storageRepositoryMock.Setup(x => x.GetNextPositionAsync(4, null)).ReturnsAsync(0);
        var game = new Game("Skull") { Id = 1 };
        SetupItems([game], []);

        await _service.Assign(new AssignStorageCommand { LocationId = 4, Items = [new StorageItemReference { Type = StorageItemType.Game, Id = 1 }] });

        game.StoredAtLocationId.Should().Be(4);
        game.StorageLocationId.Should().BeNull();
    }

    [Fact]
    public async Task Assign_ShouldClearThePlace_WithoutATarget()
    {
        var game = StoredGame(1, 1, 3);
        var expansion = new Expansion("Oceania", 5, 1) { Id = 7 };
        expansion.Store(1, 3, 0);
        SetupItems([game], [expansion]);

        await _service.Assign(new AssignStorageCommand
        {
            Items = [new StorageItemReference { Type = StorageItemType.Game, Id = 1 }, new StorageItemReference { Type = StorageItemType.Expansion, Id = 7 }]
        });

        game.StoredAtLocationId.Should().BeNull();
        expansion.StorageLocationId.Should().BeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Assign_ShouldThrow_WhenAnItemDoesNotExist()
    {
        SetupItems([], []);

        var act = () => _service.Assign(new AssignStorageCommand { Items = [new StorageItemReference { Type = StorageItemType.Game, Id = 3 }] });

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Assign_ShouldRefuseAWantedGame()
    {
        SetupLocation(1);
        _storageRepositoryMock.Setup(x => x.GetNextPositionAsync(1, null)).ReturnsAsync(0);
        SetupItems([new Game("Ark Nova", state: GameState.Wanted) { Id = 1 }], []);

        var act = () => _service.Assign(new AssignStorageCommand { LocationId = 1, Items = [new StorageItemReference { Type = StorageItemType.Game, Id = 1 }] });

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.StorageNotAllowedForState);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Arrange_ShouldNumberTheItemsInTheGivenOrder()
    {
        var room = Room(5, 1);
        _storageRepositoryMock.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(room);
        var first = StoredGame(1, 1, 5, 9);
        var second = StoredGame(2, 1, 6, 0);
        SetupItems([first, second], []);

        await _service.Arrange(new ArrangeStorageCommand
        {
            StorageLocationId = 5,
            Items = [new StorageItemReference { Type = StorageItemType.Game, Id = 2 }, new StorageItemReference { Type = StorageItemType.Game, Id = 1 }]
        });

        second.StoragePosition.Should().Be(0);
        second.StorageLocationId.Should().Be(5);
        first.StoragePosition.Should().Be(1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateBoxOrientation_ShouldTurnTheBox()
    {
        var game = new Game("Catan") { Id = 1 };
        SetupItems([game], []);

        await _service.UpdateBoxOrientation(new UpdateBoxOrientationCommand
        {
            Item = new StorageItemReference { Type = StorageItemType.Game, Id = 1 },
            Orientation = BoxOrientation.Flat
        });

        game.BoxOrientation.Should().Be(BoxOrientation.Flat);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
