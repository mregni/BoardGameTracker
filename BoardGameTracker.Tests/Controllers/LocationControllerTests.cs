using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Locations.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class LocationControllerTests
{
    private readonly Mock<ILocationService> _locationServiceMock;
    private readonly LocationController _controller;

    public LocationControllerTests()
    {
        _locationServiceMock = new Mock<ILocationService>();
        _controller = new LocationController(_locationServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _locationServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocations_ShouldReturnOkWithLocations_WhenLocationsExist()
    {
        // Arrange
        var locations = new List<Location>
        {
            new Location("Home") { Id = 1 },
            new Location("Office") { Id = 2 },
            new Location("Café") { Id = 3 }
        };

        _locationServiceMock
            .Setup(x => x.GetLocations())
            .ReturnsAsync(locations);

        // Act
        var result = await _controller.GetLocations();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLocations = okResult.Value.Should().BeAssignableTo<List<LocationDto>>().Subject;

        returnedLocations.Should().HaveCount(3);
        returnedLocations[0].Name.Should().Be("Home");
        returnedLocations[1].Name.Should().Be("Office");
        returnedLocations[2].Name.Should().Be("Café");

        _locationServiceMock.Verify(x => x.GetLocations(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocations_ShouldReturnOkWithEmptyList_WhenNoLocationsExist()
    {
        // Arrange
        _locationServiceMock
            .Setup(x => x.GetLocations())
            .ReturnsAsync([]);

        // Act
        var result = await _controller.GetLocations();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLocations = okResult.Value.Should().BeAssignableTo<List<LocationDto>>().Subject;

        returnedLocations.Should().BeEmpty();

        _locationServiceMock.Verify(x => x.GetLocations(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateLocation_ShouldReturnOkWithCreatedLocation_WhenLocationIsCreated()
    {
        // Arrange
        var command = new CreateLocationCommand
        {
            Name = "Game Store"
        };

        var createdLocation = new Location(command.Name) { Id = 1 };

        _locationServiceMock
            .Setup(x => x.Create(command))
            .ReturnsAsync(createdLocation);

        // Act
        var result = await _controller.CreateLocation(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var locationDto = okResult.Value.Should().BeAssignableTo<LocationDto>().Subject;

        locationDto.Id.Should().Be(1);
        locationDto.Name.Should().Be("Game Store");

        _locationServiceMock.Verify(x => x.Create(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLocation_ShouldReturnOkWithUpdatedLocation_WhenLocationIsUpdated()
    {
        // Arrange
        var command = new UpdateLocationCommand
        {
            Id = 1,
            Name = "Updated Home"
        };

        var updatedLocation = new Location(command.Name) { Id = command.Id };

        _locationServiceMock
            .Setup(x => x.Update(command))
            .ReturnsAsync(updatedLocation);

        // Act
        var result = await _controller.UpdateLocation(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var locationDto = okResult.Value.Should().BeAssignableTo<LocationDto>().Subject;

        locationDto.Id.Should().Be(1);
        locationDto.Name.Should().Be("Updated Home");

        _locationServiceMock.Verify(x => x.Update(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLocation_ShouldThrow_WhenLocationDoesNotExist()
    {
        // Arrange
        var command = new UpdateLocationCommand
        {
            Id = 999,
            Name = "NonExistent"
        };

        _locationServiceMock
            .Setup(x => x.Update(command))
            .ThrowsAsync(new EntityNotFoundException(nameof(Location), command.Id));

        // Act
        var action = async () => await _controller.UpdateLocation(command);

        // Assert
        await action.Should().ThrowAsync<EntityNotFoundException>();

        _locationServiceMock.Verify(x => x.Update(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteLocation_ShouldReturnNoContent_WhenLocationIsDeleted()
    {
        // Arrange
        var locationId = 1;
        var location = new Location("Test") { Id = locationId };

        _locationServiceMock
            .Setup(x => x.GetByIdAsync(locationId))
            .ReturnsAsync(location);

        _locationServiceMock
            .Setup(x => x.Delete(locationId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteLocation(locationId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _locationServiceMock.Verify(x => x.GetByIdAsync(locationId), Times.Once);
        _locationServiceMock.Verify(x => x.Delete(locationId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteLocation_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        // Arrange
        var locationId = 999;

        _locationServiceMock
            .Setup(x => x.GetByIdAsync(locationId))
            .ReturnsAsync((Location?)null);

        // Act
        var result = await _controller.DeleteLocation(locationId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _locationServiceMock.Verify(x => x.GetByIdAsync(locationId), Times.Once);
        VerifyNoOtherCalls();
    }
}
