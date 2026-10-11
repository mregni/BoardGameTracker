using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Locations.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Locations;

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
        var locations = new List<LocationDto>
        {
            new() { Id = 1, Name = "Home", PlayCount = 2 },
            new() { Id = 2, Name = "Office" },
            new() { Id = 3, Name = "Café" }
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
    public async Task CreateLocation_ShouldReturnCreatedWithLocation_WhenLocationIsCreated()
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
        var okResult = result.Should().BeOfType<CreatedResult>().Subject;
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

    [Fact]
    public async Task GetLocationById_ShouldReturnOk_WhenLocationExists()
    {
        var location = new LocationDto { Id = 3, Name = "Home", PlayCount = 2 };
        _locationServiceMock.Setup(x => x.GetDetails(3)).ReturnsAsync(location);

        var result = await _controller.GetLocationById(3);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(location);
        _locationServiceMock.Verify(x => x.GetDetails(3), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocationById_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        _locationServiceMock.Setup(x => x.GetDetails(3)).ReturnsAsync((LocationDto?)null);

        var result = await _controller.GetLocationById(3);

        result.Should().BeOfType<NotFoundResult>();
        _locationServiceMock.Verify(x => x.GetDetails(3), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocationStatistics_ShouldReturnOk_WhenLocationExists()
    {
        var statistics = new LocationStatisticsDto { SessionCount = 4, DistinctGameCount = 2, DistinctPlayerCount = 3, TotalPlayedTime = 120 };
        _locationServiceMock.Setup(x => x.ExistsAsync(3)).ReturnsAsync(true);
        _locationServiceMock.Setup(x => x.GetStatistics(3)).ReturnsAsync(statistics);

        var result = await _controller.GetLocationStatistics(3);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(statistics);
        _locationServiceMock.Verify(x => x.ExistsAsync(3), Times.Once);
        _locationServiceMock.Verify(x => x.GetStatistics(3), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocationStatistics_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        _locationServiceMock.Setup(x => x.ExistsAsync(3)).ReturnsAsync(false);

        var result = await _controller.GetLocationStatistics(3);

        result.Should().BeOfType<NotFoundResult>();
        _locationServiceMock.Verify(x => x.ExistsAsync(3), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocationSessions_ShouldReturnOkWithSessionDtos()
    {
        var sessions = new List<Session>
        {
            new(1, new System.DateTime(2030, 1, 2), new System.DateTime(2030, 1, 2, 1, 0, 0), string.Empty) { Id = 2 },
            new(1, new System.DateTime(2030, 1, 1), new System.DateTime(2030, 1, 1, 1, 0, 0), string.Empty) { Id = 1 }
        };
        _locationServiceMock.Setup(x => x.GetSessions(3, 10)).ReturnsAsync(sessions);

        var result = await _controller.GetLocationSessions(3, 10);

        var returned = result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;
        returned.Select(x => x.Id).Should().Equal(2, 1);
        _locationServiceMock.Verify(x => x.GetSessions(3, 10), Times.Once);
        VerifyNoOtherCalls();
    }
}
