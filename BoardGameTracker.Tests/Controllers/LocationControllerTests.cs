using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Locations.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class LocationControllerTests
{
    private readonly Mock<ILocationService> _locationServiceMock;
    private readonly Mock<ILogger<LocationController>> _loggerMock;
    private readonly LocationController _controller;

    public LocationControllerTests()
    {
        _locationServiceMock = new Mock<ILocationService>();
        _loggerMock = new Mock<ILogger<LocationController>>();
        _controller = new LocationController(_locationServiceMock.Object, _loggerMock.Object);
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
            .ReturnsAsync(new List<Location>());

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
            .Setup(x => x.Create(It.IsAny<Location>()))
            .ReturnsAsync(createdLocation);

        // Act
        var result = await _controller.CreateLocation(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var locationDto = okResult.Value.Should().BeAssignableTo<LocationDto>().Subject;

        locationDto.Id.Should().Be(1);
        locationDto.Name.Should().Be("Game Store");

        _locationServiceMock.Verify(x => x.Create(It.Is<Location>(l => l.Name == command.Name)), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateLocation_ShouldReturnBadRequest_WhenCommandIsNull()
    {
        // Act
        var result = await _controller.CreateLocation(null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateLocation_ShouldReturnInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var command = new CreateLocationCommand
        {
            Name = "Library"
        };

        var expectedException = new InvalidOperationException("Database error");

        _locationServiceMock
            .Setup(x => x.Create(It.IsAny<Location>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.CreateLocation(command);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        _locationServiceMock.Verify(x => x.Create(It.IsAny<Location>()), Times.Once);
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

        _locationServiceMock
            .Setup(x => x.Update(It.IsAny<Location>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateLocation(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var locationDto = okResult.Value.Should().BeAssignableTo<LocationDto>().Subject;

        locationDto.Id.Should().Be(1);
        locationDto.Name.Should().Be("Updated Home");

        _locationServiceMock.Verify(x => x.Update(It.Is<Location>(l => l.Id == command.Id && l.Name == command.Name)), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLocation_ShouldReturnBadRequest_WhenCommandIsNull()
    {
        // Act
        var result = await _controller.UpdateLocation(null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLocation_ShouldReturnInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var command = new UpdateLocationCommand
        {
            Id = 1,
            Name = "Park"
        };

        var expectedException = new TimeoutException("Update timeout");

        _locationServiceMock
            .Setup(x => x.Update(It.IsAny<Location>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.UpdateLocation(command);

        // Assert
        var statusCodeResult = result.Should().BeOfType<StatusCodeResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        _locationServiceMock.Verify(x => x.Update(It.IsAny<Location>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteLocation_ShouldReturnOkWithSuccess_WhenLocationIsDeleted()
    {
        // Arrange
        var locationId = 1;

        _locationServiceMock
            .Setup(x => x.Delete(locationId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteLocation(locationId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        response.Should().NotBeNull();

        _locationServiceMock.Verify(x => x.Delete(locationId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteLocation_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var locationId = 999;
        var expectedException = new InvalidOperationException("Cannot delete location");

        _locationServiceMock
            .Setup(x => x.Delete(locationId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.DeleteLocation(locationId));

        exception.Should().Be(expectedException);

        _locationServiceMock.Verify(x => x.Delete(locationId), Times.Once);
        VerifyNoOtherCalls();
    }
}
