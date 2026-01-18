using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Sessions;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class SessionServiceTests
{
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IBadgeService> _badgeServiceMock;
    private readonly Mock<IGameService> _gameServiceMock;
    private readonly Mock<ILocationService> _locationServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly SessionService _sessionService;

    public SessionServiceTests()
    {
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _badgeServiceMock = new Mock<IBadgeService>();
        _gameServiceMock = new Mock<IGameService>();
        _locationServiceMock = new Mock<ILocationService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _sessionService = new SessionService(
            _sessionRepositoryMock.Object,
            _badgeServiceMock.Object,
            _gameServiceMock.Object,
            _locationServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _badgeServiceMock.VerifyNoOtherCalls();
        _gameServiceMock.VerifyNoOtherCalls();
        _locationServiceMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    #region Create Tests

    [Fact]
    public async Task Create_ShouldCreateSession_AndAwardBadges()
    {
        // Arrange
        var session = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Test session");

        _sessionRepositoryMock
            .Setup(x => x.CreateAsync(session))
            .ReturnsAsync(session);

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(session))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _sessionService.Create(session);

        // Assert
        result.Should().NotBeNull();

        _sessionRepositoryMock.Verify(x => x.CreateAsync(session), Times.Once);
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(session), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ShouldReturnSession_WhenSessionExists()
    {
        // Arrange
        var sessionId = 1;
        var session = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Test session") { Id = sessionId };

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _sessionService.Get(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);

        _sessionRepositoryMock.Verify(x => x.GetByIdAsync(sessionId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = 999;

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(sessionId))
            .ReturnsAsync((Session?)null);

        // Act
        var result = await _sessionService.Get(sessionId);

        // Assert
        result.Should().BeNull();

        _sessionRepositoryMock.Verify(x => x.GetByIdAsync(sessionId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldDeleteSession()
    {
        // Arrange
        var sessionId = 1;

        _sessionRepositoryMock
            .Setup(x => x.DeleteAsync(sessionId))
            .ReturnsAsync(true);

        // Act
        await _sessionService.Delete(sessionId);

        // Assert
        _sessionRepositoryMock.Verify(x => x.DeleteAsync(sessionId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdateSession_AndAwardBadges()
    {
        // Arrange
        var session = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Updated session") { Id = 1 };

        _sessionRepositoryMock
            .Setup(x => x.UpdateAsync(session))
            .ReturnsAsync(session);

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(session))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sessionService.Update(session);

        // Assert
        result.Should().NotBeNull();
        result.Comment.Should().Be("Updated session");

        _sessionRepositoryMock.Verify(x => x.UpdateAsync(session), Times.Once);
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(session), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CreateFromCommand Tests

    [Fact]
    public async Task CreateFromCommand_ShouldCreateSession_WithBasicData()
    {
        // Arrange
        var command = new CreateSessionCommand
        {
            GameId = 1,
            Start = DateTime.UtcNow.AddHours(-2),
            Minutes = 120,
            Comment = "Great game",
            PlayerSessions = new List<CreatePlayerSessionCommand>
            {
                new CreatePlayerSessionCommand { PlayerId = 1, Won = true, Score = 100 }
            }
        };

        _sessionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Session>()))
            .ReturnsAsync((Session s) => s);

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(It.IsAny<Session>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _sessionService.CreateFromCommand(command);

        // Assert
        result.Should().NotBeNull();
        result.GameId.Should().Be(1);
        result.Comment.Should().Be("Great game");
        result.PlayerSessions.Should().HaveCount(1);

        _sessionRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Session>()), Times.Once);
        _badgeServiceMock.Verify(x => x.AwardBadgesAsync(It.IsAny<Session>()), Times.Once);
    }

    [Fact]
    public async Task CreateFromCommand_ShouldAddExpansions_WhenProvided()
    {
        // Arrange
        var expansions = new List<Expansion>
        {
            new Expansion("Expansion 1", 100, 1) { Id = 1 },
            new Expansion("Expansion 2", 101, 1) { Id = 2 }
        };

        var command = new CreateSessionCommand
        {
            GameId = 1,
            Start = DateTime.UtcNow.AddHours(-2),
            Minutes = 120,
            ExpansionIds = new List<int> { 1, 2 },
            PlayerSessions = new List<CreatePlayerSessionCommand>
            {
                new CreatePlayerSessionCommand { PlayerId = 1, Won = true }
            }
        };

        _gameServiceMock
            .Setup(x => x.GetGameExpansions(It.Is<List<int>>(ids => ids.Contains(1) && ids.Contains(2))))
            .ReturnsAsync(expansions);

        _sessionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Session>()))
            .ReturnsAsync((Session s) => s);

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(It.IsAny<Session>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _sessionService.CreateFromCommand(command);

        // Assert
        result.Expansions.Should().HaveCount(2);
        _gameServiceMock.Verify(x => x.GetGameExpansions(It.IsAny<List<int>>()), Times.Once);
    }

    [Fact]
    public async Task CreateFromCommand_ShouldSetLocation_WhenLocationIdProvided()
    {
        // Arrange
        var location = new Location("Game Store") { Id = 5 };
        var locations = new List<Location> { location };

        var command = new CreateSessionCommand
        {
            GameId = 1,
            Start = DateTime.UtcNow.AddHours(-2),
            Minutes = 120,
            LocationId = 5,
            PlayerSessions = new List<CreatePlayerSessionCommand>
            {
                new CreatePlayerSessionCommand { PlayerId = 1, Won = true }
            }
        };

        _locationServiceMock
            .Setup(x => x.GetLocations())
            .ReturnsAsync(locations);

        _sessionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Session>()))
            .ReturnsAsync((Session s) => s);

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(It.IsAny<Session>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _sessionService.CreateFromCommand(command);

        // Assert
        result.LocationId.Should().Be(5);
        _locationServiceMock.Verify(x => x.GetLocations(), Times.Once);
    }

    [Fact]
    public async Task CreateFromCommand_ShouldAddMultiplePlayers()
    {
        // Arrange
        var command = new CreateSessionCommand
        {
            GameId = 1,
            Start = DateTime.UtcNow.AddHours(-2),
            Minutes = 120,
            PlayerSessions = new List<CreatePlayerSessionCommand>
            {
                new CreatePlayerSessionCommand { PlayerId = 1, Won = true, Score = 100 },
                new CreatePlayerSessionCommand { PlayerId = 2, Won = false, Score = 80 },
                new CreatePlayerSessionCommand { PlayerId = 3, Won = false, Score = 60 }
            }
        };

        _sessionRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Session>()))
            .ReturnsAsync((Session s) => s);

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(It.IsAny<Session>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _sessionService.CreateFromCommand(command);

        // Assert
        result.PlayerSessions.Should().HaveCount(3);
    }

    #endregion

    #region UpdateFromCommand Tests

    [Fact]
    public async Task UpdateFromCommand_ShouldUpdateSession_WhenSessionExists()
    {
        // Arrange
        var sessionId = 1;
        var existingSession = new Session(1, DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-1), "Old comment") { Id = sessionId };
        existingSession.AddPlayerSession(1, 50, false, false);

        var command = new UpdateSessionCommand
        {
            Id = sessionId,
            GameId = 1,
            Start = DateTime.UtcNow.AddHours(-2),
            Minutes = 120,
            Comment = "Updated comment",
            PlayerSessions = new List<CreatePlayerSessionCommand>
            {
                new CreatePlayerSessionCommand { PlayerId = 1, Won = true, Score = 100 }
            }
        };

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(sessionId))
            .ReturnsAsync(existingSession);

        _sessionRepositoryMock
            .Setup(x => x.UpdateAsync(existingSession))
            .ReturnsAsync(existingSession);

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(existingSession))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sessionService.UpdateFromCommand(command);

        // Assert
        result.Should().NotBeNull();
        result.Comment.Should().Be("Updated comment");

        _sessionRepositoryMock.Verify(x => x.GetByIdAsync(sessionId), Times.Once);
        _sessionRepositoryMock.Verify(x => x.UpdateAsync(existingSession), Times.Once);
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldThrowException_WhenSessionNotFound()
    {
        // Arrange
        var command = new UpdateSessionCommand
        {
            Id = 999,
            GameId = 1,
            Start = DateTime.UtcNow,
            Minutes = 60,
            PlayerSessions = new List<CreatePlayerSessionCommand>()
        };

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(command.Id))
            .ReturnsAsync((Session?)null);

        // Act
        var action = async () => await _sessionService.UpdateFromCommand(command);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Session with ID 999 not found");
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldAddNewExpansions()
    {
        // Arrange
        var sessionId = 1;
        var existingSession = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Comment") { Id = sessionId };
        existingSession.AddPlayerSession(1, null, false, false);

        var newExpansion = new Expansion("New Expansion", 100, 1) { Id = 5 };

        var command = new UpdateSessionCommand
        {
            Id = sessionId,
            GameId = 1,
            Start = DateTime.UtcNow.AddHours(-2),
            Minutes = 120,
            ExpansionIds = new List<int> { 5 },
            PlayerSessions = new List<CreatePlayerSessionCommand>
            {
                new CreatePlayerSessionCommand { PlayerId = 1, Won = true }
            }
        };

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(sessionId))
            .ReturnsAsync(existingSession);

        _gameServiceMock
            .Setup(x => x.GetGameExpansions(It.Is<List<int>>(ids => ids.Contains(5))))
            .ReturnsAsync(new List<Expansion> { newExpansion });

        _sessionRepositoryMock
            .Setup(x => x.UpdateAsync(existingSession))
            .ReturnsAsync(existingSession);

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(existingSession))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sessionService.UpdateFromCommand(command);

        // Assert
        result.Expansions.Should().Contain(e => e.Id == 5);
        _gameServiceMock.Verify(x => x.GetGameExpansions(It.IsAny<List<int>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldRemovePlayers_NotInCommand()
    {
        // Arrange
        var sessionId = 1;
        var existingSession = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Comment") { Id = sessionId };
        existingSession.AddPlayerSession(1, null, false, false);
        existingSession.AddPlayerSession(2, null, false, false); // This player will be removed

        var command = new UpdateSessionCommand
        {
            Id = sessionId,
            GameId = 1,
            Start = DateTime.UtcNow.AddHours(-2),
            Minutes = 120,
            PlayerSessions = new List<CreatePlayerSessionCommand>
            {
                new CreatePlayerSessionCommand { PlayerId = 1, Won = true } // Only player 1
            }
        };

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(sessionId))
            .ReturnsAsync(existingSession);

        _sessionRepositoryMock
            .Setup(x => x.UpdateAsync(existingSession))
            .ReturnsAsync(existingSession);

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(existingSession))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sessionService.UpdateFromCommand(command);

        // Assert
        result.PlayerSessions.Should().HaveCount(1);
        result.PlayerSessions.Should().NotContain(ps => ps.PlayerId == 2);
    }

    [Fact]
    public async Task UpdateFromCommand_ShouldClearLocation_WhenLocationIdNull()
    {
        // Arrange
        var sessionId = 1;
        var location = new Location("Old Location") { Id = 1 };
        var existingSession = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Comment") { Id = sessionId };
        existingSession.SetLocation(location);
        existingSession.AddPlayerSession(1, null, false, false);

        var command = new UpdateSessionCommand
        {
            Id = sessionId,
            GameId = 1,
            Start = DateTime.UtcNow.AddHours(-2),
            Minutes = 120,
            LocationId = null, // Clear location
            PlayerSessions = new List<CreatePlayerSessionCommand>
            {
                new CreatePlayerSessionCommand { PlayerId = 1, Won = true }
            }
        };

        _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(sessionId))
            .ReturnsAsync(existingSession);

        _sessionRepositoryMock
            .Setup(x => x.UpdateAsync(existingSession))
            .ReturnsAsync(existingSession);

        _badgeServiceMock
            .Setup(x => x.AwardBadgesAsync(existingSession))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sessionService.UpdateFromCommand(command);

        // Assert
        result.LocationId.Should().BeNull();
    }

    #endregion
}
