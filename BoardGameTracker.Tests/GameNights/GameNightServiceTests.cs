using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;
using BoardGameTracker.Core.GameNights;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Players.Specifications;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class GameNightServiceTests
{
    private readonly Mock<IRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IReadRepository<GameNightRsvp>> _rsvpRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IBackgroundEmailSender> _backgroundEmailSenderMock;
    private readonly Mock<IPublicUrlBuilder> _publicUrlBuilderMock;
    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly Mock<ILogger<GameNightService>> _loggerMock;
    private readonly GameNightService _gameNightService;

    public GameNightServiceTests()
    {
        _gameNightRepositoryMock = new Mock<IRepository<GameNight>>();
        _rsvpRepositoryMock = new Mock<IReadRepository<GameNightRsvp>>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _backgroundEmailSenderMock = new Mock<IBackgroundEmailSender>();
        _publicUrlBuilderMock = new Mock<IPublicUrlBuilder>();
        _configRepositoryMock = new Mock<IConfigRepository>();
        _loggerMock = new Mock<ILogger<GameNightService>>();

        _gameNightService = new GameNightService(
            _gameNightRepositoryMock.Object,
            _rsvpRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _gameRepositoryMock.Object,
            _playerRepositoryMock.Object,
            _emailServiceMock.Object,
            _backgroundEmailSenderMock.Object,
            _publicUrlBuilderMock.Object,
            _dateTimeProviderMock.Object,
            new MemoryCache(new MemoryCacheOptions()),
            _configRepositoryMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _rsvpRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _gameRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _emailServiceMock.VerifyNoOtherCalls();
        _backgroundEmailSenderMock.VerifyNoOtherCalls();
        _publicUrlBuilderMock.VerifyNoOtherCalls();
        _configRepositoryMock.VerifyNoOtherCalls();
    }

    private void SetupRsvpAuthentication(bool required)
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false))
            .ReturnsAsync(required);
    }

    private void SetupGameNightsEnabled(bool enabled)
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true))
            .ReturnsAsync(enabled);
    }

    private void VerifyGameNightsEnabledChecked()
    {
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true), Times.Once);
    }

    #region GetGameNights Tests

    [Fact]
    public async Task GetGameNights_ShouldReturnAllGameNights_WhenGameNightsExist()
    {
        var gameNights = new List<GameNight>
        {
            GameNight.Create("Night 1", "Notes 1", DateTime.UtcNow.AddDays(1), 1, 1),
            GameNight.Create("Night 2", "Notes 2", DateTime.UtcNow.AddDays(2), 2, 1),
        };

        SetupGameNightsEnabled(true);
        _gameNightRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNights);

        var result = await _gameNightService.GetGameNights();

        result.Should().HaveCount(2);
        result.Should().Contain(g => g.Title == "Night 1");
        result.Should().Contain(g => g.Title == "Night 2");

        _gameNightRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameNights_ShouldReturnEmptyList_WhenNoGameNightsExist()
    {
        SetupGameNightsEnabled(true);
        _gameNightRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _gameNightService.GetGameNights();

        result.Should().BeEmpty();

        _gameNightRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameNights_ShouldThrowFeatureDisabled_WhenGameNightsAreDisabled()
    {
        SetupGameNightsEnabled(false);

        var action = async () => await _gameNightService.GetGameNights();

        await action.Should().ThrowAsync<FeatureDisabledException>()
            .Where(e => e.Feature == Constants.AppConfig.GameNightsEnabled);

        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ShouldReturnGameNight_WhenGameNightExists()
    {
        var gameNightId = 1;
        var gameNight = GameNight.Create("Night 1", "Notes", DateTime.UtcNow.AddDays(1), 1, 1);

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetById(gameNightId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Night 1");

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenGameNightDoesNotExist()
    {
        var gameNightId = 999;

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameNight?)null);

        var result = await _gameNightService.GetById(gameNightId);

        result.Should().BeNull();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region End and Reopen Tests

    [Fact]
    public async Task End_ShouldEndTheNightNow()
    {
        var now = new DateTime(2026, 10, 17, 1, 30, 0, DateTimeKind.Utc);
        var gameNight = GameNight.Create("Night", "", now.AddHours(-5), 1, 1);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<GameNightWithRunningGamesSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.End(4);

        result.Should().BeSameAs(gameNight);
        gameNight.EndedAt.Should().Be(now);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightWithRunningGamesSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task End_ShouldBeRefused_WhileAGameIsRunning()
    {
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow.AddHours(-2), 1, 1);
        gameNight.RunningGames.Add(RunningGame.Create(4, 9, DateTime.UtcNow, [1]));
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<GameNightWithRunningGamesSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);

        var action = () => _gameNightService.End(4);

        var error = await action.Should().ThrowAsync<ValidationException>();
        error.Which.Message.Should().Be(Constants.Errors.GameNightGamesRunning);
        gameNight.EndedAt.Should().BeNull();
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightWithRunningGamesSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(-13, false)]
    [InlineData(-11, true)]
    [InlineData(-12, true)]
    public async Task End_ShouldOnlyBeAllowed_FromTwelveHoursBeforeTheStart(int hoursFromStart, bool allowed)
    {
        var start = new DateTime(2026, 10, 24, 19, 0, 0, DateTimeKind.Utc);
        var now = start.AddHours(hoursFromStart);
        var gameNight = GameNight.Create("Night", "", start, 1, 1);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<GameNightWithRunningGamesSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);

        var action = () => _gameNightService.End(4);

        if (allowed)
        {
            await action.Should().NotThrowAsync();
            gameNight.EndedAt.Should().Be(now);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }
        else
        {
            var error = await action.Should().ThrowAsync<ValidationException>();
            error.Which.Message.Should().Be(Constants.Errors.GameNightNotStarted);
            gameNight.EndedAt.Should().BeNull();
        }

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightWithRunningGamesSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task End_ShouldReturnNull_WhenTheNightDoesNotExist()
    {
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<GameNightWithRunningGamesSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameNight?)null);

        var result = await _gameNightService.End(4);

        result.Should().BeNull();
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightWithRunningGamesSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reopen_ShouldClearTheEnd()
    {
        var now = new DateTime(2026, 10, 17, 1, 30, 0, DateTimeKind.Utc);
        var gameNight = GameNight.Create("Night", "", now.AddHours(-5), 1, 1);
        gameNight.End(now);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);
        _gameNightRepositoryMock.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(gameNight);

        var result = await _gameNightService.Reopen(4);

        result.Should().BeSameAs(gameNight);
        gameNight.EndedAt.Should().BeNull();
        gameNight.IsOver(now).Should().BeFalse();
        _gameNightRepositoryMock.Verify(x => x.GetByIdAsync(4), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(12)]
    [InlineData(30)]
    public async Task Reopen_ShouldBeRefused_OnceThePlayWindowHasPassed(int hoursSinceStart)
    {
        var start = new DateTime(2026, 10, 2, 19, 0, 0, DateTimeKind.Utc);
        var gameNight = GameNight.Create("Night", "", start, 1, 1);
        gameNight.End(start.AddHours(4));
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(start.AddHours(hoursSinceStart));
        _gameNightRepositoryMock.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(gameNight);

        var action = () => _gameNightService.Reopen(4);

        var error = await action.Should().ThrowAsync<ValidationException>();
        error.Which.Message.Should().Be(Constants.Errors.GameNightOver);
        gameNight.EndedAt.Should().Be(start.AddHours(4));
        _gameNightRepositoryMock.Verify(x => x.GetByIdAsync(4), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reopen_ShouldReturnNull_WhenTheNightDoesNotExist()
    {
        _gameNightRepositoryMock.Setup(x => x.GetByIdAsync(4)).ReturnsAsync((GameNight?)null);

        var result = await _gameNightService.Reopen(4);

        result.Should().BeNull();
        _gameNightRepositoryMock.Verify(x => x.GetByIdAsync(4), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Attendance Tests

    private static GameNight NightWithPlay(DateTime start)
    {
        var gameNight = GameNight.Create("Night", "", start, 1, 1);
        var session = new Session(9, start, start.AddHours(1), string.Empty);
        session.AddPlayerSession(2, null, true, false);
        session.AddPlayerSession(3, null, false, false);
        gameNight.Sessions.Add(session);
        gameNight.RunningGames.Add(RunningGame.Create(4, 9, start.AddHours(1), [4]));
        return gameNight;
    }

    private void SetupAttendanceNight(GameNight? gameNight)
    {
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<GameNightAttendanceSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);
    }

    private void VerifyAttendanceNightLoaded()
    {
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightAttendanceSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAttendance_ShouldStoreOnlyTheDifferenceFromWhoPlayed()
    {
        var start = new DateTime(2026, 10, 17, 19, 0, 0, DateTimeKind.Utc);
        var gameNight = NightWithPlay(start);
        SetupAttendanceNight(gameNight);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(start.AddHours(2));
        _playerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<PlayerIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([2, 3, 5]);

        var result = await _gameNightService.UpdateAttendance(4, new UpdateAttendanceCommand { PresentPlayerIds = [2, 3, 5, 5] });

        result.Should().BeSameAs(gameNight);
        gameNight.Attendance.Select(x => (x.PlayerId, x.Present)).Should().Equal((1, false), (5, true));
        gameNight.GetAttendeeIds().Should().Equal(2, 3, 4, 5);
        VerifyAttendanceNightLoaded();
        _playerRepositoryMock.Verify(x => x.ListAsync(It.IsAny<PlayerIdsSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAttendance_ShouldReplaceEarlierOverrides()
    {
        var start = new DateTime(2026, 10, 17, 19, 0, 0, DateTimeKind.Utc);
        var gameNight = NightWithPlay(start);
        gameNight.SetAttendance([5, 6]);
        SetupAttendanceNight(gameNight);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(start.AddHours(20));
        _playerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<PlayerIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 6]);

        await _gameNightService.UpdateAttendance(4, new UpdateAttendanceCommand { PresentPlayerIds = [1, 6] });

        gameNight.Attendance.Select(x => (x.PlayerId, x.Present)).Should().Equal((6, true));
        gameNight.GetAttendeeIds().Should().Equal(1, 2, 3, 4, 6);
        VerifyAttendanceNightLoaded();
        _playerRepositoryMock.Verify(x => x.ListAsync(It.IsAny<PlayerIdsSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAttendance_ShouldReturnNull_WhenTheNightDoesNotExist()
    {
        SetupAttendanceNight(null);

        var result = await _gameNightService.UpdateAttendance(4, new UpdateAttendanceCommand { PresentPlayerIds = [1] });

        result.Should().BeNull();
        VerifyAttendanceNightLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAttendance_ShouldBeRefused_BeforePlayHasOpened()
    {
        var start = new DateTime(2026, 10, 24, 19, 0, 0, DateTimeKind.Utc);
        SetupAttendanceNight(GameNight.Create("Night", "", start, 1, 1));
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(start.AddHours(-13));

        var action = () => _gameNightService.UpdateAttendance(4, new UpdateAttendanceCommand { PresentPlayerIds = [1] });

        var error = await action.Should().ThrowAsync<ValidationException>();
        error.Which.Message.Should().Be(Constants.Errors.GameNightNotStarted);
        VerifyAttendanceNightLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAttendance_ShouldThrow_WhenAPlayerDoesNotExist()
    {
        var start = new DateTime(2026, 10, 17, 19, 0, 0, DateTimeKind.Utc);
        var gameNight = NightWithPlay(start);
        SetupAttendanceNight(gameNight);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(start);
        _playerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<PlayerIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1]);

        var action = () => _gameNightService.UpdateAttendance(4, new UpdateAttendanceCommand { PresentPlayerIds = [1, 77] });

        await action.Should().ThrowAsync<EntityNotFoundException>();
        gameNight.Attendance.Should().BeEmpty();
        VerifyAttendanceNightLoaded();
        _playerRepositoryMock.Verify(x => x.ListAsync(It.IsAny<PlayerIdsSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetDetail Tests

    [Fact]
    public async Task GetDetail_ShouldReturnTheNightWithItsDetails()
    {
        var gameNight = GameNight.Create("Night 1", "Notes", DateTime.UtcNow, 1, 1);
        SetupGameNightsEnabled(true);
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<GameNightDetailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetDetail(1);

        result.Should().BeSameAs(gameNight);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightDetailSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDetail_ShouldThrowFeatureDisabled_WhenGameNightsAreDisabled()
    {
        SetupGameNightsEnabled(false);

        var action = () => _gameNightService.GetDetail(1);

        await action.Should().ThrowAsync<FeatureDisabledException>();

        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ShouldAddHostAsAccepted_WhenHostIsNotInInvitedList()
    {
        var command = new CreateGameNightCommand
        {
            Title = "Game Night",
            Notes = "Some notes",
            StartDate = DateTime.UtcNow.AddDays(7),
            HostId = 10,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = [1, 2, 3]
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([]);

        _gameNightRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<GameNight>()))
            .ReturnsAsync((GameNight g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Create(command);

        result.Should().NotBeNull();
        result.InvitedPlayers.Should().HaveCount(4);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 10 && p.State == GameNightRsvpState.Accepted);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 1 && p.State == GameNightRsvpState.Pending);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 2 && p.State == GameNightRsvpState.Pending);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 3 && p.State == GameNightRsvpState.Pending);

        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<GameNight>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldUpdateHostStateToAccepted_WhenHostIsInInvitedList()
    {
        var command = new CreateGameNightCommand
        {
            Title = "Game Night",
            Notes = "Some notes",
            StartDate = DateTime.UtcNow.AddDays(7),
            HostId = 2,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = [1, 2, 3]
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([]);

        _gameNightRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<GameNight>()))
            .ReturnsAsync((GameNight g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Create(command);

        result.Should().NotBeNull();
        result.InvitedPlayers.Should().HaveCount(3);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 2 && p.State == GameNightRsvpState.Accepted);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 1 && p.State == GameNightRsvpState.Pending);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 3 && p.State == GameNightRsvpState.Pending);

        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<GameNight>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldFetchSuggestedGames_ByProvidedIds()
    {
        var suggestedGameIds = new List<int> { 5, 10, 15 };
        var games = new List<Game>
        {
            new Game("Chess") { Id = 5 },
            new Game("Catan") { Id = 10 },
            new Game("Risk") { Id = 15 },
        };

        var command = new CreateGameNightCommand
        {
            Title = "Game Night",
            Notes = string.Empty,
            StartDate = DateTime.UtcNow.AddDays(1),
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = suggestedGameIds,
            InvitedPlayerIds = []
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(suggestedGameIds))
            .ReturnsAsync(games);

        _gameNightRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<GameNight>()))
            .ReturnsAsync((GameNight g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Create(command);

        result.SuggestedGames.Should().HaveCount(3);
        result.SuggestedGames.Should().Contain(g => g.Id == 5);
        result.SuggestedGames.Should().Contain(g => g.Id == 10);
        result.SuggestedGames.Should().Contain(g => g.Id == 15);

        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(suggestedGameIds), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<GameNight>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldTreatNullCollections_AsEmpty()
    {
        var command = new CreateGameNightCommand
        {
            Title = "Game Night",
            Notes = string.Empty,
            StartDate = DateTime.UtcNow.AddDays(1),
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = null!,
            InvitedPlayerIds = null!
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.Is<List<int>>(ids => ids.Count == 0)))
            .ReturnsAsync([]);

        _gameNightRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<GameNight>()))
            .ReturnsAsync((GameNight g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Create(command);

        result.SuggestedGames.Should().BeEmpty();
        result.InvitedPlayers.Should().ContainSingle(p => p.PlayerId == 1 && p.State == GameNightRsvpState.Accepted);

        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(It.Is<List<int>>(ids => ids.Count == 0)), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<GameNight>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldRefuseSuggestedGamesThatAreNotOwned()
    {
        var command = new CreateGameNightCommand
        {
            Title = "Game Night",
            Notes = string.Empty,
            StartDate = DateTime.UtcNow.AddDays(1),
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = [5, 6],
            InvitedPlayerIds = []
        };
        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([new Game("Chess") { Id = 5 }, new Game("Root", false, GameState.Wanted) { Id = 6 }]);

        var action = async () => await _gameNightService.Create(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.SuggestedGameNotOwned);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdateGameNightProperties_WhenGameNightExists()
    {
        var existingGameNight = GameNight.Create("Old Title", "Old Notes", DateTime.UtcNow, 1, 1);
        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "New Title",
            Notes = "New Notes",
            StartDate = DateTime.UtcNow.AddDays(5),
            HostId = 2,
            LocationId = 3,
            SuggestedGameIds = [7],
            InvitedPlayerIds = []
        };

        var games = new List<Game> { new Game("Catan") { Id = 7 } };

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGameNight);

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync(games);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.Should().NotBeNull();
        result.Title.Should().Be("New Title");
        result.Notes.Should().Be("New Notes");
        result.HostId.Should().Be(2);
        result.LocationId.Should().Be(3);
        result.SuggestedGames.Should().HaveCount(1);
        result.SuggestedGames.Should().Contain(g => g.Id == 7);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldRefuseNewSuggestedGamesThatAreNotOwned()
    {
        var existingGameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "Night",
            Notes = string.Empty,
            StartDate = DateTime.UtcNow.AddDays(5),
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = [6],
            InvitedPlayerIds = []
        };
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGameNight);
        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([new Game("Root", false, GameState.Wanted) { Id = 6 }]);

        var action = async () => await _gameNightService.Update(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.SuggestedGameNotOwned);
        existingGameNight.Title.Should().Be("Night");
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldKeepAnAlreadySuggestedGame_ThatIsNoLongerOwned()
    {
        var soldGame = new Game("Root", false, GameState.ForTrade) { Id = 6 };
        var existingGameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        existingGameNight.SetSuggestedGames([soldGame]);
        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "Renamed",
            Notes = string.Empty,
            StartDate = DateTime.UtcNow.AddDays(5),
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = [6],
            InvitedPlayerIds = []
        };
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGameNight);
        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([soldGame]);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.Title.Should().Be("Renamed");
        result.SuggestedGames.Should().ContainSingle(g => g.Id == 6);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldThrowEntityNotFoundException_WhenGameNightNotFound()
    {
        var command = new UpdateGameNightCommand
        {
            Id = 999,
            Title = "Title",
            Notes = string.Empty,
            StartDate = DateTime.UtcNow,
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = []
        };

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameNight?)null);

        var action = async () => await _gameNightService.Update(command);

        await action.Should().ThrowAsync<EntityNotFoundException>();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldAddNewPlayersAndRemoveOldPlayers_WhenInvitedPlayerIdsChange()
    {
        var existingRsvp1 = GameNightRsvp.Create(1, GameNightRsvpState.Accepted);
        var existingRsvp2 = GameNightRsvp.Create(2, GameNightRsvpState.Pending);
        var existingGameNight = GameNight.Create("Title", "Notes", DateTime.UtcNow, 1, 1);
        existingGameNight.SetInvitedPlayers([existingRsvp1, existingRsvp2]);

        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "Title",
            Notes = "Notes",
            StartDate = DateTime.UtcNow,
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = [1, 3]
        };

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGameNight);

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([]);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.InvitedPlayers.Should().HaveCount(2);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 1);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 3);
        result.InvitedPlayers.Should().NotContain(p => p.PlayerId == 2);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldKeepHostAsAccepted_WhenHostNotInInvitedPlayerIds()
    {
        var hostRsvp = GameNightRsvp.Create(1, GameNightRsvpState.Accepted);
        var guestRsvp = GameNightRsvp.Create(2, GameNightRsvpState.Pending);
        var existingGameNight = GameNight.Create("Title", "Notes", DateTime.UtcNow, 1, 1);
        existingGameNight.SetInvitedPlayers([hostRsvp, guestRsvp]);

        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "Title",
            Notes = "Notes",
            StartDate = DateTime.UtcNow,
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = [2, 3]
        };

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGameNight);

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([]);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.InvitedPlayers.Should().HaveCount(3);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 1 && p.State == GameNightRsvpState.Accepted);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 2);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 3);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldAcceptForANewHost_WhoWasNotInvitedYet()
    {
        var existingGameNight = GameNight.Create("Title", "Notes", DateTime.UtcNow, 1, 1);
        existingGameNight.SetInvitedPlayers([GameNightRsvp.Create(1, GameNightRsvpState.Accepted), GameNightRsvp.Create(2, GameNightRsvpState.Pending)]);
        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "Title",
            Notes = "Notes",
            StartDate = DateTime.UtcNow,
            HostId = 5,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = [1, 2]
        };
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGameNight);
        _gameRepositoryMock.Setup(x => x.GetByIdsAsync(command.SuggestedGameIds)).ReturnsAsync([]);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.InvitedPlayers.Should().ContainSingle(p => p.PlayerId == 5).Which.State.Should().Be(GameNightRsvpState.Accepted);
        result.InvitedPlayers.Should().ContainSingle(p => p.PlayerId == 2).Which.State.Should().Be(GameNightRsvpState.Pending);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldAcceptForANewHost_WhoWasAlreadyInvited()
    {
        var existingGameNight = GameNight.Create("Title", "Notes", DateTime.UtcNow, 1, 1);
        existingGameNight.SetInvitedPlayers([GameNightRsvp.Create(1, GameNightRsvpState.Accepted), GameNightRsvp.Create(2, GameNightRsvpState.Pending)]);
        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "Title",
            Notes = "Notes",
            StartDate = DateTime.UtcNow,
            HostId = 2,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = [1, 2]
        };
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGameNight);
        _gameRepositoryMock.Setup(x => x.GetByIdsAsync(command.SuggestedGameIds)).ReturnsAsync([]);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.InvitedPlayers.Should().ContainSingle(p => p.PlayerId == 2).Which.State.Should().Be(GameNightRsvpState.Accepted);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldCallDeleteAsyncAndSaveChanges()
    {
        var gameNightId = 1;

        _gameNightRepositoryMock
            .Setup(x => x.DeleteAsync(gameNightId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _gameNightService.Delete(gameNightId);

        _gameNightRepositoryMock.Verify(x => x.DeleteAsync(gameNightId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region SendInvites Tests

    private static GameNightRsvp RsvpWithPlayer(int playerId, GameNightRsvpState state, Player player)
    {
        var rsvp = GameNightRsvp.Create(playerId, state);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.Player))!.SetValue(rsvp, player);
        return rsvp;
    }

    private static GameNightRsvp RsvpWithGameNight(
        int playerId,
        GameNightRsvpState state,
        Player player,
        int hostId,
        Player host)
    {
        var rsvp = RsvpWithPlayer(playerId, state, player);
        var gameNight = GameNight.Create("Games night", "", new DateTime(2026, 8, 13, 11, 47, 0, DateTimeKind.Utc), hostId, 1);
        typeof(GameNight).GetProperty(nameof(GameNight.Host))!.SetValue(gameNight, host);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        return rsvp;
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldThrow_WhenGameNightNotFound()
    {
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>())).ReturnsAsync((GameNight?)null);

        var act = () => _gameNightService.SendInvitesAsync(99);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldThrow_WhenEmailNotConfigured()
    {
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(false);

        var act = () => _gameNightService.SendInvitesAsync(1);

        await act.Should().ThrowAsync<DomainException>();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldRefuseWithoutStartingTheCooldown_WhenThePublicUrlIsNotConfigured()
    {
        var withEmail = RsvpWithPlayer(1, GameNightRsvpState.Pending, new Player("Alice", null, "alice@test.com"));
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers([withEmail]);
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock
            .SetupSequence(x => x.BuildRsvpUrlAsync(gameNight.LinkId))
            .ThrowsAsync(new DomainException(Constants.Errors.PublicUrlNotConfigured))
            .ReturnsAsync("https://games.example.com/rsvp");

        var first = () => _gameNightService.SendInvitesAsync(1);
        await first.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.PublicUrlNotConfigured);
        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(1);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(2));
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Exactly(2));
        _backgroundEmailSenderMock.Verify(x => x.Queue("alice@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldSendToPlayersWithEmailAndSkipOthers()
    {
        var withEmail = RsvpWithPlayer(1, GameNightRsvpState.Pending, new Player("Alice", null, "alice@test.com"));
        var noEmail = RsvpWithPlayer(2, GameNightRsvpState.Pending, new Player("Bob"));
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers([withEmail, noEmail]);

        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock.Setup(x => x.BuildRsvpUrlAsync(gameNight.LinkId)).ReturnsAsync("http://x/rsvp");

        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(1);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("alice@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldOnlyEmailPendingPlayers()
    {
        var pending = RsvpWithPlayer(1, GameNightRsvpState.Pending, new Player("Alice", null, "alice@test.com"));
        var accepted = RsvpWithPlayer(2, GameNightRsvpState.Accepted, new Player("Host", null, "host@test.com"));
        var declined = RsvpWithPlayer(3, GameNightRsvpState.Declined, new Player("Bob", null, "bob@test.com"));
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 2, 1);
        gameNight.SetInvitedPlayers([pending, accepted, declined]);

        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock.Setup(x => x.BuildRsvpUrlAsync(gameNight.LinkId)).ReturnsAsync("http://x/rsvp");

        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(1);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("alice@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("host@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _backgroundEmailSenderMock.Verify(x => x.Queue("bob@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldAllowARetry_WhenNoInviteCouldBeQueued()
    {
        var noEmail = RsvpWithPlayer(1, GameNightRsvpState.Pending, new Player("Alice"));
        var withEmail = RsvpWithPlayer(2, GameNightRsvpState.Pending, new Player("Bob", null, "bob@test.com"));
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers([noEmail]);
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock.Setup(x => x.BuildRsvpUrlAsync(gameNight.LinkId)).ReturnsAsync("http://x/rsvp");

        var failed = await _gameNightService.SendInvitesAsync(1);
        gameNight.SetInvitedPlayers([withEmail]);
        var retried = await _gameNightService.SendInvitesAsync(1);
        var again = () => _gameNightService.SendInvitesAsync(1);

        failed.Sent.Should().Be(0);
        retried.Sent.Should().Be(1);
        await again.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.InvitesCooldown);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(3));
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Exactly(3));
        _backgroundEmailSenderMock.Verify(x => x.Queue("bob@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldFetchRsvpByPlayerAndGame_WhenIdIsNull()
    {
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        var command = new UpdateRsvpCommand
        {
            Id = null,
            GameNightId = 10,
            PlayerId = 3,
            State = GameNightRsvpState.Declined
        };

        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByPlayerAndGameNightSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvp);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvp(command);

        result.Should().NotBeNull();
        result.State.Should().Be(GameNightRsvpState.Declined);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByPlayerAndGameNightSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldUpdate_WhenAnonymousLinkMatchesTheGameNight()
    {
        SetupGameNightsEnabled(true);
        var gameNight = GameNight.Create("Night", string.Empty, DateTime.UtcNow, 1, 1);
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Accepted };

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvp);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvpByLink(gameNight.LinkId, command, false);

        result.HostNotified.Should().BeFalse();
        result.Rsvp.State.Should().Be(GameNightRsvpState.Accepted);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldThrowAuthenticationFailed_WhenAnonymousAndAuthenticationRequired()
    {
        SetupGameNightsEnabled(true);
        var gameNight = GameNight.Create("Night", string.Empty, DateTime.UtcNow, 1, 1);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Accepted };

        SetupRsvpAuthentication(true);

        var action = async () => await _gameNightService.UpdateRsvpByLink(gameNight.LinkId, command, false);

        await action.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(Constants.Errors.NotAuthenticated);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<ISingleResultSpecification<GameNightRsvp>>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldNotCheckSetting_WhenAuthenticated()
    {
        SetupGameNightsEnabled(true);
        var gameNight = GameNight.Create("Night", string.Empty, DateTime.UtcNow, 1, 1);
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Declined };

        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvp);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvpByLink(gameNight.LinkId, command, true);

        result.HostNotified.Should().BeFalse();
        result.Rsvp.State.Should().Be(GameNightRsvpState.Declined);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateRsvpByLink_ShouldThrowRsvpClosed_WhenTheGameNightHasStarted(bool isAuthenticated)
    {
        SetupGameNightsEnabled(true);
        var start = new DateTime(2026, 10, 10, 19, 30, 0, DateTimeKind.Utc);
        var gameNight = GameNight.Create("Night", string.Empty, start, 1, 1);
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Accepted };

        SetupRsvpAuthentication(false);
        _dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(start.AddMinutes(1));
        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvp);

        var action = async () => await _gameNightService.UpdateRsvpByLink(gameNight.LinkId, command, isAuthenticated);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.RsvpClosed);
        rsvp.State.Should().Be(GameNightRsvpState.Pending);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), isAuthenticated ? Times.Never() : Times.Once());
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldUpdate_WhenTheGameNightStartsLater()
    {
        SetupGameNightsEnabled(true);
        var start = new DateTime(2026, 10, 10, 19, 30, 0, DateTimeKind.Utc);
        var gameNight = GameNight.Create("Night", string.Empty, start, 1, 1);
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Accepted };

        _dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(start.AddMinutes(-1));
        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvp);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvpByLink(gameNight.LinkId, command, true);

        result.HostNotified.Should().BeFalse();
        result.Rsvp.State.Should().Be(GameNightRsvpState.Accepted);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldReportTheHostNotified_WhenTheEmailWasQueued()
    {
        SetupGameNightsEnabled(true);
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Maybe };

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        var result = await _gameNightService.UpdateRsvpByLink(rsvp.GameNight!.LinkId, command, false);

        result.HostNotified.Should().BeTrue();
        result.Rsvp.State.Should().Be(GameNightRsvpState.Maybe);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("host@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldNotReportTheHostNotified_WhenTheHostHasNoEmail()
    {
        SetupGameNightsEnabled(true);
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        var result = await _gameNightService.UpdateRsvpByLink(rsvp.GameNight!.LinkId, command, false);

        result.HostNotified.Should().BeFalse();
        result.Rsvp.State.Should().Be(GameNightRsvpState.Accepted);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldNotReportTheHostNotified_WhenTheAnswerDidNotChange()
    {
        SetupGameNightsEnabled(true);
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Accepted, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);

        var result = await _gameNightService.UpdateRsvpByLink(rsvp.GameNight!.LinkId, command, false);

        result.HostNotified.Should().BeFalse();
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateRsvpByLink_ShouldThrowEntityNotFound_WhenTheLinkDoesNotMatch(bool isAuthenticated)
    {
        SetupGameNightsEnabled(true);
        var gameNight = GameNight.Create("Night", string.Empty, DateTime.UtcNow, 1, 1);
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Accepted };

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvp);

        var action = async () => await _gameNightService.UpdateRsvpByLink(Guid.NewGuid(), command, isAuthenticated);

        await action.Should().ThrowAsync<EntityNotFoundException>();
        rsvp.State.Should().Be(GameNightRsvpState.Pending);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), isAuthenticated ? Times.Never() : Times.Once());
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNotLookAtTheRsvpSetting()
    {
        var gameNight = GameNight.Create("Night", string.Empty, DateTime.UtcNow, 1, 1);
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Declined };

        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvp);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvp(command);

        result.State.Should().Be(GameNightRsvpState.Declined);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldThrow_WhenRsvpNotFound()
    {
        var command = new UpdateRsvpCommand
        {
            Id = 999,
            State = GameNightRsvpState.Accepted
        };

        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameNightRsvp?)null);

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<EntityNotFoundException>();

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldThrowArgumentNullException_WhenIdAndGameNightIdAreNull()
    {
        var command = new UpdateRsvpCommand
        {
            Id = null,
            GameNightId = null,
            PlayerId = 3,
            State = GameNightRsvpState.Accepted
        };

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<ArgumentNullException>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldThrowArgumentNullException_WhenIdAndPlayerIdAreNull()
    {
        var command = new UpdateRsvpCommand
        {
            Id = null,
            GameNightId = 10,
            PlayerId = null,
            State = GameNightRsvpState.Accepted
        };

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<ArgumentNullException>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldThrow_WhenRsvpNotFoundByPlayerAndGameNight()
    {
        var command = new UpdateRsvpCommand
        {
            Id = null,
            GameNightId = 10,
            PlayerId = 3,
            State = GameNightRsvpState.Accepted
        };

        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByPlayerAndGameNightSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameNightRsvp?)null);

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<EntityNotFoundException>();

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByPlayerAndGameNightSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldUpdateState_WhenRsvpIsFound()
    {
        var rsvp = GameNightRsvp.Create(1, GameNightRsvpState.Pending);
        var command = new UpdateRsvpCommand
        {
            Id = 7,
            State = GameNightRsvpState.Declined
        };

        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvp);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvp(command);

        result.State.Should().Be(GameNightRsvpState.Declined);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GameNightRsvpState.Pending, GameNightRsvpState.Accepted, "will come to")]
    [InlineData(GameNightRsvpState.Pending, GameNightRsvpState.Declined, "will not come to")]
    [InlineData(GameNightRsvpState.Accepted, GameNightRsvpState.Pending, "has not decided yet about")]
    [InlineData(GameNightRsvpState.Pending, GameNightRsvpState.Maybe, "might come to")]
    public async Task UpdateRsvp_ShouldWordEmailByState(GameNightRsvpState from, GameNightRsvpState state, string expected)
    {
        var rsvp = RsvpWithGameNight(2, from, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = state };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        await _gameNightService.UpdateRsvp(command);

        _backgroundEmailSenderMock.Verify(
            x => x.Queue(
                "host@test.com",
                It.Is<string>(s => s.Contains("Games night")),
                It.Is<string>(b => b.Contains("Kathleen") && b.Contains(expected))),
            Times.Once);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNeitherSaveNorEmail_WhenTheStateDoesNotChange()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Accepted, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);

        var result = await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted });

        result.State.Should().Be(GameNightRsvpState.Accepted);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldEmailTheHostOncePerCooldown_WhenAnInviteeKeepsChangingTheirAnswer()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted });
        await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Declined });
        await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted });

        rsvp.State.Should().Be(GameNightRsvpState.Accepted);
        _backgroundEmailSenderMock.Verify(x => x.Queue("host@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Exactly(3));
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(3));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNotEmail_WhenHostRespondsToOwnGameNight()
    {
        var rsvp = RsvpWithGameNight(1, GameNightRsvpState.Pending, new Player("Mikhael", null, "host@test.com"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        await _gameNightService.UpdateRsvp(command);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GameNightRsvpState.Declined)]
    [InlineData(GameNightRsvpState.Pending)]
    public async Task UpdateRsvp_ShouldThrow_WhenTheHostWouldStopAttending(GameNightRsvpState state)
    {
        var rsvp = RsvpWithGameNight(1, GameNightRsvpState.Accepted, new Player("Mikhael"), hostId: 1, host: new Player("Mikhael"));
        var command = new UpdateRsvpCommand { Id = 7, State = state };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.HostRsvpLocked);
        rsvp.State.Should().Be(GameNightRsvpState.Accepted);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldThrow_WhenTheHostDeclinesThroughThePublicLink()
    {
        SetupGameNightsEnabled(true);
        var rsvp = RsvpWithGameNight(1, GameNightRsvpState.Accepted, new Player("Mikhael"), hostId: 1, host: new Player("Mikhael"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Declined };

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);

        var action = async () => await _gameNightService.UpdateRsvpByLink(rsvp.GameNight!.LinkId, command, false);

        await action.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.HostRsvpLocked);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNotEmail_WhenHostHasNoEmail()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        await _gameNightService.UpdateRsvp(command);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNotEmail_WhenEmailIsNotConfigured()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(false);

        await _gameNightService.UpdateRsvp(command);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountFutureGameNights_ShouldReturnCountFromRepository()
    {
        SetupGameNightsEnabled(true);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2030, 1, 1, 22, 0, 0, DateTimeKind.Utc));
        _gameNightRepositoryMock
            .Setup(x => x.CountAsync(It.Is<ISpecification<GameNight>>(s => s is FutureGameNightsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var result = await _gameNightService.CountFutureGameNights();

        result.Should().Be(7);

        _gameNightRepositoryMock.Verify(x => x.CountAsync(It.Is<ISpecification<GameNight>>(s => s is FutureGameNightsSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountFutureGameNights_ShouldReturnZero_WhenGameNightsAreDisabled()
    {
        SetupGameNightsEnabled(false);

        var result = await _gameNightService.CountFutureGameNights();

        result.Should().Be(0);

        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetByLinkId Tests

    [Fact]
    public async Task GetByLinkId_ShouldReturnGameNight_WhenFound()
    {
        SetupGameNightsEnabled(true);
        var linkId = Guid.NewGuid();
        var gameNight = GameNight.Create("Night", "Notes", DateTime.UtcNow.AddDays(1), 1, 1);

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetByLinkId(linkId, true);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Night");

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByLinkId_ShouldReturnGameNight_WhenAnonymousAndAuthenticationNotRequired()
    {
        SetupGameNightsEnabled(true);
        var linkId = Guid.NewGuid();
        var gameNight = GameNight.Create("Night", "Notes", DateTime.UtcNow.AddDays(1), 1, 1);

        SetupRsvpAuthentication(false);
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetByLinkId(linkId, false);

        result.Should().BeSameAs(gameNight);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByLinkId_ShouldThrowAuthenticationFailed_WhenAnonymousAndAuthenticationRequired()
    {
        SetupGameNightsEnabled(true);
        SetupRsvpAuthentication(true);

        var action = async () => await _gameNightService.GetByLinkId(Guid.NewGuid(), false);

        await action.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(Constants.Errors.NotAuthenticated);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByLinkId_ShouldReturnNull_WhenNotFound()
    {
        SetupGameNightsEnabled(true);
        var linkId = Guid.NewGuid();

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameNight?)null);

        var result = await _gameNightService.GetByLinkId(linkId, true);

        result.Should().BeNull();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByLinkId_ShouldThrowFeatureDisabled_WhenGameNightsAreDisabled()
    {
        SetupGameNightsEnabled(false);

        var action = async () => await _gameNightService.GetByLinkId(Guid.NewGuid(), true);

        await action.Should().ThrowAsync<FeatureDisabledException>();
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldThrowFeatureDisabled_WhenGameNightsAreDisabled()
    {
        SetupGameNightsEnabled(false);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Accepted };

        var action = async () => await _gameNightService.UpdateRsvpByLink(Guid.NewGuid(), command, true);

        await action.Should().ThrowAsync<FeatureDisabledException>();
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    #endregion

    #region Suggested games

    private static readonly DateTime SuggestionNow = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    private void SetupSuggestionNight(GameNight? gameNight)
    {
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(SuggestionNow);
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);
    }

    private void VerifySuggestionNightLookup()
    {
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddSuggestedGame_ShouldAddTheGameAndSave_WhenItIsNotSuggestedYet()
    {
        var gameNight = GameNight.Create("Friday", "", SuggestionNow.AddDays(7), 1, 1);
        var game = new Game("Root", false, GameState.Owned) { Id = 9 };
        SetupSuggestionNight(gameNight);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(game);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        result.Should().BeSameAs(gameNight);
        result.SuggestedGames.Should().ContainSingle().Which.Should().BeSameAs(game);
        VerifySuggestionNightLookup();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(9), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddSuggestedGame_ShouldNotSave_WhenTheGameIsAlreadySuggested()
    {
        var game = new Game("Root", false, GameState.Owned) { Id = 9 };
        var gameNight = GameNight.Create("Friday", "", SuggestionNow.AddDays(7), 1, 1);
        gameNight.SetSuggestedGames([game]);
        SetupSuggestionNight(gameNight);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(game);

        var result = await _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        result.SuggestedGames.Should().ContainSingle();
        VerifySuggestionNightLookup();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(9), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddSuggestedGame_ShouldThrowNotFound_WhenTheGameNightDoesNotExist()
    {
        SetupSuggestionNight(null);

        var action = () => _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<EntityNotFoundException>();
        VerifySuggestionNightLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddSuggestedGame_ShouldThrowValidation_WhenTheGameNightIsOver()
    {
        SetupSuggestionNight(GameNight.Create("Last week", "", SuggestionNow.AddDays(-7), 1, 1));

        var action = () => _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.GameNightOver);
        VerifySuggestionNightLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddSuggestedGame_ShouldThrowValidation_WhenTheGameNightWasEnded()
    {
        var gameNight = GameNight.Create("Tonight", "", SuggestionNow.AddHours(-1), 1, 1);
        gameNight.End(SuggestionNow);
        SetupSuggestionNight(gameNight);

        var action = () => _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.GameNightEnded);
        VerifySuggestionNightLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddSuggestedGame_ShouldAccept_ANightThatIsStillBeingPlayed()
    {
        var gameNight = GameNight.Create("Tonight", "", SuggestionNow.AddHours(-2), 1, 1);
        var game = new Game("Root", false, GameState.Owned) { Id = 9 };
        SetupSuggestionNight(gameNight);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(game);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        result.SuggestedGames.Should().ContainSingle();
        VerifySuggestionNightLookup();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(9), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddSuggestedGame_ShouldThrowNotFound_WhenTheGameDoesNotExist()
    {
        SetupSuggestionNight(GameNight.Create("Friday", "", SuggestionNow.AddDays(7), 1, 1));
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(9)).ReturnsAsync((Game?)null);

        var action = () => _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<EntityNotFoundException>();
        VerifySuggestionNightLookup();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(9), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GameState.Wanted)]
    [InlineData(GameState.PreviouslyOwned)]
    [InlineData(GameState.ForTrade)]
    public async Task AddSuggestedGame_ShouldThrowValidation_WhenTheGameIsNotOwned(GameState state)
    {
        SetupSuggestionNight(GameNight.Create("Friday", "", SuggestionNow.AddDays(7), 1, 1));
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(new Game("Root", false, state) { Id = 9 });

        var action = () => _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.SuggestedGameNotOwned);
        VerifySuggestionNightLookup();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(9), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveSuggestedGame_ShouldRemoveTheGameAndSave_WhenItIsSuggested()
    {
        var gameNight = GameNight.Create("Friday", "", SuggestionNow.AddDays(7), 1, 1);
        gameNight.SetSuggestedGames([new Game("Root") { Id = 9 }, new Game("Everdell") { Id = 10 }]);
        SetupSuggestionNight(gameNight);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        await _gameNightService.RemoveSuggestedGame(new RemoveSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        gameNight.SuggestedGames.Should().ContainSingle().Which.Id.Should().Be(10);
        VerifySuggestionNightLookup();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveSuggestedGame_ShouldNotSave_WhenTheGameIsNotSuggested()
    {
        var gameNight = GameNight.Create("Friday", "", SuggestionNow.AddDays(7), 1, 1);
        SetupSuggestionNight(gameNight);

        await _gameNightService.RemoveSuggestedGame(new RemoveSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        gameNight.SuggestedGames.Should().BeEmpty();
        VerifySuggestionNightLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveSuggestedGame_ShouldThrowNotFound_WhenTheGameNightDoesNotExist()
    {
        SetupSuggestionNight(null);

        var action = () => _gameNightService.RemoveSuggestedGame(new RemoveSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<EntityNotFoundException>();
        VerifySuggestionNightLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveSuggestedGame_ShouldThrowValidation_WhenTheGameNightIsOver()
    {
        SetupSuggestionNight(GameNight.Create("Last week", "", SuggestionNow.AddDays(-7), 1, 1));

        var action = () => _gameNightService.RemoveSuggestedGame(new RemoveSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.GameNightOver);
        VerifySuggestionNightLookup();
        VerifyNoOtherCalls();
    }

    #endregion
}
