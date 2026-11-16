using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Storage.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Storage;

public class StorageControllerTests
{
    private readonly Mock<IStorageService> _storageServiceMock = new();
    private readonly StorageController _controller;

    public StorageControllerTests()
    {
        _controller = new StorageController(_storageServiceMock.Object);
    }

    [Fact]
    public async Task GetStorageLocations_ShouldReturnEveryNode()
    {
        var bookcase = new StorageLocation("Billy", 1, null, StorageLayout.Shelves, 2) { Id = 3 };
        _storageServiceMock.Setup(x => x.GetAll()).ReturnsAsync([bookcase, .. bookcase.Children]);

        var result = await _controller.GetStorageLocations();

        var list = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeAssignableTo<List<StorageLocationDto>>().Subject;
        list.Should().HaveCount(3);
        list[0].Should().BeEquivalentTo(new StorageLocationDto { Id = 3, Name = "Billy", LocationId = 1, Layout = StorageLayout.Shelves, Rows = 2, Columns = 1 });
        list.Skip(1).Should().OnlyContain(x => x.Layout == StorageLayout.Slot && x.Name == null);
        _storageServiceMock.Verify(x => x.GetAll(), Times.Once);
        _storageServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateStorageLocation_ShouldReturnCreated()
    {
        var command = new CreateStorageLocationCommand { Name = "Attic", LocationId = 1, Layout = StorageLayout.Free };
        _storageServiceMock.Setup(x => x.Create(command)).ReturnsAsync(new StorageLocation("Attic", 1, null, StorageLayout.Free) { Id = 9 });

        var result = await _controller.CreateStorageLocation(command);

        result.Should().BeOfType<CreatedResult>().Subject.Value.Should().BeOfType<StorageLocationDto>().Which.Id.Should().Be(9);
        _storageServiceMock.Verify(x => x.Create(command), Times.Once);
        _storageServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAndMove_ShouldReturnTheStorageLocation()
    {
        var update = new UpdateStorageLocationCommand { Name = "Loft" };
        var move = new MoveStorageLocationCommand { LocationId = 2 };
        var attic = new StorageLocation("Loft", 2, null, StorageLayout.Free) { Id = 9 };
        _storageServiceMock.Setup(x => x.Update(9, update)).ReturnsAsync(attic);
        _storageServiceMock.Setup(x => x.Move(9, move)).ReturnsAsync(attic);

        (await _controller.UpdateStorageLocation(9, update)).Should().BeOfType<OkObjectResult>();
        (await _controller.MoveStorageLocation(9, move)).Should().BeOfType<OkObjectResult>();

        _storageServiceMock.Verify(x => x.Update(9, update), Times.Once);
        _storageServiceMock.Verify(x => x.Move(9, move), Times.Once);
        _storageServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Writes_ShouldReturnNoContent()
    {
        var assign = new AssignStorageCommand();
        var arrange = new ArrangeStorageCommand();
        var orientation = new UpdateBoxOrientationCommand { Item = new StorageItemReference { Id = 1 } };

        (await _controller.DeleteStorageLocation(4)).Should().BeOfType<NoContentResult>();
        (await _controller.Assign(assign)).Should().BeOfType<NoContentResult>();
        (await _controller.Arrange(arrange)).Should().BeOfType<NoContentResult>();
        (await _controller.UpdateBoxOrientation(orientation)).Should().BeOfType<NoContentResult>();

        _storageServiceMock.Verify(x => x.Delete(4), Times.Once);
        _storageServiceMock.Verify(x => x.Assign(assign), Times.Once);
        _storageServiceMock.Verify(x => x.Arrange(arrange), Times.Once);
        _storageServiceMock.Verify(x => x.UpdateBoxOrientation(orientation), Times.Once);
        _storageServiceMock.VerifyNoOtherCalls();
    }
}
