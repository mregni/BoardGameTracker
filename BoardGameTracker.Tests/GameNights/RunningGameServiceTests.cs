using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class RunningGameServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 22, 42, 20, DateTimeKind.Utc);

    private readonly Mock<IRepository<RunningGame>> _runningGameRepositoryMock;
    private readonly Mock<IReadRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IGameService> _gameServiceMock;
    private readonly Mock<IPlayerService> _playerServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly RunningGameService _service;

    public RunningGameServiceTests()
    {
        _runningGameRepositoryMock = new Mock<IRepository<RunningGame>>();
        _gameNightRepositoryMock = new Mock<IReadRepository<GameNight>>();
        _gameServiceMock = new Mock<IGameService>();
        _playerServiceMock = new Mock<IPlayerService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);

        _service = new RunningGameService(
            _runningGameRepositoryMock.Object,
            _gameNightRepositoryMock.Object,
            _gameServiceMock.Object,
            _playerServiceMock.Object,
            _sessionServiceMock.Object,
            _unitOfWorkMock.Object,
            _dateTimeProviderMock.Object,
            new Mock<ILogger<RunningGameService>>().Object);
    }

    private void VerifyNoOtherCalls()
    {
        _runningGameRepositoryMock.VerifyNoOtherCalls();
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _gameServiceMock.VerifyNoOtherCalls();
        _playerServiceMock.VerifyNoOtherCalls();
        _sessionServiceMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    private void SetupGameNight(GameNight? gameNight)
    {
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<GameNightByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);
    }

    private static GameNight CreateNight() => GameNight.Create("Night", "", Now.AddHours(-2), 1, 5);

    private static RunningGame CreateRunningGame(DateTime start)
    {
        var runningGame = RunningGame.Create(3, 9, start, [1, 2]);
        runningGame.Id = 12;
        var gameNight = GameNight.Create("Night", "", start, 1, 5);
        typeof(RunningGame).GetProperty(nameof(RunningGame.GameNight))!.SetValue(runningGame, gameNight);
        return runningGame;
    }

    [Fact]
    public async Task GetById_ShouldReturnTheRunningGame()
    {
        var runningGame = CreateRunningGame(Now);
        _runningGameRepositoryMock.Setup(x => x.GetByIdAsync(12)).ReturnsAsync(runningGame);

        var result = await _service.GetById(12);

        result.Should().BeSameAs(runningGame);
        _runningGameRepositoryMock.Verify(x => x.GetByIdAsync(12), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Start_ShouldCreateARunningGame_StartingNow()
    {
        SetupGameNight(CreateNight());
        _gameServiceMock.Setup(x => x.ExistsAsync(9)).ReturnsAsync(true);
        _playerServiceMock
            .Setup(x => x.GetExistingIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync((IEnumerable<int> ids) => ids.ToList());
        _runningGameRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<RunningGame>()))
            .ReturnsAsync((RunningGame r) => r);

        var result = await _service.Start(3, new StartRunningGameCommand { GameId = 9, PlayerIds = [1, 2, 2] });

        result.GameNightId.Should().Be(3);
        result.GameId.Should().Be(9);
        result.Start.Should().Be(Now);
        result.GetPlayerIds().Should().Equal(1, 2);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _gameServiceMock.Verify(x => x.ExistsAsync(9), Times.Once);
        _playerServiceMock.Verify(x => x.GetExistingIdsAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 1, 2 }))), Times.Once);
        _runningGameRepositoryMock.Verify(x => x.CreateAsync(result), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Start_ShouldThrowEntityNotFound_WhenTheGameNightDoesNotExist()
    {
        SetupGameNight(null);

        var act = () => _service.Start(3, new StartRunningGameCommand { GameId = 9, PlayerIds = [1] });

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Start_ShouldBeRefused_WhenTheNightHasEnded()
    {
        var gameNight = CreateNight();
        gameNight.End(Now.AddMinutes(-5));
        SetupGameNight(gameNight);

        var act = () => _service.Start(3, new StartRunningGameCommand { GameId = 9, PlayerIds = [1] });

        var error = await act.Should().ThrowAsync<ValidationException>();
        error.Which.Message.Should().Be(Constants.Errors.GameNightEnded);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Start_ShouldBeRefused_WhenTheNightIsMoreThanTwelveHoursAway()
    {
        SetupGameNight(GameNight.Create("Night", "", Now.AddDays(7), 1, 5));

        var act = () => _service.Start(3, new StartRunningGameCommand { GameId = 9, PlayerIds = [1] });

        var error = await act.Should().ThrowAsync<ValidationException>();
        error.Which.Message.Should().Be(Constants.Errors.GameNightNotStarted);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Finish_ShouldBeRefused_WhenTheNightIsMoreThanTwelveHoursAway()
    {
        var runningGame = RunningGame.Create(3, 9, Now, [1]);
        runningGame.Id = 12;
        typeof(RunningGame).GetProperty(nameof(RunningGame.GameNight))!.SetValue(runningGame, GameNight.Create("Night", "", Now.AddHours(13), 1, 5));
        _runningGameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(runningGame);

        var act = () => _service.Finish(12, new FinishRunningGameCommand { PlayerSessions = [] });

        var error = await act.Should().ThrowAsync<ValidationException>();
        error.Which.Message.Should().Be(Constants.Errors.GameNightNotStarted);
        _runningGameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Start_ShouldThrowEntityNotFound_WhenTheGameDoesNotExist()
    {
        SetupGameNight(CreateNight());
        _gameServiceMock.Setup(x => x.ExistsAsync(9)).ReturnsAsync(false);

        var act = () => _service.Start(3, new StartRunningGameCommand { GameId = 9, PlayerIds = [1] });

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _gameServiceMock.Verify(x => x.ExistsAsync(9), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Start_ShouldThrowEntityNotFound_WhenAPlayerDoesNotExist()
    {
        SetupGameNight(CreateNight());
        _gameServiceMock.Setup(x => x.ExistsAsync(9)).ReturnsAsync(true);
        _playerServiceMock
            .Setup(x => x.GetExistingIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([1]);

        var act = () => _service.Start(3, new StartRunningGameCommand { GameId = 9, PlayerIds = [1, 8] });

        var error = await act.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityId.Should().Be(8);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<GameNightByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _gameServiceMock.Verify(x => x.ExistsAsync(9), Times.Once);
        _playerServiceMock.Verify(x => x.GetExistingIdsAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Finish_ShouldTurnTheRunningGameIntoASessionOfTheNight_EndingNow()
    {
        var runningGame = CreateRunningGame(Now.AddMinutes(-37));
        var session = new Session(9, runningGame.Start, Now, string.Empty) { Id = 40 };
        var players = new List<CreatePlayerSessionCommand> { new() { PlayerId = 1, Score = 88, Won = true } };
        _runningGameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(runningGame);
        _sessionServiceMock
            .Setup(x => x.CreateFromCommand(It.IsAny<CreateSessionCommand>()))
            .ReturnsAsync(session);

        var result = await _service.Finish(12, new FinishRunningGameCommand { PlayerSessions = players });

        result.Should().BeSameAs(session);
        _runningGameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _runningGameRepositoryMock.Verify(x => x.DeleteAsync(12), Times.Once);
        _sessionServiceMock.Verify(x => x.CreateFromCommand(It.Is<CreateSessionCommand>(c =>
            c.GameId == 9 &&
            c.Start == runningGame.Start &&
            c.Minutes == 37 &&
            c.LocationId == 5 &&
            c.GameNightId == 3 &&
            c.PlayerSessions == players)), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Finish_ShouldUseTheChangedTimes_WhenGiven()
    {
        var runningGame = CreateRunningGame(Now.AddMinutes(-37));
        var start = Now.AddHours(-2);
        _runningGameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(runningGame);
        _sessionServiceMock
            .Setup(x => x.CreateFromCommand(It.IsAny<CreateSessionCommand>()))
            .ReturnsAsync(new Session(9, start, Now, string.Empty));

        await _service.Finish(12, new FinishRunningGameCommand
        {
            PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = 1 }],
            Start = start,
            Minutes = 50,
        });

        _runningGameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _runningGameRepositoryMock.Verify(x => x.DeleteAsync(12), Times.Once);
        _sessionServiceMock.Verify(x => x.CreateFromCommand(It.Is<CreateSessionCommand>(c => c.Start == start && c.Minutes == 50)), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Finish_ShouldPassTheLocationCommentAndExpansions_WhenGiven()
    {
        var runningGame = CreateRunningGame(Now.AddMinutes(-37));
        _runningGameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(runningGame);
        _sessionServiceMock
            .Setup(x => x.CreateFromCommand(It.IsAny<CreateSessionCommand>()))
            .ReturnsAsync(new Session(9, runningGame.Start, Now, "Close one"));

        await _service.Finish(12, new FinishRunningGameCommand
        {
            PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = 1 }],
            LocationId = 8,
            Comment = "Close one",
            ExpansionIds = [3, 4],
        });

        _runningGameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _runningGameRepositoryMock.Verify(x => x.DeleteAsync(12), Times.Once);
        _sessionServiceMock.Verify(x => x.CreateFromCommand(It.Is<CreateSessionCommand>(c =>
            c.LocationId == 8 &&
            c.Comment == "Close one" &&
            c.ExpansionIds.SequenceEqual(new[] { 3, 4 }))), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Finish_ShouldPassTheTeamResultOn_WhenCoop()
    {
        var runningGame = CreateRunningGame(Now.AddMinutes(-37));
        _runningGameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(runningGame);
        _sessionServiceMock
            .Setup(x => x.CreateFromCommand(It.IsAny<CreateSessionCommand>()))
            .ReturnsAsync(new Session(9, runningGame.Start, Now, string.Empty));

        await _service.Finish(12, new FinishRunningGameCommand
        {
            PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = 1 }],
            IsCoop = true,
            TeamWon = false,
            TeamScore = 14,
        });

        _runningGameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _runningGameRepositoryMock.Verify(x => x.DeleteAsync(12), Times.Once);
        _sessionServiceMock.Verify(x => x.CreateFromCommand(It.Is<CreateSessionCommand>(c => c.IsCoop && c.TeamWon == false && c.TeamScore == 14)), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Finish_ShouldCountAtLeastOneMinute_WhenFinishedRightAway()
    {
        var runningGame = CreateRunningGame(Now.AddSeconds(-10));
        _runningGameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(runningGame);
        _sessionServiceMock
            .Setup(x => x.CreateFromCommand(It.IsAny<CreateSessionCommand>()))
            .ReturnsAsync(new Session(9, runningGame.Start, Now, string.Empty));

        await _service.Finish(12, new FinishRunningGameCommand { PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = 1 }] });

        _runningGameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _runningGameRepositoryMock.Verify(x => x.DeleteAsync(12), Times.Once);
        _sessionServiceMock.Verify(x => x.CreateFromCommand(It.Is<CreateSessionCommand>(c => c.Minutes == 1)), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Finish_ShouldThrowEntityNotFound_WhenTheRunningGameDoesNotExist()
    {
        _runningGameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunningGame?)null);

        var act = () => _service.Finish(12, new FinishRunningGameCommand { PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = 1 }] });

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _runningGameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.IsAny<RunningGameByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheRunningGame()
    {
        await _service.Delete(12);

        _runningGameRepositoryMock.Verify(x => x.DeleteAsync(12), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }
}
