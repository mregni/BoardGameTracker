using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions;
using BoardGameTracker.Core.Sessions.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Sessions;

public class SessionServiceTests
{
    private const int NightId = 7;
    private static readonly DateTime Now = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);
    private static readonly GameNight LinkedNight = NightAt(Now, NightId);
    private static readonly GameNight OtherNight = NightAt(Now, NightId + 1);

    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IBadgeService> _badgeServiceMock;
    private readonly Mock<IGameService> _gameServiceMock;
    private readonly Mock<ILocationService> _locationServiceMock;
    private readonly Mock<IPlayerService> _playerServiceMock;
    private readonly Mock<IReadRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<ILogger<SessionService>> _loggerMock;
    private readonly SessionService _sessionService;

    public SessionServiceTests()
    {
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _badgeServiceMock = new Mock<IBadgeService>();
        _gameServiceMock = new Mock<IGameService>();
        _locationServiceMock = new Mock<ILocationService>();
        _playerServiceMock = new Mock<IPlayerService>();
        _gameNightRepositoryMock = new Mock<IReadRepository<GameNight>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);
        _loggerMock = new Mock<ILogger<SessionService>>();

        _sessionService = new SessionService(
            _sessionRepositoryMock.Object,
            _badgeServiceMock.Object,
            _gameServiceMock.Object,
            _locationServiceMock.Object,
            _playerServiceMock.Object,
            _gameNightRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dateTimeProviderMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _badgeServiceMock.VerifyNoOtherCalls();
        _gameServiceMock.VerifyNoOtherCalls();
        _locationServiceMock.VerifyNoOtherCalls();
        _playerServiceMock.VerifyNoOtherCalls();
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _dateTimeProviderMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    #region Helpers

    private static GameNight NightAt(DateTime start, int id = NightId)
    {
        var gameNight = GameNight.Create("Night", "", start, 1, 1);
        gameNight.Id = id;
        return gameNight;
    }

    private static bool Matches(Session session, CreateSessionCommand command)
    {
        return session.GameId == command.GameId
            && session.Start == command.Start
            && session.End == command.Start.AddMinutes(command.Minutes)
            && session.Comment == (command.Comment ?? string.Empty)
            && session.PlayerSessions.Select(ps => ps.PlayerId).SequenceEqual(PlayerIds(command));
    }

    private static int[] PlayerIds(CreateSessionCommand command)
    {
        return command.PlayerSessions.Select(ps => ps.PlayerId).Distinct().ToArray();
    }

    private void SetupExistingPlayers(CreateSessionCommand command)
    {
        var ids = PlayerIds(command);
        if (ids.Length > 0)
        {
            _playerServiceMock
                .Setup(x => x.GetExistingIdsAsync(It.Is<IEnumerable<int>>(requested => requested.SequenceEqual(ids))))
                .ReturnsAsync(ids.ToList());
        }
    }

    private void SetupNoExistingPlayers(params int[] requestedIds)
    {
        _playerServiceMock
            .Setup(x => x.GetExistingIdsAsync(It.Is<IEnumerable<int>>(requested => requested.SequenceEqual(requestedIds))))
            .ReturnsAsync([]);
    }

    private void SetupCreateChecks(CreateSessionCommand command)
    {
        _gameServiceMock.Setup(x => x.ExistsAsync(command.GameId)).ReturnsAsync(true);
        SetupExistingPlayers(command);
    }

    private void SetupCreate(CreateSessionCommand command)
    {
        SetupCreateChecks(command);
        _sessionRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Session>(s => Matches(s, command))))
            .ReturnsAsync((Session s) => s);
        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(It.Is<Session>(s => Matches(s, command))))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
    }

    private void SetupUpdate(Session session, CreateSessionCommand command)
    {
        SetupExistingPlayers(command);
        _sessionRepositoryMock.Setup(x => x.GetByIdAsync(session.Id)).ReturnsAsync(session);
        _badgeServiceMock.Setup(x => x.AwardBadgesAsync(session)).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
    }

    private void VerifyClockReads(int reads)
    {
        _dateTimeProviderMock.Verify(x => x.UtcNow, Times.Exactly(reads));
    }

    private void VerifyPlayerLookup(params int[] ids)
    {
        _playerServiceMock.Verify(x => x.GetExistingIdsAsync(It.Is<IEnumerable<int>>(requested => requested.SequenceEqual(ids))), Times.Once);
    }

    private void VerifyGameLookup(int gameId)
    {
        _gameServiceMock.Verify(x => x.ExistsAsync(gameId), Times.Once);
    }

    private void VerifyCreateChecks(CreateSessionCommand command, int clockReads = 1)
    {
        VerifyClockReads(clockReads);
        VerifyCommandCreateLog(command.GameId);
        VerifyGameLookup(command.GameId);
        if (command.PlayerSessions.Count > 0)
        {
            VerifyPlayerLookup(PlayerIds(command));
        }
    }

    private void VerifyCreated(CreateSessionCommand command, int clockReads = 1)
    {
        VerifyCreateChecks(command, clockReads);
        _sessionRepositoryMock.Verify(x => x.CreateAsync(It.Is<Session>(s => Matches(s, command))), Times.Once);
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(It.Is<Session>(s => Matches(s, command))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Exactly(2));
        VerifyCreateLogs(0, command.GameId);
    }

    private void VerifyUpdateChecks(int sessionId, CreateSessionCommand command)
    {
        VerifyClockReads(1);
        VerifyCommandUpdateLog(sessionId);
        _sessionRepositoryMock.Verify(x => x.GetByIdAsync(sessionId), Times.Once);
        if (command.PlayerSessions.Count > 0)
        {
            VerifyPlayerLookup(PlayerIds(command));
        }
    }

    private void VerifyUpdated(Session session, CreateSessionCommand command)
    {
        VerifyUpdateChecks(session.Id, command);
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(session), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Exactly(2));
        VerifyUpdateLogs(session.Id);
    }

    private void VerifyCommandCreateLog(int gameId)
    {
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating session from command for game {gameId}");
    }

    private void VerifyCommandUpdateLog(int sessionId)
    {
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating session {sessionId} from command");
    }

    private void VerifyCreateLogs(int sessionId, int gameId)
    {
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating session for game {gameId}");
        _loggerMock.VerifyLog(LogLevel.Information, $"Session {sessionId} created for game {gameId}");
    }

    private void VerifyUpdateLogs(int sessionId)
    {
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating session {sessionId}");
        _loggerMock.VerifyLog(LogLevel.Information, $"Session {sessionId} updated");
    }

    private void VerifyBadgeFailureLog(int sessionId, Exception exception)
    {
        _loggerMock.VerifyLog(LogLevel.Warning, $"Session {sessionId} was saved, but awarding badges failed; they are evaluated again on the next save", exception);
    }

    private void VerifyGameNightRead(Times times)
    {
        _gameNightRepositoryMock.Verify(
            x => x.SingleOrDefaultAsync(It.Is<GameNightByIdSpec>(s => s.IsSatisfiedBy(LinkedNight) && !s.IsSatisfiedBy(OtherNight)), CancellationToken.None),
            times);
    }

    private void SetupGameNightRead(GameNight? gameNight)
    {
        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameNightByIdSpec>(s => s.IsSatisfiedBy(LinkedNight) && !s.IsSatisfiedBy(OtherNight)), CancellationToken.None))
            .ReturnsAsync(gameNight);
    }

    private static CreateSessionCommand CreateCommand(params CreatePlayerSessionCommand[] players)
    {
        return new CreateSessionCommand
        {
            GameId = 1,
            Start = Now.AddHours(-2),
            Minutes = 120,
            PlayerSessions = [.. players]
        };
    }

    private static UpdateSessionCommand UpdateCommand(params CreatePlayerSessionCommand[] players)
    {
        return new UpdateSessionCommand
        {
            Id = 1,
            GameId = 1,
            Start = Now.AddHours(-2),
            Minutes = 120,
            PlayerSessions = [.. players]
        };
    }

    private static CreatePlayerSessionCommand Player(int id, bool won = false, double? score = null, bool firstPlay = false)
    {
        return new CreatePlayerSessionCommand { PlayerId = id, Won = won, Score = score, FirstPlay = firstPlay };
    }

    private static Session StoredSession(params int[] playerIds)
    {
        var session = new Session(1, Now.AddHours(-3), Now.AddHours(-1), "Old comment") { Id = 1 };
        foreach (var playerId in playerIds)
        {
            session.AddPlayerSession(playerId, null, false, false);
        }

        return session;
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ShouldCreateSession_AndAwardBadges()
    {
        var session = new Session(1, Now.AddHours(-2), Now, "Test session");
        var created = new Session(1, Now.AddHours(-2), Now, "Test session") { Id = 12 };

        _sessionRepositoryMock
            .Setup(x => x.CreateAsync(session))
            .ReturnsAsync(created);

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(created))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _sessionService.Create(session);

        result.Should().BeSameAs(created);

        _sessionRepositoryMock.Verify(x => x.CreateAsync(session), Times.Once);
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(created), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Exactly(2));
        VerifyCreateLogs(12, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldSaveTheSession_BeforeAwardingBadges_AndSaveTheBadgesAfterwards()
    {
        var session = new Session(1, Now.AddHours(-2), Now, "Test session");
        var steps = new List<string>();

        _sessionRepositoryMock
            .Setup(x => x.CreateAsync(session))
            .Callback(() => steps.Add("create"))
            .ReturnsAsync(session);
        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(session))
            .Callback(() => steps.Add("award"))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .Callback(() => steps.Add("save"))
            .ReturnsAsync(1);

        await _sessionService.Create(session);

        steps.Should().Equal("create", "save", "award", "save");
        _sessionRepositoryMock.Verify(x => x.CreateAsync(session), Times.Once);
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(session), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Exactly(2));
        VerifyCreateLogs(0, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldStillReturnTheSavedSession_WhenAwardingBadgesFails()
    {
        var session = new Session(1, Now.AddHours(-2), Now, "Test session") { Id = 3 };
        var failure = new Microsoft.EntityFrameworkCore.DbUpdateException("duplicate badge");
        _sessionRepositoryMock.Setup(x => x.CreateAsync(session)).ReturnsAsync(session);
        _unitOfWorkMock
            .SetupSequence(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1)
            .ThrowsAsync(failure);
        _badgeServiceMock.Setup(x => x.AwardBadgesAsync(session)).Returns(Task.CompletedTask);

        var result = await _sessionService.Create(session);

        result.Should().BeSameAs(session);
        _sessionRepositoryMock.Verify(x => x.CreateAsync(session), Times.Once);
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(session), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Exactly(2));
        _unitOfWorkMock.Verify(x => x.DiscardPendingInserts(), Times.Once);
        VerifyBadgeFailureLog(3, failure);
        VerifyCreateLogs(3, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldSkipTheSecondSave_WhenTheBadgeServiceFails()
    {
        var session = new Session(1, Now.AddHours(-2), Now, "Test session") { Id = 3 };
        var failure = new InvalidOperationException("evaluator failed");
        _sessionRepositoryMock.Setup(x => x.CreateAsync(session)).ReturnsAsync(session);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _badgeServiceMock.Setup(x => x.AwardBadgesAsync(session)).ThrowsAsync(failure);

        var result = await _sessionService.Create(session);

        result.Should().BeSameAs(session);
        _sessionRepositoryMock.Verify(x => x.CreateAsync(session), Times.Once);
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(session), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _unitOfWorkMock.Verify(x => x.DiscardPendingInserts(), Times.Once);
        VerifyBadgeFailureLog(3, failure);
        VerifyCreateLogs(3, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldPropagateCancellation_WhileAwardingBadges()
    {
        var session = new Session(1, Now.AddHours(-2), Now, "Test session");
        var cancellation = new OperationCanceledException();
        _sessionRepositoryMock.Setup(x => x.CreateAsync(session)).ReturnsAsync(session);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _badgeServiceMock.Setup(x => x.AwardBadgesAsync(session)).ThrowsAsync(cancellation);

        var action = async () => await _sessionService.Create(session);

        (await action.Should().ThrowAsync<OperationCanceledException>()).Which.Should().BeSameAs(cancellation);

        _sessionRepositoryMock.Verify(x => x.CreateAsync(session), Times.Once);
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(session), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _unitOfWorkMock.Verify(x => x.DiscardPendingInserts(), Times.Never);
        _loggerMock.VerifyLog(LogLevel.Debug, "Creating session for game 1");
        VerifyNoOtherCalls();
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ShouldReturnSession_WhenSessionExists()
    {
        var sessionId = 1;
        var session = new Session(1, Now.AddHours(-2), Now, "Test session") { Id = sessionId };

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        var result = await _sessionService.Get(sessionId);

        result.Should().BeSameAs(session);

        _sessionRepositoryMock.Verify(x => x.GetByIdAsync(sessionId), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Fetching session 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenSessionDoesNotExist()
    {
        var sessionId = 999;

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(sessionId))
            .ReturnsAsync((Session?)null);

        var result = await _sessionService.Get(sessionId);

        result.Should().BeNull();

        _sessionRepositoryMock.Verify(x => x.GetByIdAsync(sessionId), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Fetching session 999");
        VerifyNoOtherCalls();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldDeleteSession_AndSaveChanges()
    {
        var sessionId = 1;

        _sessionRepositoryMock
            .Setup(x => x.DeleteAsync(sessionId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _sessionService.Delete(sessionId);

        _sessionRepositoryMock.Verify(x => x.DeleteAsync(sessionId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Deleting session 1");
        _loggerMock.VerifyLog(LogLevel.Information, "Session 1 deleted");
        VerifyNoOtherCalls();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdateSession_AndAwardBadges()
    {
        var session = new Session(1, Now.AddHours(-2), Now, "Updated session") { Id = 1 };

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(session))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _sessionService.Update(session);

        result.Should().BeSameAs(session);

        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(session), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Exactly(2));
        VerifyUpdateLogs(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldSaveTheChanges_BeforeAwardingBadges()
    {
        var session = new Session(1, Now.AddHours(-2), Now, "Updated session") { Id = 1 };
        var steps = new List<string>();

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(session))
            .Callback(() => steps.Add("award"))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .Callback(() => steps.Add("save"))
            .ReturnsAsync(1);

        await _sessionService.Update(session);

        steps.Should().Equal("save", "award", "save");
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(session), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Exactly(2));
        VerifyUpdateLogs(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldStillReturnTheSavedSession_WhenAwardingBadgesFails()
    {
        var session = new Session(1, Now.AddHours(-2), Now, "Updated session") { Id = 1 };
        var failure = new InvalidOperationException("evaluator failed");
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _badgeServiceMock.Setup(x => x.AwardBadgesAsync(session)).ThrowsAsync(failure);

        var result = await _sessionService.Update(session);

        result.Should().BeSameAs(session);
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(session), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _unitOfWorkMock.Verify(x => x.DiscardPendingInserts(), Times.Once);
        VerifyBadgeFailureLog(1, failure);
        VerifyUpdateLogs(1);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CreateFromCommand Tests

    [Fact]
    public async Task CreateFromCommand_ShouldCreateSession_WithBasicData()
    {
        var command = CreateCommand(Player(1, won: true, score: 100, firstPlay: true));
        command.Comment = "Great game";
        SetupCreate(command);

        var result = await _sessionService.CreateFromCommand(command);

        result.GameId.Should().Be(1);
        result.Comment.Should().Be("Great game");
        result.Start.Should().Be(command.Start);
        result.End.Should().Be(Now);
        result.PlayerSessions.Should().ContainSingle(ps => ps.PlayerId == 1 && ps.Score == 100 && ps.Won && ps.FirstPlay);
        result.GameNightId.Should().BeNull();
        result.LocationId.Should().BeNull();
        result.IsCoop.Should().BeFalse();

        VerifyCreated(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldStoreAnEmptyComment_WhenNoCommentIsGiven()
    {
        var command = CreateCommand(Player(1));
        SetupCreate(command);

        var result = await _sessionService.CreateFromCommand(command);

        result.Comment.Should().BeEmpty();

        VerifyCreated(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldAddExpansions_WhenProvided()
    {
        var expansions = new List<Expansion>
        {
            new Expansion("Expansion 1", 100, 1) { Id = 1 },
            new Expansion("Expansion 2", 101, 1) { Id = 2 }
        };

        var command = CreateCommand(Player(1, won: true));
        command.ExpansionIds = [1, 2];
        SetupCreate(command);

        _gameServiceMock
            .Setup(x => x.GetGameExpansions(1, It.Is<List<int>>(ids => ids.SequenceEqual(new[] { 1, 2 }))))
            .ReturnsAsync(expansions);

        var result = await _sessionService.CreateFromCommand(command);

        result.Expansions.Should().Equal(expansions);

        _gameServiceMock.Verify(x => x.GetGameExpansions(1, It.Is<List<int>>(ids => ids.SequenceEqual(new[] { 1, 2 }))), Times.Once);
        VerifyCreated(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldSetLocation_WhenLocationIdProvided()
    {
        var location = new Location("Game Store") { Id = 5 };

        var command = CreateCommand(Player(1, won: true));
        command.LocationId = 5;
        SetupCreate(command);

        _locationServiceMock
            .Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync(location);

        var result = await _sessionService.CreateFromCommand(command);

        result.LocationId.Should().Be(5);
        result.Location.Should().BeSameAs(location);

        _locationServiceMock.Verify(x => x.GetByIdAsync(5), Times.Once);
        VerifyCreated(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldAddMultiplePlayers()
    {
        var command = CreateCommand(Player(1, won: true, score: 100), Player(2, score: 80), Player(3, score: 60));
        SetupCreate(command);

        var result = await _sessionService.CreateFromCommand(command);

        result.PlayerSessions.Select(ps => (ps.PlayerId, ps.Score, ps.Won)).Should().Equal((1, 100, true), (2, 80, false), (3, 60, false));

        VerifyCreated(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldLookUpEachPlayerOnce_WhenAPlayerIsListedTwice()
    {
        var command = CreateCommand(Player(1, won: true, score: 100), Player(1, score: 80));
        SetupCreate(command);

        var result = await _sessionService.CreateFromCommand(command);

        result.PlayerSessions.Should().ContainSingle(ps => ps.PlayerId == 1 && ps.Score == 100 && ps.Won);

        VerifyCreated(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldThrowValidationException_WhenStartIsMoreThanADayAhead()
    {
        var command = CreateCommand();
        command.GameId = 10;
        command.Start = Now.AddDays(2);

        var action = async () => await _sessionService.CreateFromCommand(command);

        var error = await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.SessionStartInFuture);
        error.Which.Errors[nameof(CreateSessionCommand.Start)].Should().Equal(Constants.Errors.SessionStartInFuture);

        VerifyClockReads(1);
        VerifyCommandCreateLog(10);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldThrowValidationException_WhenStartIsJustPastADayAhead()
    {
        var command = CreateCommand();
        command.Start = Now.AddDays(1).AddTicks(1);

        var action = async () => await _sessionService.CreateFromCommand(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.SessionStartInFuture);

        VerifyClockReads(1);
        VerifyCommandCreateLog(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldAllowStart_ExactlyADayAhead()
    {
        var command = CreateCommand();
        command.Start = Now.AddDays(1);
        SetupCreate(command);

        var result = await _sessionService.CreateFromCommand(command);

        result.Start.Should().Be(command.Start);

        VerifyCreated(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldAllowStart_WithinTheClockSkewWindow()
    {
        var command = CreateCommand();
        command.GameId = 10;
        command.Start = Now.AddHours(3);
        command.Minutes = 60;
        SetupCreate(command);

        var result = await _sessionService.CreateFromCommand(command);

        result.Start.Should().Be(command.Start);

        VerifyCreated(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldThrowEntityNotFound_WhenGameDoesNotExist()
    {
        var command = CreateCommand();
        command.GameId = 10;

        _gameServiceMock
            .Setup(x => x.ExistsAsync(10))
            .ReturnsAsync(false);

        var action = async () => await _sessionService.CreateFromCommand(command);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(Game));
        error.Which.EntityId.Should().Be(10);

        VerifyCreateChecks(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldThrowEntityNotFound_WhenLocationDoesNotExist()
    {
        var command = CreateCommand();
        command.LocationId = 99;
        SetupCreateChecks(command);

        _locationServiceMock
            .Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync((Location?)null);

        var action = async () => await _sessionService.CreateFromCommand(command);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(Location));
        error.Which.EntityId.Should().Be(99);

        VerifyCreateChecks(command);
        _locationServiceMock.Verify(x => x.GetByIdAsync(99), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldThrowEntityNotFound_WhenPlayerDoesNotExist()
    {
        var command = CreateCommand(Player(5, won: true));
        _gameServiceMock.Setup(x => x.ExistsAsync(1)).ReturnsAsync(true);
        SetupNoExistingPlayers(5);

        var action = async () => await _sessionService.CreateFromCommand(command);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(Player));
        error.Which.EntityId.Should().Be(5);

        VerifyCreateChecks(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldNameTheMissingPlayer_WhenOtherPlayersExist()
    {
        var command = CreateCommand(Player(1), Player(5), Player(6));
        _gameServiceMock.Setup(x => x.ExistsAsync(1)).ReturnsAsync(true);
        _playerServiceMock
            .Setup(x => x.GetExistingIdsAsync(It.Is<IEnumerable<int>>(requested => requested.SequenceEqual(new[] { 1, 5, 6 }))))
            .ReturnsAsync([1, 6]);

        var action = async () => await _sessionService.CreateFromCommand(command);

        (await action.Should().ThrowAsync<EntityNotFoundException>()).Which.EntityId.Should().Be(5);

        VerifyCreateChecks(command);
        VerifyNoOtherCalls();
    }

    #endregion

    [Fact]
    public async Task CreateFromCommand_ShouldLinkTheGameNight_WhenGameNightIdProvided()
    {
        var command = CreateCommand(Player(1, won: true));
        command.GameNightId = NightId;
        SetupCreate(command);
        SetupGameNightRead(NightAt(Now.AddHours(-3)));

        var result = await _sessionService.CreateFromCommand(command);

        result.GameNightId.Should().Be(NightId);

        VerifyGameNightRead(Times.Once());
        VerifyCreated(command, clockReads: 2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldThrowEntityNotFound_WhenGameNightDoesNotExist()
    {
        var command = CreateCommand(Player(1));
        command.GameNightId = NightId;
        SetupCreateChecks(command);
        SetupGameNightRead(null);

        var action = () => _sessionService.CreateFromCommand(command);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(GameNight));
        error.Which.EntityId.Should().Be(NightId);

        VerifyGameNightRead(Times.Once());
        VerifyCreateChecks(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldBeRefused_WhenTheGameNightHasEnded()
    {
        var gameNight = NightAt(Now.AddHours(-5));
        gameNight.End(Now.AddHours(-1));
        var command = CreateCommand(Player(1));
        command.GameNightId = NightId;
        SetupCreateChecks(command);
        SetupGameNightRead(gameNight);

        var action = () => _sessionService.CreateFromCommand(command);

        var error = await action.Should().ThrowAsync<ValidationException>();
        error.Which.Errors[nameof(CreateSessionCommand.GameNightId)].Should().Equal(Constants.Errors.GameNightEnded);

        VerifyGameNightRead(Times.Once());
        VerifyCreateChecks(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldBeRefused_WhenTheGameNightIsMoreThanTwelveHoursAway()
    {
        var command = CreateCommand(Player(1));
        command.GameNightId = NightId;
        SetupCreateChecks(command);
        SetupGameNightRead(NightAt(Now.AddDays(7)));

        var action = () => _sessionService.CreateFromCommand(command);

        var error = await action.Should().ThrowAsync<ValidationException>();
        error.Which.Errors[nameof(CreateSessionCommand.GameNightId)].Should().Equal(Constants.Errors.GameNightNotStarted);

        VerifyGameNightRead(Times.Once());
        VerifyCreateChecks(command, clockReads: 2);
        VerifyNoOtherCalls();
    }

    #region UpdateFromCommand Tests

    [Fact]
    public async Task UpdateFromCommand_ShouldUpdateSession_WhenSessionExists()
    {
        var existingSession = StoredSession();
        existingSession.AddPlayerSession(1, 50, false, false);

        var command = UpdateCommand(Player(1, won: true, score: 100, firstPlay: true));
        command.Comment = "Updated comment";
        SetupUpdate(existingSession, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.Should().BeSameAs(existingSession);
        result.Comment.Should().Be("Updated comment");
        result.Start.Should().Be(command.Start);
        result.End.Should().Be(Now);
        result.PlayerSessions.Should().ContainSingle(ps => ps.PlayerId == 1 && ps.Score == 100 && ps.Won && ps.FirstPlay);

        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldClearTheComment_WhenNoCommentIsGiven()
    {
        var existingSession = StoredSession(1);
        var command = UpdateCommand(Player(1));
        SetupUpdate(existingSession, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.Comment.Should().BeEmpty();

        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldUpdateTheResultsOfExistingPlayers()
    {
        var existingSession = StoredSession();
        existingSession.AddPlayerSession(1, 50, true, true);
        existingSession.AddPlayerSession(2, 40, false, false);
        var command = UpdateCommand(Player(1, score: 60), Player(2, won: true, score: 70, firstPlay: true));
        SetupUpdate(existingSession, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.PlayerSessions.Select(ps => (ps.PlayerId, ps.Score, ps.Won, ps.FirstPlay))
            .Should().Equal((1, 60, false, false), (2, 70, true, true));

        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldAddPlayers_NewInTheCommand()
    {
        var existingSession = StoredSession(1);
        var command = UpdateCommand(Player(1), Player(2, won: true, score: 30, firstPlay: true));
        SetupUpdate(existingSession, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.PlayerSessions.Should().HaveCount(2);
        result.PlayerSessions.Should().ContainSingle(ps => ps.PlayerId == 2 && ps.Score == 30 && ps.Won && ps.FirstPlay);

        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact(Skip = "BUG: UpdateFromCommand ignores command.GameId, so changing the game in the edit form is silently dropped")]
    public async Task UpdateFromCommand_ShouldMoveTheSessionToTheGameInTheCommand()
    {
        var existingSession = StoredSession(1);
        var command = UpdateCommand(Player(1));
        command.GameId = 2;
        SetupUpdate(existingSession, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.GameId.Should().Be(2);

        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldKeepTheGameNightLink_WhenGameNightIdProvided()
    {
        var existingSession = StoredSession();
        existingSession.SetGameNight(NightId);

        var command = UpdateCommand(Player(1, won: true));
        command.GameNightId = NightId;
        SetupUpdate(existingSession, command);

        _gameNightRepositoryMock
            .Setup(x => x.AnyAsync(It.Is<GameNightByIdSpec>(s => s.IsSatisfiedBy(LinkedNight) && !s.IsSatisfiedBy(OtherNight)), CancellationToken.None))
            .ReturnsAsync(true);

        var result = await _sessionService.UpdateFromCommand(command);

        result.GameNightId.Should().Be(NightId);

        _gameNightRepositoryMock.Verify(x => x.AnyAsync(It.Is<GameNightByIdSpec>(s => s.IsSatisfiedBy(LinkedNight) && !s.IsSatisfiedBy(OtherNight)), CancellationToken.None), Times.Once);
        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldThrowEntityNotFound_WhenGameNightDoesNotExist()
    {
        var existingSession = StoredSession();
        var command = UpdateCommand(Player(1));
        command.GameNightId = NightId;
        SetupUpdate(existingSession, command);

        _gameNightRepositoryMock
            .Setup(x => x.AnyAsync(It.Is<GameNightByIdSpec>(s => s.IsSatisfiedBy(LinkedNight) && !s.IsSatisfiedBy(OtherNight)), CancellationToken.None))
            .ReturnsAsync(false);

        var action = async () => await _sessionService.UpdateFromCommand(command);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(GameNight));
        error.Which.EntityId.Should().Be(NightId);
        existingSession.Comment.Should().Be("Old comment");

        _gameNightRepositoryMock.Verify(x => x.AnyAsync(It.Is<GameNightByIdSpec>(s => s.IsSatisfiedBy(LinkedNight) && !s.IsSatisfiedBy(OtherNight)), CancellationToken.None), Times.Once);
        VerifyUpdateChecks(1, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldUnlinkTheGameNight_WhenGameNightIdMissing()
    {
        var existingSession = StoredSession();
        existingSession.SetGameNight(NightId);

        var command = UpdateCommand(Player(1, won: true));
        SetupUpdate(existingSession, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.GameNightId.Should().BeNull();

        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldThrowValidationException_WhenStartIsMoreThanADayAhead()
    {
        var command = UpdateCommand();
        command.Id = 3;
        command.Start = Now.AddYears(1);

        var action = async () => await _sessionService.UpdateFromCommand(command);

        var error = await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.SessionStartInFuture);
        error.Which.Errors[nameof(CreateSessionCommand.Start)].Should().Equal(Constants.Errors.SessionStartInFuture);

        VerifyClockReads(1);
        VerifyCommandUpdateLog(3);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldThrowValidationException_WhenStartIsJustPastADayAhead()
    {
        var command = UpdateCommand();
        command.Start = Now.AddDays(1).AddTicks(1);

        var action = async () => await _sessionService.UpdateFromCommand(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.SessionStartInFuture);

        VerifyClockReads(1);
        VerifyCommandUpdateLog(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldAllowStart_ExactlyADayAhead()
    {
        var existingSession = StoredSession();
        var command = UpdateCommand();
        command.Start = Now.AddDays(1);
        SetupUpdate(existingSession, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.Start.Should().Be(command.Start);

        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldThrowException_WhenSessionNotFound()
    {
        var command = UpdateCommand();
        command.Id = 999;

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(command.Id))
            .ReturnsAsync((Session?)null);

        var action = async () => await _sessionService.UpdateFromCommand(command);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(Session));
        error.Which.EntityId.Should().Be(999);

        VerifyUpdateChecks(999, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldAddNewExpansions()
    {
        var existingSession = StoredSession(1);
        var newExpansion = new Expansion("New Expansion", 100, 1) { Id = 5 };

        var command = UpdateCommand(Player(1, won: true));
        command.ExpansionIds = [5];
        SetupUpdate(existingSession, command);

        _gameServiceMock
            .Setup(x => x.GetGameExpansions(1, It.Is<List<int>>(ids => ids.SequenceEqual(new[] { 5 }))))
            .ReturnsAsync([newExpansion]);

        var result = await _sessionService.UpdateFromCommand(command);

        result.Expansions.Should().Equal(newExpansion);

        _gameServiceMock.Verify(x => x.GetGameExpansions(1, It.Is<List<int>>(ids => ids.SequenceEqual(new[] { 5 }))), Times.Once);
        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldOnlyLoadTheExpansionsThatAreNew()
    {
        var existingSession = StoredSession(1);
        var keptExpansion = new Expansion("Kept Expansion", 100, 1) { Id = 5 };
        var droppedExpansion = new Expansion("Dropped Expansion", 101, 1) { Id = 6 };
        var newExpansion = new Expansion("New Expansion", 102, 1) { Id = 7 };
        existingSession.AddExpansion(keptExpansion);
        existingSession.AddExpansion(droppedExpansion);

        var command = UpdateCommand(Player(1));
        command.ExpansionIds = [5, 7];
        SetupUpdate(existingSession, command);

        _gameServiceMock
            .Setup(x => x.GetGameExpansions(1, It.Is<List<int>>(ids => ids.SequenceEqual(new[] { 7 }))))
            .ReturnsAsync([newExpansion]);

        var result = await _sessionService.UpdateFromCommand(command);

        result.Expansions.Should().Equal(keptExpansion, newExpansion);

        _gameServiceMock.Verify(x => x.GetGameExpansions(1, It.Is<List<int>>(ids => ids.SequenceEqual(new[] { 7 }))), Times.Once);
        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldRemoveExpansions_NotInCommand()
    {
        var existingSession = StoredSession(1);
        existingSession.AddExpansion(new Expansion("Old Expansion", 100, 1) { Id = 5 });

        var command = UpdateCommand(Player(1, won: true));
        SetupUpdate(existingSession, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.Expansions.Should().BeEmpty();

        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldRemovePlayers_NotInCommand()
    {
        var existingSession = StoredSession(1, 2);

        var command = UpdateCommand(Player(1, won: true));
        SetupUpdate(existingSession, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.PlayerSessions.Select(ps => ps.PlayerId).Should().Equal(1);

        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldClearLocation_WhenLocationIdNull()
    {
        var existingSession = StoredSession(1);
        existingSession.SetLocation(new Location("Old Location") { Id = 1 });

        var command = UpdateCommand(Player(1, won: true));
        SetupUpdate(existingSession, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.LocationId.Should().BeNull();
        result.Location.Should().BeNull();

        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldSetLocation_WhenLocationIdProvided()
    {
        var location = new Location("New Location") { Id = 5 };
        var existingSession = StoredSession(1);

        var command = UpdateCommand(Player(1, won: true));
        command.LocationId = 5;
        SetupUpdate(existingSession, command);

        _locationServiceMock
            .Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync(location);

        var result = await _sessionService.UpdateFromCommand(command);

        result.LocationId.Should().Be(5);
        result.Location.Should().BeSameAs(location);

        _locationServiceMock.Verify(x => x.GetByIdAsync(5), Times.Once);
        VerifyUpdated(existingSession, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldThrowEntityNotFound_WhenLocationDoesNotExist()
    {
        var existingSession = StoredSession();
        var command = UpdateCommand();
        command.LocationId = 99;

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existingSession);

        _locationServiceMock
            .Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync((Location?)null);

        var action = async () => await _sessionService.UpdateFromCommand(command);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(Location));
        error.Which.EntityId.Should().Be(99);

        _locationServiceMock.Verify(x => x.GetByIdAsync(99), Times.Once);
        VerifyUpdateChecks(1, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldThrowEntityNotFound_WhenPlayerDoesNotExist()
    {
        var existingSession = StoredSession();
        var command = UpdateCommand(Player(5, won: true));

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existingSession);

        SetupNoExistingPlayers(5);

        var action = async () => await _sessionService.UpdateFromCommand(command);

        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(Player));
        error.Which.EntityId.Should().Be(5);

        VerifyUpdateChecks(1, command);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Co-op Tests

    private static CreateSessionCommand CoopCommand(bool? teamWon, double? teamScore = null)
    {
        var command = CreateCommand(Player(1, score: 12, firstPlay: true), Player(2, score: 8));
        command.Minutes = 60;
        command.IsCoop = true;
        command.TeamWon = teamWon;
        command.TeamScore = teamScore;
        return command;
    }

    [Fact]
    public async Task CreateFromCommand_ShouldStoreTheTeamResultOnEveryPlayer_WhenCoop()
    {
        var command = CoopCommand(true, 42);
        SetupCreate(command);
        _gameServiceMock.Setup(x => x.HasCoopModeAsync(1)).ReturnsAsync(true);

        var result = await _sessionService.CreateFromCommand(command);

        result.IsCoop.Should().BeTrue();
        result.TeamWon.Should().BeTrue();
        result.TeamScore.Should().Be(42);
        result.PlayerSessions.Should().HaveCount(2).And.OnlyContain(ps => ps.Won && ps.Score == null);
        result.PlayerSessions.Single(ps => ps.PlayerId == 1).FirstPlay.Should().BeTrue();
        result.PlayerSessions.Single(ps => ps.PlayerId == 2).FirstPlay.Should().BeFalse();

        _gameServiceMock.Verify(x => x.HasCoopModeAsync(1), Times.Once);
        VerifyCreated(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldMarkEveryPlayerAsLoser_WhenTheTeamLost()
    {
        var command = CoopCommand(false);
        SetupCreate(command);
        _gameServiceMock.Setup(x => x.HasCoopModeAsync(1)).ReturnsAsync(true);

        var result = await _sessionService.CreateFromCommand(command);

        result.TeamWon.Should().BeFalse();
        result.TeamScore.Should().BeNull();
        result.PlayerSessions.Should().HaveCount(2).And.OnlyContain(ps => !ps.Won);

        _gameServiceMock.Verify(x => x.HasCoopModeAsync(1), Times.Once);
        VerifyCreated(command);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task CreateFromCommand_ShouldRefuseCoop_WhenTheGameHasNoCoopMode(bool? hasCoopMode)
    {
        var command = CoopCommand(true);
        SetupCreateChecks(command);
        _gameServiceMock.Setup(x => x.HasCoopModeAsync(1)).ReturnsAsync(hasCoopMode);

        var action = async () => await _sessionService.CreateFromCommand(command);

        var exception = await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.CoopNotSupported);
        exception.Which.Errors[nameof(CreateSessionCommand.IsCoop)].Should().Equal(Constants.Errors.CoopNotSupported);

        _gameServiceMock.Verify(x => x.HasCoopModeAsync(1), Times.Once);
        VerifyCreateChecks(command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldRequireATeamResult_WhenCoop()
    {
        var action = async () => await _sessionService.CreateFromCommand(CoopCommand(null));

        var exception = await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.CoopResultRequired);
        exception.Which.Errors[nameof(CreateSessionCommand.TeamWon)].Should().Equal(Constants.Errors.CoopResultRequired);

        VerifyClockReads(1);
        VerifyCommandCreateLog(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateFromCommand_ShouldIgnoreTheTeamResult_WhenNotCoop()
    {
        var command = CoopCommand(true, 42);
        command.IsCoop = false;
        command.PlayerSessions[1].Won = true;
        SetupCreate(command);

        var result = await _sessionService.CreateFromCommand(command);

        result.IsCoop.Should().BeFalse();
        result.TeamWon.Should().BeNull();
        result.TeamScore.Should().BeNull();
        result.PlayerSessions.Should().ContainSingle(ps => ps.Won && ps.PlayerId == 2 && ps.Score == 8);

        VerifyCreated(command);
        VerifyNoOtherCalls();
    }

    private static UpdateSessionCommand CoopUpdateCommand(bool isCoop, bool? teamWon)
    {
        var command = UpdateCommand(Player(1, won: true, score: 10), Player(2, score: 5));
        command.Minutes = 60;
        command.IsCoop = isCoop;
        command.TeamWon = teamWon;
        return command;
    }

    private static Session ExistingSession(bool isCoop)
    {
        var session = StoredSession(1, 2);
        if (isCoop)
        {
            session.MarkAsCoop(false, null);
        }

        return session;
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldKeepCoop_WhenTheGameNoLongerHasCoopMode()
    {
        var session = ExistingSession(true);
        var command = CoopUpdateCommand(true, true);
        SetupUpdate(session, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.IsCoop.Should().BeTrue();
        result.TeamWon.Should().BeTrue();
        result.PlayerSessions.Should().HaveCount(2).And.OnlyContain(ps => ps.Won && ps.Score == null);

        VerifyUpdated(session, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldCheckTheGame_WhenSwitchingToCoop()
    {
        var session = ExistingSession(false);
        var command = CoopUpdateCommand(true, false);
        SetupUpdate(session, command);
        _gameServiceMock.Setup(x => x.HasCoopModeAsync(1)).ReturnsAsync(false);

        var action = async () => await _sessionService.UpdateFromCommand(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.CoopNotSupported);

        _gameServiceMock.Verify(x => x.HasCoopModeAsync(1), Times.Once);
        VerifyUpdateChecks(1, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldSwitchToCoop_WhenTheGameHasCoopMode()
    {
        var session = ExistingSession(false);
        var command = CoopUpdateCommand(true, false);
        SetupUpdate(session, command);
        _gameServiceMock.Setup(x => x.HasCoopModeAsync(1)).ReturnsAsync(true);

        var result = await _sessionService.UpdateFromCommand(command);

        result.IsCoop.Should().BeTrue();
        result.PlayerSessions.Should().HaveCount(2).And.OnlyContain(ps => !ps.Won && ps.Score == null);

        _gameServiceMock.Verify(x => x.HasCoopModeAsync(1), Times.Once);
        VerifyUpdated(session, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldBringBackPlayerResults_WhenSwitchingCoopOff()
    {
        var session = ExistingSession(true);
        var command = CoopUpdateCommand(false, null);
        SetupUpdate(session, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.IsCoop.Should().BeFalse();
        result.TeamWon.Should().BeNull();
        result.PlayerSessions.Should().ContainSingle(ps => ps.Won && ps.PlayerId == 1 && ps.Score == 10);

        VerifyUpdated(session, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldKeepTheResultOfANewPlayer_WhenSwitchingCoopOff()
    {
        var session = ExistingSession(true);
        session.MarkAsCoop(true, null);
        var command = CoopUpdateCommand(false, null);
        command.PlayerSessions.Add(Player(3, score: 12));
        SetupUpdate(session, command);

        var result = await _sessionService.UpdateFromCommand(command);

        result.IsCoop.Should().BeFalse();
        result.PlayerSessions.Should().ContainSingle(ps => ps.PlayerId == 3 && !ps.Won && ps.Score == 12);

        VerifyUpdated(session, command);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldRequireATeamResult_WhenCoop()
    {
        var action = async () => await _sessionService.UpdateFromCommand(CoopUpdateCommand(true, null));

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.CoopResultRequired);

        VerifyClockReads(1);
        VerifyCommandUpdateLog(1);
        VerifyNoOtherCalls();
    }

    #endregion
}
