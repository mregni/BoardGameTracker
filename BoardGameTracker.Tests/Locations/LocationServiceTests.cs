using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Locations;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using BoardGameTracker.Core.Locations.Specifications;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Locations;

public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _locationRepositoryMock;
    private readonly Mock<IReadRepository<Session>> _sessionRepositoryMock;
    private readonly Mock<IReadRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<LocationService>> _loggerMock;
    private readonly LocationService _locationService;

    public LocationServiceTests()
    {
        _locationRepositoryMock = new Mock<ILocationRepository>();
        _sessionRepositoryMock = new Mock<IReadRepository<Session>>();
        _gameNightRepositoryMock = new Mock<IReadRepository<GameNight>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<LocationService>>();

        _locationService = new LocationService(
            _locationRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _gameNightRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _locationRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    private void VerifyNameChecked()
    {
        _locationRepositoryMock.Verify(
            x => x.AnyAsync(It.IsAny<LocationWithNameSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyGameNightsChecked()
    {
        _gameNightRepositoryMock.Verify(
            x => x.AnyAsync(It.IsAny<GameNightsAtLocationSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #region GetLocations Tests

    [Fact]
    public async Task GetLocations_ShouldReturnAllLocations_WhenLocationsExist()
    {
        // Arrange
        var locations = new List<LocationDto>
        {
            new() { Id = 1, Name = "Living Room", PlayCount = 4 },
            new() { Id = 2, Name = "Game Store" },
            new() { Id = 3, Name = "Friend's House" }
        };

        _locationRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<LocationsOverviewSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(locations);

        // Act
        var result = await _locationService.GetLocations();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(l => l.Name == "Living Room" && l.PlayCount == 4);
        result.Should().Contain(l => l.Name == "Game Store");
        result.Should().Contain(l => l.Name == "Friend's House");

        _locationRepositoryMock.Verify(x => x.ListAsync(It.IsAny<LocationsOverviewSpec>(), It.IsAny<CancellationToken>()), Times.Once);
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

        VerifyNameChecked();
        _locationRepositoryMock.Verify(x => x.CreateAsync(It.Is<Location>(l => l.Name == "New Location")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldTrimName()
    {
        var command = new CreateLocationCommand { Name = "  Attic  " };

        _locationRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Location>()))
            .ReturnsAsync((Location l) => l);

        var result = await _locationService.Create(command);

        result.Name.Should().Be("Attic");

        VerifyNameChecked();
        _locationRepositoryMock.Verify(x => x.CreateAsync(It.Is<Location>(l => l.Name == "Attic")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_ShouldThrowValidationException_WhenNameIsBlank(string name)
    {
        var command = new CreateLocationCommand { Name = name };

        var action = async () => await _locationService.Create(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.LocationNameRequired);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldThrowValidationException_WhenNameAlreadyExists()
    {
        var command = new CreateLocationCommand { Name = "living room" };

        _locationRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<LocationWithNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = async () => await _locationService.Create(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.LocationNameAlreadyExists);

        VerifyNameChecked();
        VerifyNoOtherCalls();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldDeleteLocation_AndSaveChanges()
    {
        // Arrange
        var locationId = 1;

        _gameNightRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<GameNightsAtLocationSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _locationRepositoryMock
            .Setup(x => x.DeleteAsync(locationId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _locationService.Delete(locationId);

        // Assert
        VerifyGameNightsChecked();
        _locationRepositoryMock.Verify(x => x.DeleteAsync(locationId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldThrowDomainException_WhenLocationIsUsedByGameNights()
    {
        _gameNightRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<GameNightsAtLocationSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _locationService.Delete(1);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.LocationUsedByGameNights);

        VerifyGameNightsChecked();
        _locationRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
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
        VerifyNameChecked();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldThrowValidationException_WhenAnotherLocationHasTheName()
    {
        var command = new UpdateLocationCommand { Id = 1, Name = "Game Store" };
        var existingLocation = new Location("Old Location") { Id = 1 };

        _locationRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existingLocation);
        _locationRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<LocationWithNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = async () => await _locationService.Update(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.LocationNameAlreadyExists);
        existingLocation.Name.Should().Be("Old Location");

        _locationRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        VerifyNameChecked();
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

    #region Details Tests

    [Fact]
    public async Task GetDetails_ShouldReturnProjectedLocation_WhenLocationExists()
    {
        var location = new LocationDto { Id = 4, Name = "Home", PlayCount = 3 };
        _locationRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LocationDetailsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var result = await _locationService.GetDetails(4);

        result.Should().BeSameAs(location);
        _locationRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.IsAny<LocationDetailsSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDetails_ShouldReturnNull_WhenLocationDoesNotExist()
    {
        _locationRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LocationDetailsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocationDto?)null);

        var result = await _locationService.GetDetails(4);

        result.Should().BeNull();
        _locationRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.IsAny<LocationDetailsSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistsAsync_ShouldReturnRepositoryResult(bool exists)
    {
        _locationRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<LocationByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);

        var result = await _locationService.ExistsAsync(2);

        result.Should().Be(exists);
        _locationRepositoryMock.Verify(x => x.AnyAsync(It.IsAny<LocationByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldCombineRepositoryAggregates()
    {
        _locationRepositoryMock.Setup(x => x.GetDistinctGameCount(3)).ReturnsAsync(6);
        _locationRepositoryMock.Setup(x => x.GetDistinctPlayerCount(3)).ReturnsAsync(5);
        _locationRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(3)).ReturnsAsync(460);

        var result = await _locationService.GetStatistics(3);

        result.DistinctGameCount.Should().Be(6);
        result.DistinctPlayerCount.Should().Be(5);
        result.TotalPlayedTime.Should().Be(460);
        _locationRepositoryMock.Verify(x => x.GetDistinctGameCount(3), Times.Once);
        _locationRepositoryMock.Verify(x => x.GetDistinctPlayerCount(3), Times.Once);
        _locationRepositoryMock.Verify(x => x.GetPlayLengthInMinutes(3), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSessions_ShouldReturnSessionsAtLocation()
    {
        var sessions = new List<Session>
        {
            new(1, new System.DateTime(2030, 1, 2), new System.DateTime(2030, 1, 2, 1, 0, 0), string.Empty) { Id = 2 },
            new(1, new System.DateTime(2030, 1, 1), new System.DateTime(2030, 1, 1, 1, 0, 0), string.Empty) { Id = 1 }
        };
        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<SessionsByLocationRecentFirstSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var result = await _locationService.GetSessions(3);

        result.Should().BeSameAs(sessions);
        _sessionRepositoryMock.Verify(x => x.ListAsync(It.IsAny<SessionsByLocationRecentFirstSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
