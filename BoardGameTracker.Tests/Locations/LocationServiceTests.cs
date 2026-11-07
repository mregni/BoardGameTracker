using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
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
using BoardGameTracker.Tests.Locations.Specifications;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Locations;

public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _locationRepositoryMock;
    private readonly Mock<IReadRepository<Session>> _sessionRepositoryMock;
    private readonly Mock<IReadRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly LocationService _locationService;

    public LocationServiceTests()
    {
        _locationRepositoryMock = new Mock<ILocationRepository>();
        _sessionRepositoryMock = new Mock<IReadRepository<Session>>();
        _gameNightRepositoryMock = new Mock<IReadRepository<GameNight>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _locationService = new LocationService(
            _locationRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _gameNightRepositoryMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<LocationService>.Instance);
    }

    private void VerifyNoOtherCalls()
    {
        _locationRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    private static LocationWithNameSpec NameSpec(string pattern, int? excludedId = null) =>
        It.Is<LocationWithNameSpec>(s => LocationWithNameSpecTests.Matches(s, pattern, excludedId));

    private static TSpec LocationSpec<TSpec>(int id) where TSpec : class, ISpecification<Location> =>
        It.Is<TSpec>(s => s.IsSatisfiedBy(new Location("Home") { Id = id }) && !s.IsSatisfiedBy(new Location("Home") { Id = id + 1 }));

    private static GameNightsAtLocationSpec NightsAt(int id) =>
        It.Is<GameNightsAtLocationSpec>(s => s.IsSatisfiedBy(NightAt(id)) && !s.IsSatisfiedBy(NightAt(id + 1)));

    private static SessionsByLocationRecentFirstSpec SessionsAt(int id, int take) =>
        It.Is<SessionsByLocationRecentFirstSpec>(s => s.IsSatisfiedBy(SessionAt(id)) && !s.IsSatisfiedBy(SessionAt(id + 1)) && s.Take == take);

    private static GameNight NightAt(int locationId) => GameNight.Create("Night", string.Empty, new DateTime(2026, 10, 16, 20, 0, 0, DateTimeKind.Utc), 1, locationId);

    private static Session SessionAt(int locationId) => new SessionBuilder().AtLocation(new Location("Home") { Id = locationId }).Build();

    private void VerifyNameChecked(string pattern, int? excludedId = null)
    {
        _locationRepositoryMock.Verify(x => x.AnyAsync(NameSpec(pattern, excludedId), CancellationToken.None), Times.Once);
    }

    private void VerifyGameNightsChecked(int id)
    {
        _gameNightRepositoryMock.Verify(x => x.AnyAsync(NightsAt(id), CancellationToken.None), Times.Once);
    }

    private void VerifySessionsChecked(int id)
    {
        _sessionRepositoryMock.Verify(x => x.AnyAsync(SessionsAt(id, 1), CancellationToken.None), Times.Once);
    }

    #region GetLocations Tests

    [Fact]
    public async Task GetLocations_ShouldReturnAllLocations_WhenLocationsExist()
    {
        var locations = new List<LocationDto>
        {
            new() { Id = 1, Name = "Living Room", PlayCount = 4 },
            new() { Id = 2, Name = "Game Store" },
            new() { Id = 3, Name = "Friend's House" }
        };

        _locationRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<LocationsOverviewSpec>(), CancellationToken.None))
            .ReturnsAsync(locations);

        var result = await _locationService.GetLocations();

        result.Should().HaveCount(3);
        result.Should().Contain(l => l.Name == "Living Room" && l.PlayCount == 4);
        result.Should().Contain(l => l.Name == "Game Store");
        result.Should().Contain(l => l.Name == "Friend's House");

        _locationRepositoryMock.Verify(x => x.ListAsync(It.IsAny<LocationsOverviewSpec>(), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnLocation_WhenLocationExists()
    {
        var location = new Location("Living Room") { Id = 1 };

        _locationRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(location);

        var result = await _locationService.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Living Room");

        _locationRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenLocationDoesNotExist()
    {
        _locationRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Location?)null);

        var result = await _locationService.GetByIdAsync(999);

        result.Should().BeNull();

        _locationRepositoryMock.Verify(x => x.GetByIdAsync(999), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ShouldCreateLocation_AndSaveChanges()
    {
        var command = new CreateLocationCommand { Name = "New Location" };

        _locationRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Location>(l => l.Name == "New Location")))
            .ReturnsAsync((Location l) => l);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _locationService.Create(command);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Location");

        VerifyNameChecked("New Location");
        _locationRepositoryMock.Verify(x => x.CreateAsync(It.Is<Location>(l => l.Name == "New Location")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldTrimName()
    {
        var command = new CreateLocationCommand { Name = "  Attic  " };

        _locationRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Location>(l => l.Name == "Attic")))
            .ReturnsAsync((Location l) => l);

        var result = await _locationService.Create(command);

        result.Name.Should().Be("Attic");

        VerifyNameChecked("Attic");
        _locationRepositoryMock.Verify(x => x.CreateAsync(It.Is<Location>(l => l.Name == "Attic")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_ShouldThrowValidationException_WhenNameIsBlank(string? name)
    {
        var command = new CreateLocationCommand { Name = name! };

        var action = async () => await _locationService.Create(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.LocationNameRequired);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldThrowValidationException_WhenNameAlreadyExists()
    {
        var command = new CreateLocationCommand { Name = "living room" };

        _locationRepositoryMock
            .Setup(x => x.AnyAsync(NameSpec("living room"), CancellationToken.None))
            .ReturnsAsync(true);

        var action = async () => await _locationService.Create(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.LocationNameAlreadyExists);

        VerifyNameChecked("living room");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldDeleteLocation_AndSaveChanges()
    {
        var locationId = 1;

        _gameNightRepositoryMock
            .Setup(x => x.AnyAsync(NightsAt(locationId), CancellationToken.None))
            .ReturnsAsync(false);

        _sessionRepositoryMock
            .Setup(x => x.AnyAsync(SessionsAt(locationId, 1), CancellationToken.None))
            .ReturnsAsync(false);

        _locationRepositoryMock
            .Setup(x => x.DeleteAsync(locationId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _locationService.Delete(locationId);

        VerifyGameNightsChecked(locationId);
        VerifySessionsChecked(locationId);
        _locationRepositoryMock.Verify(x => x.DeleteAsync(locationId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldThrowDomainException_WhenLocationIsUsedByGameNights()
    {
        _gameNightRepositoryMock
            .Setup(x => x.AnyAsync(NightsAt(1), CancellationToken.None))
            .ReturnsAsync(true);

        var act = async () => await _locationService.Delete(1);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.LocationUsedByGameNights);

        VerifyGameNightsChecked(1);
        _locationRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldThrowDomainException_WhenLocationIsUsedBySessions()
    {
        _gameNightRepositoryMock
            .Setup(x => x.AnyAsync(NightsAt(1), CancellationToken.None))
            .ReturnsAsync(false);
        _sessionRepositoryMock
            .Setup(x => x.AnyAsync(SessionsAt(1, 1), CancellationToken.None))
            .ReturnsAsync(true);

        var act = async () => await _locationService.Delete(1);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.LocationUsedBySessions);

        VerifyGameNightsChecked(1);
        VerifySessionsChecked(1);
        _locationRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdateLocation_AndSaveChanges()
    {
        var command = new UpdateLocationCommand { Id = 1, Name = "Updated Location" };
        var existingLocation = new Location("Old Location") { Id = 1 };

        _locationRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existingLocation);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _locationService.Update(command);

        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Location");

        _locationRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        VerifyNameChecked("Updated Location", 1);
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
            .Setup(x => x.AnyAsync(NameSpec("Game Store", 1), CancellationToken.None))
            .ReturnsAsync(true);

        var action = async () => await _locationService.Update(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.LocationNameAlreadyExists);
        existingLocation.Name.Should().Be("Old Location");

        _locationRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        VerifyNameChecked("Game Store", 1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldThrowEntityNotFoundException_WhenLocationNotFound()
    {
        var command = new UpdateLocationCommand { Id = 999, Name = "NonExistent" };

        _locationRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Location?)null);

        var action = async () => await _locationService.Update(command);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(Location));
        error.Which.EntityId.Should().Be(999);

        _locationRepositoryMock.Verify(x => x.GetByIdAsync(999), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldStoreTheTrimmedName()
    {
        var existingLocation = new Location("Old Location") { Id = 1 };
        _locationRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingLocation);

        var result = await _locationService.Update(new UpdateLocationCommand { Id = 1, Name = "  Attic  " });

        result.Should().BeSameAs(existingLocation);
        existingLocation.Name.Should().Be("Attic");
        _locationRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        VerifyNameChecked("Attic", 1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Update_ShouldThrowValidationException_WhenNameIsBlank(string? name)
    {
        var existingLocation = new Location("Old Location") { Id = 1 };
        _locationRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingLocation);

        var action = () => _locationService.Update(new UpdateLocationCommand { Id = 1, Name = name! });

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.LocationNameRequired);
        existingLocation.Name.Should().Be("Old Location");
        _locationRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ShouldReturnLocationCount()
    {
        var expectedCount = 10;
        var token = new CancellationTokenSource().Token;

        _locationRepositoryMock
            .Setup(x => x.CountAsync(token))
            .ReturnsAsync(expectedCount);

        var result = await _locationService.CountAsync(token);

        result.Should().Be(10);

        _locationRepositoryMock.Verify(x => x.CountAsync(token), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Details Tests

    [Fact]
    public async Task GetDetails_ShouldReturnProjectedLocation_WhenLocationExists()
    {
        var location = new LocationDto { Id = 4, Name = "Home", PlayCount = 3 };
        _locationRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(LocationSpec<LocationDetailsSpec>(4), CancellationToken.None))
            .ReturnsAsync(location);

        var result = await _locationService.GetDetails(4);

        result.Should().BeSameAs(location);
        _locationRepositoryMock.Verify(x => x.FirstOrDefaultAsync(LocationSpec<LocationDetailsSpec>(4), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDetails_ShouldReturnNull_WhenLocationDoesNotExist()
    {
        _locationRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(LocationSpec<LocationDetailsSpec>(4), CancellationToken.None))
            .ReturnsAsync((LocationDto?)null);

        var result = await _locationService.GetDetails(4);

        result.Should().BeNull();
        _locationRepositoryMock.Verify(x => x.FirstOrDefaultAsync(LocationSpec<LocationDetailsSpec>(4), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistsAsync_ShouldReturnRepositoryResult(bool exists)
    {
        _locationRepositoryMock
            .Setup(x => x.AnyAsync(LocationSpec<LocationByIdSpec>(2), CancellationToken.None))
            .ReturnsAsync(exists);

        var result = await _locationService.ExistsAsync(2);

        result.Should().Be(exists);
        _locationRepositoryMock.Verify(x => x.AnyAsync(LocationSpec<LocationByIdSpec>(2), CancellationToken.None), Times.Once);
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

    [Theory]
    [InlineData(null, -1)]
    [InlineData(5, 5)]
    public async Task GetSessions_ShouldReturnSessionsAtLocation(int? count, int take)
    {
        var sessions = new List<Session>
        {
            new(1, new System.DateTime(2030, 1, 2), new System.DateTime(2030, 1, 2, 1, 0, 0), string.Empty) { Id = 2 },
            new(1, new System.DateTime(2030, 1, 1), new System.DateTime(2030, 1, 1, 1, 0, 0), string.Empty) { Id = 1 }
        };
        _sessionRepositoryMock
            .Setup(x => x.ListAsync(SessionsAt(3, take), CancellationToken.None))
            .ReturnsAsync(sessions);

        var result = await _locationService.GetSessions(3, count);

        result.Should().BeSameAs(sessions);
        _sessionRepositoryMock.Verify(x => x.ListAsync(SessionsAt(3, take), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
