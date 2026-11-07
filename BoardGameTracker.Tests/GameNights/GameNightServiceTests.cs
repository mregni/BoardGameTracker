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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class GameNightServiceTests
{
    private const string InviteSubject = "You're invited: Night";
    private const string HostSubject = "RSVP update: Games night";

    private static readonly DateTime RsvpNightStart = new(2026, 8, 13, 11, 47, 0, DateTimeKind.Utc);
    private static readonly DateTime BeforeTheNight = RsvpNightStart.AddHours(-1);

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
            NullLogger<GameNightService>.Instance);
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
        _dateTimeProviderMock.VerifyNoOtherCalls();
    }

    private static TSpec NightSpec<TSpec>(int id) where TSpec : class, ISingleResultSpecification<GameNight> =>
        It.Is<TSpec>(s => s.IsSatisfiedBy(NightWithId(id)) && !s.IsSatisfiedBy(NightWithId(id + 1)));

    private static GameNightByLinkIdSpec LinkSpec(GameNight gameNight) =>
        It.Is<GameNightByLinkIdSpec>(s => s.IsSatisfiedBy(gameNight) && !s.IsSatisfiedBy(NightWithId(1)));

    private static RsvpByIdSpec RsvpSpec(int id) =>
        It.Is<RsvpByIdSpec>(s => s.IsSatisfiedBy(RsvpWithId(id)) && !s.IsSatisfiedBy(RsvpWithId(id + 1)));

    private static RsvpByPlayerAndGameNightSpec RsvpSpec(int playerId, int gameNightId) =>
        It.Is<RsvpByPlayerAndGameNightSpec>(s =>
            s.IsSatisfiedBy(RsvpOf(playerId, gameNightId))
            && !s.IsSatisfiedBy(RsvpOf(playerId + 1, gameNightId))
            && !s.IsSatisfiedBy(RsvpOf(playerId, gameNightId + 1)));

    private static PlayerIdsSpec PlayersSpec(params int[] ids) =>
        It.Is<PlayerIdsSpec>(s => ids.All(id => s.IsSatisfiedBy(new Player("Player") { Id = id })) && !s.IsSatisfiedBy(new Player("Player") { Id = 999 }));

    private static FutureGameNightsSpec FutureSpec(DateTime now) =>
        It.Is<FutureGameNightsSpec>(s =>
            s.IsSatisfiedBy(GameNight.Create("Open", "", now - GameNight.PlayWindow + TimeSpan.FromMinutes(1), 1, 1))
            && !s.IsSatisfiedBy(GameNight.Create("Over", "", now - GameNight.PlayWindow, 1, 1)));

    private static GameNight NightFrom(CreateGameNightCommand command) =>
        It.Is<GameNight>(g =>
            g.Title == command.Title
            && g.Notes == command.Notes
            && g.StartDate == command.StartDate
            && g.HostId == command.HostId
            && g.LocationId == command.LocationId);

    private static GameNight NightWithId(int id)
    {
        var gameNight = GameNight.Create("Night", "", RsvpNightStart, 1, 1);
        gameNight.Id = id;
        return gameNight;
    }

    private static GameNightRsvp RsvpWithId(int id)
    {
        var rsvp = GameNightRsvp.Create(1, GameNightRsvpState.Pending);
        rsvp.Id = id;
        return rsvp;
    }

    private static GameNightRsvp RsvpOf(int playerId, int gameNightId)
    {
        var rsvp = GameNightRsvp.Create(playerId, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNightId))!.SetValue(rsvp, gameNightId);
        return rsvp;
    }

    private static string InviteBody(string url, string title = "Night") =>
        $"<p>You're invited to <strong>{title}</strong>.</p><p><a href=\"{url}\">Respond to the invitation</a></p>";

    private static string HostBody(string player, string response) =>
        $"<p><strong>{player}</strong> {response} game night <strong>Games night</strong> on 13 August 2026 at 13:47.</p>";

    private void SetupClock(DateTime utcNow)
    {
        _dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(utcNow);
    }

    private void SetupLocalTime()
    {
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(RsvpNightStart)).Returns(RsvpNightStart.AddHours(2));
    }

    private void VerifyClockRead(int times = 1)
    {
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Exactly(times));
    }

    private void VerifyLocalTimeRead()
    {
        _dateTimeProviderMock.Verify(x => x.ConvertToLocalTime(RsvpNightStart), Times.Once);
    }

    private void VerifyNotSaved()
    {
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
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
            .Setup(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), CancellationToken.None))
            .ReturnsAsync(gameNights);

        var result = await _gameNightService.GetGameNights();

        result.Should().HaveCount(2);
        result.Should().Contain(g => g.Title == "Night 1");
        result.Should().Contain(g => g.Title == "Night 2");

        _gameNightRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), CancellationToken.None), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameNights_ShouldReturnEmptyList_WhenNoGameNightsExist()
    {
        SetupGameNightsEnabled(true);
        _gameNightRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), CancellationToken.None))
            .ReturnsAsync([]);

        var result = await _gameNightService.GetGameNights();

        result.Should().BeEmpty();

        _gameNightRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), CancellationToken.None), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(gameNightId), CancellationToken.None))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetById(gameNightId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Night 1");

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(gameNightId), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenGameNightDoesNotExist()
    {
        var gameNightId = 999;

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(gameNightId), CancellationToken.None))
            .ReturnsAsync((GameNight?)null);

        var result = await _gameNightService.GetById(gameNightId);

        result.Should().BeNull();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(gameNightId), CancellationToken.None), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightWithRunningGamesSpec>(4), CancellationToken.None))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.End(4);

        result.Should().BeSameAs(gameNight);
        gameNight.EndedAt.Should().Be(now);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightWithRunningGamesSpec>(4), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyClockRead(2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task End_ShouldBeRefused_WhileAGameIsRunning()
    {
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow.AddHours(-2), 1, 1);
        gameNight.RunningGames.Add(RunningGame.Create(4, 9, DateTime.UtcNow, [1]));
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightWithRunningGamesSpec>(4), CancellationToken.None))
            .ReturnsAsync(gameNight);

        var action = () => _gameNightService.End(4);

        var error = await action.Should().ThrowAsync<ValidationException>();
        error.Which.Message.Should().Be(Constants.Errors.GameNightGamesRunning);
        gameNight.EndedAt.Should().BeNull();
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightWithRunningGamesSpec>(4), CancellationToken.None), Times.Once);
        VerifyClockRead();
        VerifyNotSaved();
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightWithRunningGamesSpec>(4), CancellationToken.None))
            .ReturnsAsync(gameNight);

        var action = () => _gameNightService.End(4);

        if (allowed)
        {
            await action.Should().NotThrowAsync();
            gameNight.EndedAt.Should().Be(now);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
            VerifyClockRead(2);
        }
        else
        {
            var error = await action.Should().ThrowAsync<ValidationException>();
            error.Which.Message.Should().Be(Constants.Errors.GameNightNotStarted);
            gameNight.EndedAt.Should().BeNull();
            VerifyNotSaved();
            VerifyClockRead();
        }

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightWithRunningGamesSpec>(4), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task End_ShouldReturnNull_WhenTheNightDoesNotExist()
    {
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightWithRunningGamesSpec>(4), CancellationToken.None))
            .ReturnsAsync((GameNight?)null);

        var result = await _gameNightService.End(4);

        result.Should().BeNull();
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightWithRunningGamesSpec>(4), CancellationToken.None), Times.Once);
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
        VerifyClockRead();
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
        VerifyClockRead();
        VerifyNotSaved();
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightAttendanceSpec>(4), CancellationToken.None))
            .ReturnsAsync(gameNight);
    }

    private void VerifyAttendanceNightLoaded()
    {
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightAttendanceSpec>(4), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UpdateAttendance_ShouldStoreOnlyTheDifferenceFromWhoPlayed()
    {
        var start = new DateTime(2026, 10, 17, 19, 0, 0, DateTimeKind.Utc);
        var gameNight = NightWithPlay(start);
        SetupAttendanceNight(gameNight);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(start.AddHours(2));
        _playerRepositoryMock
            .Setup(x => x.ListAsync(PlayersSpec(2, 3, 5), CancellationToken.None))
            .ReturnsAsync([2, 3, 5]);

        var result = await _gameNightService.UpdateAttendance(4, new UpdateAttendanceCommand { PresentPlayerIds = [2, 3, 5, 5] });

        result.Should().BeSameAs(gameNight);
        gameNight.Attendance.Select(x => (x.PlayerId, x.Present)).Should().Equal((1, false), (5, true));
        gameNight.GetAttendeeIds().Should().Equal(2, 3, 4, 5);
        VerifyAttendanceNightLoaded();
        _playerRepositoryMock.Verify(x => x.ListAsync(PlayersSpec(2, 3, 5), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyClockRead();
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
            .Setup(x => x.ListAsync(PlayersSpec(1, 6), CancellationToken.None))
            .ReturnsAsync([1, 6]);

        await _gameNightService.UpdateAttendance(4, new UpdateAttendanceCommand { PresentPlayerIds = [1, 6] });

        gameNight.Attendance.Select(x => (x.PlayerId, x.Present)).Should().Equal((6, true));
        gameNight.GetAttendeeIds().Should().Equal(1, 2, 3, 4, 6);
        VerifyAttendanceNightLoaded();
        _playerRepositoryMock.Verify(x => x.ListAsync(PlayersSpec(1, 6), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyClockRead();
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
        VerifyClockRead();
        VerifyNotSaved();
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
            .Setup(x => x.ListAsync(PlayersSpec(1, 77), CancellationToken.None))
            .ReturnsAsync([1]);

        var action = () => _gameNightService.UpdateAttendance(4, new UpdateAttendanceCommand { PresentPlayerIds = [1, 77] });

        await action.Should().ThrowAsync<EntityNotFoundException>();
        gameNight.Attendance.Should().BeEmpty();
        VerifyAttendanceNightLoaded();
        _playerRepositoryMock.Verify(x => x.ListAsync(PlayersSpec(1, 77), CancellationToken.None), Times.Once);
        VerifyClockRead();
        VerifyNotSaved();
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightDetailSpec>(1), CancellationToken.None))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetDetail(1);

        result.Should().BeSameAs(gameNight);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightDetailSpec>(1), CancellationToken.None), Times.Once);
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
            .Setup(x => x.CreateAsync(NightFrom(command)))
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
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(NightFrom(command)), Times.Once);
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
            .Setup(x => x.CreateAsync(NightFrom(command)))
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
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(NightFrom(command)), Times.Once);
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
            .Setup(x => x.CreateAsync(NightFrom(command)))
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
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(NightFrom(command)), Times.Once);
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
            .Setup(x => x.CreateAsync(NightFrom(command)))
            .ReturnsAsync((GameNight g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Create(command);

        result.SuggestedGames.Should().BeEmpty();
        result.InvitedPlayers.Should().ContainSingle(p => p.PlayerId == 1 && p.State == GameNightRsvpState.Accepted);

        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(It.Is<List<int>>(ids => ids.Count == 0)), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(NightFrom(command)), Times.Once);
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
        VerifyNotSaved();
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None))
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
        result.StartDate.Should().Be(command.StartDate);
        result.SuggestedGames.Should().HaveCount(1);
        result.SuggestedGames.Should().Contain(g => g.Id == 7);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None))
            .ReturnsAsync(existingGameNight);
        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([new Game("Root", false, GameState.Wanted) { Id = 6 }]);

        var action = async () => await _gameNightService.Update(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.SuggestedGameNotOwned);
        existingGameNight.Title.Should().Be("Night");
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        VerifyNotSaved();
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None))
            .ReturnsAsync(existingGameNight);
        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([soldGame]);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.Title.Should().Be("Renamed");
        result.SuggestedGames.Should().ContainSingle(g => g.Id == 6);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None))
            .ReturnsAsync((GameNight?)null);

        var action = async () => await _gameNightService.Update(command);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(GameNight));
        error.Which.EntityId.Should().Be(999);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None), Times.Once);
        VerifyNotSaved();
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None))
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

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None))
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

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None))
            .ReturnsAsync(existingGameNight);
        _gameRepositoryMock.Setup(x => x.GetByIdsAsync(command.SuggestedGameIds)).ReturnsAsync([]);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.InvitedPlayers.Should().ContainSingle(p => p.PlayerId == 5).Which.State.Should().Be(GameNightRsvpState.Accepted);
        result.InvitedPlayers.Should().ContainSingle(p => p.PlayerId == 2).Which.State.Should().Be(GameNightRsvpState.Pending);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None))
            .ReturnsAsync(existingGameNight);
        _gameRepositoryMock.Setup(x => x.GetByIdsAsync(command.SuggestedGameIds)).ReturnsAsync([]);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.InvitedPlayers.Should().ContainSingle(p => p.PlayerId == 2).Which.State.Should().Be(GameNightRsvpState.Accepted);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(command.Id), CancellationToken.None), Times.Once);
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
        var gameNight = GameNight.Create("Games night", "", RsvpNightStart, hostId, 1);
        typeof(GameNight).GetProperty(nameof(GameNight.Host))!.SetValue(gameNight, host);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        return rsvp;
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldThrow_WhenGameNightNotFound()
    {
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(99), CancellationToken.None)).ReturnsAsync((GameNight?)null);

        var act = () => _gameNightService.SendInvitesAsync(99);

        var error = await act.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityId.Should().Be(99);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(99), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldThrow_WhenEmailNotConfigured()
    {
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None)).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(false);

        var act = () => _gameNightService.SendInvitesAsync(1);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.EmailNotConfigured);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldRefuseWithoutStartingTheCooldown_WhenThePublicUrlIsNotConfigured()
    {
        var withEmail = RsvpWithPlayer(1, GameNightRsvpState.Pending, new Player("Alice", null, "alice@test.com"));
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers([withEmail]);
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None)).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock
            .SetupSequence(x => x.BuildRsvpUrlAsync(gameNight.LinkId))
            .ThrowsAsync(new DomainException(Constants.Errors.PublicUrlNotConfigured))
            .ReturnsAsync("https://games.example.com/rsvp");

        var first = () => _gameNightService.SendInvitesAsync(1);
        await first.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.PublicUrlNotConfigured);
        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(1);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Exactly(2));
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(2));
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Exactly(2));
        _backgroundEmailSenderMock.Verify(x => x.Queue("alice@test.com", InviteSubject, InviteBody("https://games.example.com/rsvp")), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldSendToPlayersWithEmailAndSkipOthers()
    {
        var withEmail = RsvpWithPlayer(1, GameNightRsvpState.Pending, new Player("Alice", null, "alice@test.com"));
        var noEmail = RsvpWithPlayer(2, GameNightRsvpState.Pending, new Player("Bob"));
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers([withEmail, noEmail]);

        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None)).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock.Setup(x => x.BuildRsvpUrlAsync(gameNight.LinkId)).ReturnsAsync("http://x/rsvp");

        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(1);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("alice@test.com", InviteSubject, InviteBody("http://x/rsvp")), Times.Once);
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

        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None)).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock.Setup(x => x.BuildRsvpUrlAsync(gameNight.LinkId)).ReturnsAsync("http://x/rsvp");

        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(1);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("alice@test.com", InviteSubject, InviteBody("http://x/rsvp")), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldAllowARetry_WhenNoInviteCouldBeQueued()
    {
        var noEmail = RsvpWithPlayer(1, GameNightRsvpState.Pending, new Player("Alice"));
        var withEmail = RsvpWithPlayer(2, GameNightRsvpState.Pending, new Player("Bob", null, "bob@test.com"));
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers([noEmail]);
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None)).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock.Setup(x => x.BuildRsvpUrlAsync(gameNight.LinkId)).ReturnsAsync("http://x/rsvp");

        var failed = await _gameNightService.SendInvitesAsync(1);
        gameNight.SetInvitedPlayers([withEmail]);
        var retried = await _gameNightService.SendInvitesAsync(1);
        var again = () => _gameNightService.SendInvitesAsync(1);

        failed.Sent.Should().Be(0);
        retried.Sent.Should().Be(1);
        await again.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.InvitesCooldown);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Exactly(3));
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(3));
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Exactly(3));
        _backgroundEmailSenderMock.Verify(x => x.Queue("bob@test.com", InviteSubject, InviteBody("http://x/rsvp")), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(RsvpSpec(3, 10), CancellationToken.None))
            .ReturnsAsync(rsvp);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvp(command);

        result.Should().NotBeNull();
        result.State.Should().Be(GameNightRsvpState.Declined);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(3, 10), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldUpdate_WhenAnonymousLinkMatchesTheGameNight()
    {
        SetupGameNightsEnabled(true);
        SetupClock(BeforeTheNight);
        var gameNight = GameNight.Create("Night", string.Empty, DateTime.UtcNow, 1, 1);
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Accepted };

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None))
            .ReturnsAsync(rsvp);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvpByLink(gameNight.LinkId, command, false);

        result.HostNotified.Should().BeFalse();
        result.Rsvp.State.Should().Be(GameNightRsvpState.Accepted);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyClockRead();
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
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldNotCheckSetting_WhenAuthenticated()
    {
        SetupGameNightsEnabled(true);
        SetupClock(BeforeTheNight);
        var gameNight = GameNight.Create("Night", string.Empty, DateTime.UtcNow, 1, 1);
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Declined };

        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None))
            .ReturnsAsync(rsvp);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvpByLink(gameNight.LinkId, command, true);

        result.HostNotified.Should().BeFalse();
        result.Rsvp.State.Should().Be(GameNightRsvpState.Declined);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyClockRead();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, false, 12)]
    [InlineData(true, false, 12)]
    [InlineData(false, true, 1)]
    [InlineData(true, true, 1)]
    public async Task UpdateRsvpByLink_ShouldThrowRsvpClosed_WhenTheGameNightIsOver(bool isAuthenticated, bool ended, int hoursSinceStart)
    {
        SetupGameNightsEnabled(true);
        var start = new DateTime(2026, 10, 10, 19, 30, 0, DateTimeKind.Utc);
        var gameNight = GameNight.Create("Night", string.Empty, start, 1, 1);
        if (ended)
        {
            gameNight.End(start.AddMinutes(30));
        }

        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Accepted };

        SetupRsvpAuthentication(false);
        _dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(start.AddHours(hoursSinceStart));
        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None))
            .ReturnsAsync(rsvp);

        var action = async () => await _gameNightService.UpdateRsvpByLink(gameNight.LinkId, command, isAuthenticated);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.RsvpClosed);
        rsvp.State.Should().Be(GameNightRsvpState.Pending);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), isAuthenticated ? Times.Never() : Times.Once());
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldUpdate_WhileTheGameNightIsInProgress()
    {
        SetupGameNightsEnabled(true);
        var start = new DateTime(2026, 10, 10, 19, 30, 0, DateTimeKind.Utc);
        var gameNight = GameNight.Create("Night", string.Empty, start, 1, 1);
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        var command = new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Accepted };

        SetupRsvpAuthentication(false);
        _dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(start.AddHours(3));
        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None))
            .ReturnsAsync(rsvp);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvpByLink(gameNight.LinkId, command, false);

        result.Rsvp.State.Should().Be(GameNightRsvpState.Accepted);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None))
            .ReturnsAsync(rsvp);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvpByLink(gameNight.LinkId, command, true);

        result.HostNotified.Should().BeFalse();
        result.Rsvp.State.Should().Be(GameNightRsvpState.Accepted);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None), Times.Once);
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
        SetupClock(BeforeTheNight);
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Maybe };

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        SetupLocalTime();

        var result = await _gameNightService.UpdateRsvpByLink(rsvp.GameNight!.LinkId, command, false);

        result.HostNotified.Should().BeTrue();
        result.Rsvp.State.Should().Be(GameNightRsvpState.Maybe);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("host@test.com", HostSubject, HostBody("Kathleen", "might come to")), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyClockRead();
        VerifyLocalTimeRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldNotReportTheHostNotified_WhenTheCooldownSkippedTheEmail()
    {
        SetupGameNightsEnabled(true);
        SetupClock(BeforeTheNight);
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var linkId = rsvp.GameNight!.LinkId;

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        SetupLocalTime();

        var first = await _gameNightService.UpdateRsvpByLink(linkId, new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Maybe }, false);
        var second = await _gameNightService.UpdateRsvpByLink(linkId, new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Declined }, false);

        first.HostNotified.Should().BeTrue();
        second.HostNotified.Should().BeFalse();
        second.Rsvp.State.Should().Be(GameNightRsvpState.Declined);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Exactly(2));
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Exactly(2));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Exactly(2));
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(2));
        _backgroundEmailSenderMock.Verify(x => x.Queue("host@test.com", HostSubject, HostBody("Kathleen", "might come to")), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true), Times.Exactly(2));
        VerifyClockRead(2);
        VerifyLocalTimeRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldNotReportTheHostNotified_WhenTheHostHasNoEmail()
    {
        SetupGameNightsEnabled(true);
        SetupClock(BeforeTheNight);
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        var result = await _gameNightService.UpdateRsvpByLink(rsvp.GameNight!.LinkId, command, false);

        result.HostNotified.Should().BeFalse();
        result.Rsvp.State.Should().Be(GameNightRsvpState.Accepted);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyClockRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldNotReportTheHostNotified_WhenTheAnswerDidNotChange()
    {
        SetupGameNightsEnabled(true);
        SetupClock(BeforeTheNight);
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Accepted, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);

        var result = await _gameNightService.UpdateRsvpByLink(rsvp.GameNight!.LinkId, command, false);

        result.HostNotified.Should().BeFalse();
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyClockRead();
        VerifyNotSaved();
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
            .Setup(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None))
            .ReturnsAsync(rsvp);

        var action = async () => await _gameNightService.UpdateRsvpByLink(Guid.NewGuid(), command, isAuthenticated);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(GameNightRsvp));
        rsvp.State.Should().Be(GameNightRsvpState.Pending);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None))
            .ReturnsAsync(rsvp);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvp(command);

        result.State.Should().Be(GameNightRsvpState.Declined);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(RsvpSpec(999), CancellationToken.None))
            .ReturnsAsync((GameNightRsvp?)null);

        var action = async () => await _gameNightService.UpdateRsvp(command);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityId.Should().Be(999);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(999), CancellationToken.None), Times.Once);
        VerifyNotSaved();
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
            .Setup(x => x.SingleOrDefaultAsync(RsvpSpec(3, 10), CancellationToken.None))
            .ReturnsAsync((GameNightRsvp?)null);

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<EntityNotFoundException>();

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(3, 10), CancellationToken.None), Times.Once);
        VerifyNotSaved();
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
            .Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None))
            .ReturnsAsync(rsvp);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvp(command);

        result.State.Should().Be(GameNightRsvpState.Declined);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
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

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        SetupLocalTime();

        await _gameNightService.UpdateRsvp(command);

        _backgroundEmailSenderMock.Verify(x => x.Queue("host@test.com", HostSubject, HostBody("Kathleen", expected)), Times.Once);
        VerifyLocalTimeRead();

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNeitherSaveNorEmail_WhenTheStateDoesNotChange()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Accepted, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);

        var result = await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted });

        result.State.Should().Be(GameNightRsvpState.Accepted);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldEmailTheHostOncePerCooldown_WhenAnInviteeKeepsChangingTheirAnswer()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        SetupLocalTime();

        await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted });
        await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Declined });
        await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted });

        rsvp.State.Should().Be(GameNightRsvpState.Accepted);
        _backgroundEmailSenderMock.Verify(x => x.Queue("host@test.com", HostSubject, HostBody("Kathleen", "will come to")), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Exactly(3));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Exactly(3));
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(3));
        VerifyLocalTimeRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNotEmail_WhenHostRespondsToOwnGameNight()
    {
        var rsvp = RsvpWithGameNight(1, GameNightRsvpState.Pending, new Player("Mikhael", null, "host@test.com"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        await _gameNightService.UpdateRsvp(command);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
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

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.HostRsvpLocked);
        rsvp.State.Should().Be(GameNightRsvpState.Accepted);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldThrow_WhenTheHostDeclinesThroughThePublicLink()
    {
        SetupGameNightsEnabled(true);
        SetupClock(BeforeTheNight);
        var rsvp = RsvpWithGameNight(1, GameNightRsvpState.Accepted, new Player("Mikhael"), hostId: 1, host: new Player("Mikhael"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Declined };

        SetupRsvpAuthentication(false);
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);

        var action = async () => await _gameNightService.UpdateRsvpByLink(rsvp.GameNight!.LinkId, command, false);

        await action.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.HostRsvpLocked);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyClockRead();
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNotEmail_WhenHostHasNoEmail()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        await _gameNightService.UpdateRsvp(command);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
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

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(false);

        await _gameNightService.UpdateRsvp(command);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountFutureGameNights_ShouldReturnCountFromRepository()
    {
        SetupGameNightsEnabled(true);
        var now = new DateTime(2030, 1, 1, 22, 0, 0, DateTimeKind.Utc);
        var token = new CancellationTokenSource().Token;
        SetupClock(now);
        _gameNightRepositoryMock
            .Setup(x => x.CountAsync(FutureSpec(now), token))
            .ReturnsAsync(7);

        var result = await _gameNightService.CountFutureGameNights(token);

        result.Should().Be(7);

        _gameNightRepositoryMock.Verify(x => x.CountAsync(FutureSpec(now), token), Times.Once);
        VerifyClockRead();
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
        var gameNight = GameNight.Create("Night", "Notes", DateTime.UtcNow.AddDays(1), 1, 1);
        var linkId = gameNight.LinkId;

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(LinkSpec(gameNight), CancellationToken.None))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetByLinkId(linkId, true);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Night");

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(LinkSpec(gameNight), CancellationToken.None), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByLinkId_ShouldReturnGameNight_WhenAnonymousAndAuthenticationNotRequired()
    {
        SetupGameNightsEnabled(true);
        var gameNight = GameNight.Create("Night", "Notes", DateTime.UtcNow.AddDays(1), 1, 1);
        var linkId = gameNight.LinkId;

        SetupRsvpAuthentication(false);
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(LinkSpec(gameNight), CancellationToken.None))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetByLinkId(linkId, false);

        result.Should().BeSameAs(gameNight);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(LinkSpec(gameNight), CancellationToken.None), Times.Once);
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
        var gameNight = GameNight.Create("Night", "Notes", DateTime.UtcNow.AddDays(1), 1, 1);
        var linkId = gameNight.LinkId;

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(LinkSpec(gameNight), CancellationToken.None))
            .ReturnsAsync((GameNight?)null);

        var result = await _gameNightService.GetByLinkId(linkId, true);

        result.Should().BeNull();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(LinkSpec(gameNight), CancellationToken.None), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(3), CancellationToken.None))
            .ReturnsAsync(gameNight);
    }

    private void VerifySuggestionNightLookup(bool clockRead = true)
    {
        VerifyClockRead(clockRead ? 1 : 0);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(3), CancellationToken.None), Times.Once);
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
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddSuggestedGame_ShouldThrowNotFound_WhenTheGameNightDoesNotExist()
    {
        SetupSuggestionNight(null);

        var action = () => _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<EntityNotFoundException>();
        VerifySuggestionNightLookup(false);
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddSuggestedGame_ShouldThrowValidation_WhenTheGameNightIsOver()
    {
        SetupSuggestionNight(GameNight.Create("Last week", "", SuggestionNow.AddDays(-7), 1, 1));

        var action = () => _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.GameNightOver);
        VerifySuggestionNightLookup();
        VerifyNotSaved();
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
        VerifySuggestionNightLookup(false);
        VerifyNotSaved();
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
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GameState.Wanted)]
    [InlineData(GameState.PreviouslyOwned)]
    [InlineData(GameState.ForTrade)]
    [InlineData(GameState.Sold)]
    public async Task AddSuggestedGame_ShouldThrowValidation_WhenTheGameIsNotOwned(GameState state)
    {
        SetupSuggestionNight(GameNight.Create("Friday", "", SuggestionNow.AddDays(7), 1, 1));
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(new Game("Root", false, state) { Id = 9 });

        var action = () => _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.SuggestedGameNotOwned);
        VerifySuggestionNightLookup();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(9), Times.Once);
        VerifyNotSaved();
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
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveSuggestedGame_ShouldThrowNotFound_WhenTheGameNightDoesNotExist()
    {
        SetupSuggestionNight(null);

        var action = () => _gameNightService.RemoveSuggestedGame(new RemoveSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<EntityNotFoundException>();
        VerifySuggestionNightLookup(false);
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveSuggestedGame_ShouldThrowValidation_WhenTheGameNightIsOver()
    {
        SetupSuggestionNight(GameNight.Create("Last week", "", SuggestionNow.AddDays(-7), 1, 1));

        var action = () => _gameNightService.RemoveSuggestedGame(new RemoveSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.GameNightOver);
        VerifySuggestionNightLookup();
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    #endregion

    #region Boundaries

    [Fact]
    public async Task Reopen_ShouldBeAllowed_JustBeforeThePlayWindowCloses()
    {
        var start = new DateTime(2026, 10, 2, 19, 0, 0, DateTimeKind.Utc);
        var gameNight = GameNight.Create("Night", "", start, 1, 1);
        gameNight.End(start.AddHours(4));
        SetupClock(start + GameNight.PlayWindow - TimeSpan.FromSeconds(1));
        _gameNightRepositoryMock.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(gameNight);

        var result = await _gameNightService.Reopen(4);

        result!.EndedAt.Should().BeNull();
        _gameNightRepositoryMock.Verify(x => x.GetByIdAsync(4), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyClockRead();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task AddSuggestedGame_ShouldStayOpen_UntilThePlayWindowCloses(int secondsLeft, bool open)
    {
        var start = SuggestionNow - GameNight.PlayWindow + TimeSpan.FromSeconds(secondsLeft);
        var gameNight = GameNight.Create("Tonight", "", start, 1, 1);
        var game = new Game("Root", false, GameState.Owned) { Id = 9 };
        SetupSuggestionNight(gameNight);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(game);

        var action = () => _gameNightService.AddSuggestedGame(new AddSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        if (open)
        {
            await action.Should().NotThrowAsync();
            gameNight.SuggestedGames.Should().ContainSingle();
            _gameRepositoryMock.Verify(x => x.GetByIdAsync(9), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }
        else
        {
            await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.GameNightOver);
            VerifyNotSaved();
        }

        VerifySuggestionNightLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveSuggestedGame_ShouldThrowValidation_WhenTheGameNightWasEnded()
    {
        var gameNight = GameNight.Create("Tonight", "", SuggestionNow.AddHours(-1), 1, 1);
        gameNight.SetSuggestedGames([new Game("Root") { Id = 9 }]);
        gameNight.End(SuggestionNow);
        SetupSuggestionNight(gameNight);

        var action = () => _gameNightService.RemoveSuggestedGame(new RemoveSuggestedGameCommand { GameNightId = 3, GameId = 9 });

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.GameNightEnded);
        gameNight.SuggestedGames.Should().ContainSingle();
        VerifySuggestionNightLookup(false);
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldStayOpen_JustBeforeThePlayWindowCloses()
    {
        SetupGameNightsEnabled(true);
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"), hostId: 1, host: new Player("Mikhael"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };
        SetupClock(RsvpNightStart + GameNight.PlayWindow - TimeSpan.FromSeconds(1));
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);

        var result = await _gameNightService.UpdateRsvpByLink(rsvp.GameNight!.LinkId, command, true);

        result.Rsvp.State.Should().Be(GameNightRsvpState.Accepted);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyClockRead();
        VerifyNoOtherCalls();
    }

    #endregion

    #region Rsvp details

    [Fact]
    public async Task UpdateRsvpByLink_ShouldThrowEntityNotFound_WhenTheRsvpHasNoGameNight()
    {
        SetupGameNightsEnabled(true);
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        rsvp.Id = 5;
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None)).ReturnsAsync(rsvp);

        var action = () => _gameNightService.UpdateRsvpByLink(Guid.NewGuid(), new UpdateRsvpCommand { Id = 5, State = GameNightRsvpState.Accepted }, true);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityId.Should().Be(5);
        rsvp.State.Should().Be(GameNightRsvpState.Pending);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(5), CancellationToken.None), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvpByLink_ShouldFindTheRsvpByPlayerAndGameNight_WhenThereIsNoRsvpId()
    {
        SetupGameNightsEnabled(true);
        SetupClock(BeforeTheNight);
        var rsvp = RsvpWithGameNight(3, GameNightRsvpState.Pending, new Player("Kathleen"), hostId: 1, host: new Player("Mikhael"));
        var command = new UpdateRsvpCommand { GameNightId = 10, PlayerId = 3, State = GameNightRsvpState.Accepted };
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(3, 10), CancellationToken.None)).ReturnsAsync(rsvp);

        var result = await _gameNightService.UpdateRsvpByLink(rsvp.GameNight!.LinkId, command, true);

        result.Rsvp.Should().BeSameAs(rsvp);
        result.Rsvp.State.Should().Be(GameNightRsvpState.Accepted);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(3, 10), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyClockRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldLetTheHostConfirm_WhenTheyAlreadyAccepted()
    {
        var rsvp = RsvpWithGameNight(1, GameNightRsvpState.Accepted, new Player("Mikhael"), hostId: 1, host: new Player("Mikhael"));
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);

        var result = await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted });

        result.State.Should().Be(GameNightRsvpState.Accepted);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        VerifyNotSaved();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNameThePlayerById_WhenThePlayerIsNotLoaded()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, null!, hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        SetupLocalTime();

        await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted });

        _backgroundEmailSenderMock.Verify(x => x.Queue("host@test.com", HostSubject, HostBody("2", "will come to")), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyLocalTimeRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldEncodeThePlayerName_InTheEmailBody()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("<b>Kat</b>"), hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        SetupLocalTime();

        await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted });

        _backgroundEmailSenderMock.Verify(x => x.Queue("host@test.com", HostSubject, HostBody("&lt;b&gt;Kat&lt;/b&gt;", "will come to")), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyLocalTimeRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNotEmail_WhenTheHostEmailIsBlank()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"), hostId: 1, host: new Player("Mikhael", null, "   "));
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(rsvp);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted });

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldKeepASeparateCooldown_PerInvitee()
    {
        var host = new Player("Mikhael", null, "host@test.com");
        var first = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"), hostId: 1, host: host);
        first.Id = 7;
        var second = RsvpWithGameNight(3, GameNightRsvpState.Pending, new Player("Bram"), hostId: 1, host: host);
        second.Id = 8;
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None)).ReturnsAsync(first);
        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(RsvpSpec(8), CancellationToken.None)).ReturnsAsync(second);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        SetupLocalTime();

        await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted });
        await _gameNightService.UpdateRsvp(new UpdateRsvpCommand { Id = 8, State = GameNightRsvpState.Accepted });

        _backgroundEmailSenderMock.Verify(x => x.Queue("host@test.com", HostSubject, HostBody("Kathleen", "will come to")), Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("host@test.com", HostSubject, HostBody("Bram", "will come to")), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(7), CancellationToken.None), Times.Once);
        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(RsvpSpec(8), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Exactly(2));
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(2));
        _dateTimeProviderMock.Verify(x => x.ConvertToLocalTime(RsvpNightStart), Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    #endregion

    #region Invite details

    private void SetupInviteNight(int id, GameNight gameNight, string url = "http://x/rsvp", CancellationToken token = default)
    {
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(id), token)).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock.Setup(x => x.BuildRsvpUrlAsync(gameNight.LinkId)).ReturnsAsync(url);
    }

    private static List<GameNightRsvp> PendingWithEmail(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => RsvpWithPlayer(i, GameNightRsvpState.Pending, new Player($"Player {i}", null, $"p{i}@test.com")))
            .ToList();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldForwardTheCancellationToken()
    {
        var token = new CancellationTokenSource().Token;
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers(PendingWithEmail(1));
        SetupInviteNight(1, gameNight, token: token);

        var result = await _gameNightService.SendInvitesAsync(1, token);

        result.Sent.Should().Be(1);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), token), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        _backgroundEmailSenderMock.Verify(x => x.Queue("p1@test.com", InviteSubject, InviteBody("http://x/rsvp")), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldEncodeTheTitleAndUrl_InTheBodyOnly()
    {
        var gameNight = GameNight.Create("<Night>", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers(PendingWithEmail(1));
        SetupInviteNight(1, gameNight, "http://x/rsvp?a=1&b=2");

        await _gameNightService.SendInvitesAsync(1);

        _backgroundEmailSenderMock.Verify(
            x => x.Queue("p1@test.com", "You're invited: <Night>", InviteBody("http://x/rsvp?a=1&amp;b=2", "&lt;Night&gt;")),
            Times.Once);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldSkipInviteesWithoutALoadedPlayer()
    {
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers([GameNightRsvp.Create(1, GameNightRsvpState.Pending)]);
        SetupInviteNight(1, gameNight);

        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(0);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldStopAtFiftyRecipients()
    {
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers(PendingWithEmail(51));
        SetupInviteNight(1, gameNight);

        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(50);
        _backgroundEmailSenderMock.Verify(x => x.Queue(It.IsNotIn("p51@test.com"), InviteSubject, InviteBody("http://x/rsvp")), Times.Exactly(50));
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact(Skip = "BUG: the recipient cap counts invitees without an email address")]
    public async Task SendInvitesAsync_ShouldNotCountInviteesWithoutEmail_TowardsTheCap()
    {
        var noEmail = Enumerable.Range(100, 50).Select(i => RsvpWithPlayer(i, GameNightRsvpState.Pending, new Player($"Player {i}")));
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers(noEmail.Concat(PendingWithEmail(1)));
        SetupInviteNight(1, gameNight);

        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(1);
        _backgroundEmailSenderMock.Verify(x => x.Queue("p1@test.com", InviteSubject, InviteBody("http://x/rsvp")), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldKeepASeparateCooldown_PerGameNight()
    {
        var first = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        first.SetInvitedPlayers(PendingWithEmail(1));
        var second = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        second.SetInvitedPlayers(PendingWithEmail(1));
        SetupInviteNight(1, first);
        SetupInviteNight(2, second);

        await _gameNightService.SendInvitesAsync(1);
        var result = await _gameNightService.SendInvitesAsync(2);

        result.Sent.Should().Be(1);
        _backgroundEmailSenderMock.Verify(x => x.Queue("p1@test.com", InviteSubject, InviteBody("http://x/rsvp")), Times.Exactly(2));
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(2), CancellationToken.None), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Exactly(2));
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(first.LinkId), Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(second.LinkId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Update details

    [Fact]
    public async Task Update_ShouldTreatNullCollectionsAsEmpty_AndKeepOnlyTheHost()
    {
        var existingGameNight = GameNight.Create("Title", "Notes", DateTime.UtcNow, 1, 1);
        existingGameNight.SetSuggestedGames([new Game("Root") { Id = 9 }]);
        existingGameNight.SetInvitedPlayers([GameNightRsvp.Create(1, GameNightRsvpState.Accepted), GameNightRsvp.Create(2, GameNightRsvpState.Pending)]);
        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "Title",
            Notes = "Notes",
            StartDate = DateTime.UtcNow,
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = null!,
            InvitedPlayerIds = null!
        };
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None))
            .ReturnsAsync(existingGameNight);
        _gameRepositoryMock.Setup(x => x.GetByIdsAsync(It.Is<List<int>>(ids => ids.Count == 0))).ReturnsAsync([]);

        var result = await _gameNightService.Update(command);

        result.SuggestedGames.Should().BeEmpty();
        result.InvitedPlayers.Should().ContainSingle().Which.PlayerId.Should().Be(1);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(It.Is<List<int>>(ids => ids.Count == 0)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldKeepTheAnswerOfGuestsWhoStayInvited()
    {
        var existingGameNight = GameNight.Create("Title", "Notes", DateTime.UtcNow, 1, 1);
        existingGameNight.SetInvitedPlayers([GameNightRsvp.Create(1, GameNightRsvpState.Accepted), GameNightRsvp.Create(2, GameNightRsvpState.Declined)]);
        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "Title",
            Notes = "Notes",
            StartDate = DateTime.UtcNow,
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = [2]
        };
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None))
            .ReturnsAsync(existingGameNight);
        _gameRepositoryMock.Setup(x => x.GetByIdsAsync(command.SuggestedGameIds)).ReturnsAsync([]);

        var result = await _gameNightService.Update(command);

        result.InvitedPlayers.Should().ContainSingle(p => p.PlayerId == 2).Which.State.Should().Be(GameNightRsvpState.Declined);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(NightSpec<GameNightByIdWithDetailsSpec>(1), CancellationToken.None), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
