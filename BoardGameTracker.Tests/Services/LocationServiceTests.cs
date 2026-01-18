using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Locations;
using BoardGameTracker.Core.Locations.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _locationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly LocationService _locationService;

    public LocationServiceTests()
    {
        _locationRepositoryMock = new Mock<ILocationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _locationService = new LocationService(
            _locationRepositoryMock.Object,
            _unitOfWorkMock.Object);
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
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(locations);

        // Act
        var result = await _locationService.GetLocations();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(l => l.Name == "Living Room");
        result.Should().Contain(l => l.Name == "Game Store");
        result.Should().Contain(l => l.Name == "Friend's House");

        _locationRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocations_ShouldReturnEmptyList_WhenNoLocationsExist()
    {
        // Arrange
        _locationRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Location>());

        // Act
        var result = await _locationService.GetLocations();

        // Assert
        result.Should().BeEmpty();

        _locationRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ShouldCreateLocation_AndSaveChanges()
    {
        // Arrange
        var location = new Location("New Location");

        _locationRepositoryMock
            .Setup(x => x.CreateAsync(location))
            .ReturnsAsync(location);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _locationService.Create(location);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Location");

        _locationRepositoryMock.Verify(x => x.CreateAsync(location), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedLocation()
    {
        // Arrange
        var location = new Location("Board Game Cafe") { Id = 5 };

        _locationRepositoryMock
            .Setup(x => x.CreateAsync(location))
            .ReturnsAsync(location);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _locationService.Create(location);

        // Assert
        result.Should().BeSameAs(location);
        result.Id.Should().Be(5);
        result.Name.Should().Be("Board Game Cafe");
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

    [Fact]
    public async Task Delete_ShouldCallDeleteAsync_WithCorrectId()
    {
        // Arrange
        var locationId = 42;

        _locationRepositoryMock
            .Setup(x => x.DeleteAsync(locationId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _locationService.Delete(locationId);

        // Assert
        _locationRepositoryMock.Verify(x => x.DeleteAsync(42), Times.Once);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdateLocation_AndSaveChanges()
    {
        // Arrange
        var location = new Location("Updated Location") { Id = 1 };

        _locationRepositoryMock
            .Setup(x => x.UpdateAsync(location))
            .ReturnsAsync(location);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _locationService.Update(location);

        // Assert
        _locationRepositoryMock.Verify(x => x.UpdateAsync(location), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldPassLocationToRepository()
    {
        // Arrange
        var location = new Location("Test Location") { Id = 3 };
        location.UpdateName("Renamed Location");

        _locationRepositoryMock
            .Setup(x => x.UpdateAsync(It.Is<Location>(l => l.Name == "Renamed Location")))
            .ReturnsAsync(location);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _locationService.Update(location);

        // Assert
        _locationRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Location>(l => l.Name == "Renamed Location")), Times.Once);
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ShouldReturnLocationCount()
    {
        // Arrange
        var expectedCount = 10;

        _locationRepositoryMock
            .Setup(x => x.CountAsync())
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _locationService.CountAsync();

        // Assert
        result.Should().Be(10);

        _locationRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountAsync_ShouldReturnZero_WhenNoLocationsExist()
    {
        // Arrange
        _locationRepositoryMock
            .Setup(x => x.CountAsync())
            .ReturnsAsync(0);

        // Act
        var result = await _locationService.CountAsync();

        // Assert
        result.Should().Be(0);

        _locationRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
