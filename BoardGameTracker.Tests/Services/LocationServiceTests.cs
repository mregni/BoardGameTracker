using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Locations;
using BoardGameTracker.Core.Locations.Specifications;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class LocationServiceTests
{
    private readonly Mock<IRepository<Location>> _locationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<LocationService>> _loggerMock;
    private readonly LocationService _locationService;

    public LocationServiceTests()
    {
        _locationRepositoryMock = new Mock<IRepository<Location>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<LocationService>>();

        _locationService = new LocationService(
            _locationRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _locationRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    #region GetLocations Tests

    [Fact]
    public async Task GetLocations_ShouldReturnAllLocations_WhenLocationsExist()
    {
        // Arrange
        var locations = new List<Location>
        {
            new Location("Living Room") { Id = 1 },
            new Location("Game Store") { Id = 2 },
            new Location("Friend's House") { Id = 3 }
        };

        _locationRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<LocationsOrderedByNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(locations);

        // Act
        var result = await _locationService.GetLocations();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(l => l.Name == "Living Room");
        result.Should().Contain(l => l.Name == "Game Store");
        result.Should().Contain(l => l.Name == "Friend's House");

        _locationRepositoryMock.Verify(x => x.ListAsync(It.IsAny<LocationsOrderedByNameSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnLocation_WhenLocationExists()
    {
        // Arrange
        var location = new Location("Living Room") { Id = 1 };

        _locationRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(location);

        // Act
        var result = await _locationService.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Living Room");

        _locationRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenLocationDoesNotExist()
    {
        // Arrange
        _locationRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Location?)null);

        // Act
        var result = await _locationService.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();

        _locationRepositoryMock.Verify(x => x.GetByIdAsync(999), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ShouldCreateLocation_AndSaveChanges()
    {
        // Arrange
        var command = new CreateLocationCommand { Name = "New Location" };

        _locationRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Location>(l => l.Name == "New Location")))
            .ReturnsAsync((Location l) => l);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _locationService.Create(command);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Location");

        _locationRepositoryMock.Verify(x => x.CreateAsync(It.Is<Location>(l => l.Name == "New Location")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldDeleteLocation_AndSaveChanges()
    {
        // Arrange
        var locationId = 1;

        _locationRepositoryMock
            .Setup(x => x.DeleteAsync(locationId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _locationService.Delete(locationId);

        // Assert
        _locationRepositoryMock.Verify(x => x.DeleteAsync(locationId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdateLocation_AndSaveChanges()
    {
        // Arrange
        var command = new UpdateLocationCommand { Id = 1, Name = "Updated Location" };
        var existingLocation = new Location("Old Location") { Id = 1 };

        _locationRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existingLocation);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _locationService.Update(command);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Location");

        _locationRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldThrowEntityNotFoundException_WhenLocationNotFound()
    {
        // Arrange
        var command = new UpdateLocationCommand { Id = 999, Name = "NonExistent" };

        _locationRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Location?)null);

        // Act
        var action = async () => await _locationService.Update(command);

        // Assert
        await action.Should().ThrowAsync<EntityNotFoundException>();

        _locationRepositoryMock.Verify(x => x.GetByIdAsync(999), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ShouldReturnLocationCount()
    {
        // Arrange
        var expectedCount = 10;

        _locationRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _locationService.CountAsync();

        // Assert
        result.Should().Be(10);

        _locationRepositoryMock.Verify(x => x.CountAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
