using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Sessions;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class FirstPlayDetectionServiceTests
{
    private readonly Mock<IGameSessionRepository> _gameSessionRepositoryMock;
    private readonly FirstPlayDetectionService _service;

    public FirstPlayDetectionServiceTests()
    {
        _gameSessionRepositoryMock = new Mock<IGameSessionRepository>();
        _service = new FirstPlayDetectionService(_gameSessionRepositoryMock.Object);
    }

    #region IsFirstPlayAsync (by IDs) Tests

    [Fact]
    public async Task IsFirstPlayAsync_ShouldReturnTrue_WhenPlayerHasNoSessionsForGame()
    {
        // Arrange
        var playerId = 1;
        var gameId = 5;

        // Player has sessions for other games, but not for game 5
        var sessions = new List<Session>
        {
            CreateSession(1, 10), // Different game
            CreateSession(2, 20)  // Different game
        };

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(playerId, null))
            .ReturnsAsync(sessions);

        // Act
        var result = await _service.IsFirstPlayAsync(playerId, gameId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFirstPlayAsync_ShouldReturnFalse_WhenPlayerHasSessionForGame()
    {
        // Arrange
        var playerId = 1;
        var gameId = 5;

        var sessions = new List<Session>
        {
            CreateSession(1, 5), // Has session for game 5
            CreateSession(2, 10)
        };

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(playerId, null))
            .ReturnsAsync(sessions);

        // Act
        var result = await _service.IsFirstPlayAsync(playerId, gameId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsFirstPlayAsync_ShouldReturnTrue_WhenPlayerHasNoSessions()
    {
        // Arrange
        var playerId = 1;
        var gameId = 5;

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(playerId, null))
            .ReturnsAsync(new List<Session>());

        // Act
        var result = await _service.IsFirstPlayAsync(playerId, gameId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFirstPlayAsync_ShouldCallRepositoryWithCorrectPlayerId()
    {
        // Arrange
        var playerId = 42;
        var gameId = 5;

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(playerId, null))
            .ReturnsAsync(new List<Session>());

        // Act
        await _service.IsFirstPlayAsync(playerId, gameId);

        // Assert
        _gameSessionRepositoryMock.Verify(x => x.GetSessionsByPlayerId(playerId, null), Times.Once);
    }

    #endregion

    #region IsFirstPlayAsync (by entities) Tests

    [Fact]
    public async Task IsFirstPlayAsync_WithEntities_ShouldReturnTrue_WhenFirstPlay()
    {
        // Arrange
        var player = new Player("Test Player") { Id = 1 };
        var game = new Game("Test Game") { Id = 5 };

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(player.Id, null))
            .ReturnsAsync(new List<Session>());

        // Act
        var result = await _service.IsFirstPlayAsync(player, game);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFirstPlayAsync_WithEntities_ShouldReturnFalse_WhenNotFirstPlay()
    {
        // Arrange
        var player = new Player("Test Player") { Id = 1 };
        var game = new Game("Test Game") { Id = 5 };

        var sessions = new List<Session> { CreateSession(1, game.Id) };

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(player.Id, null))
            .ReturnsAsync(sessions);

        // Act
        var result = await _service.IsFirstPlayAsync(player, game);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsFirstPlayAsync_WithEntities_ShouldUseEntityIds()
    {
        // Arrange
        var player = new Player("Test Player") { Id = 10 };
        var game = new Game("Test Game") { Id = 20 };

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(10, null))
            .ReturnsAsync(new List<Session>());

        // Act
        await _service.IsFirstPlayAsync(player, game);

        // Assert
        _gameSessionRepositoryMock.Verify(x => x.GetSessionsByPlayerId(10, null), Times.Once);
    }

    #endregion

    #region GetFirstTimePlayerIdsAsync Tests

    [Fact]
    public async Task GetFirstTimePlayerIdsAsync_ShouldReturnAllPlayers_WhenAllAreFirstTime()
    {
        // Arrange
        var gameId = 5;
        var playerIds = new[] { 1, 2, 3 };

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(1, null))
            .ReturnsAsync(new List<Session>());
        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(2, null))
            .ReturnsAsync(new List<Session>());
        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(3, null))
            .ReturnsAsync(new List<Session>());

        // Act
        var result = await _service.GetFirstTimePlayerIdsAsync(gameId, playerIds);

        // Assert
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task GetFirstTimePlayerIdsAsync_ShouldReturnEmpty_WhenNoneAreFirstTime()
    {
        // Arrange
        var gameId = 5;
        var playerIds = new[] { 1, 2 };

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(1, null))
            .ReturnsAsync(new List<Session> { CreateSession(1, gameId) });
        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(2, null))
            .ReturnsAsync(new List<Session> { CreateSession(2, gameId) });

        // Act
        var result = await _service.GetFirstTimePlayerIdsAsync(gameId, playerIds);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFirstTimePlayerIdsAsync_ShouldReturnOnlyFirstTimePlayers()
    {
        // Arrange
        var gameId = 5;
        var playerIds = new[] { 1, 2, 3 };

        // Player 1 has played before
        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(1, null))
            .ReturnsAsync(new List<Session> { CreateSession(1, gameId) });

        // Player 2 is first time
        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(2, null))
            .ReturnsAsync(new List<Session>());

        // Player 3 has played before
        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(3, null))
            .ReturnsAsync(new List<Session> { CreateSession(3, gameId) });

        // Act
        var result = await _service.GetFirstTimePlayerIdsAsync(gameId, playerIds);

        // Assert
        result.Should().ContainSingle().Which.Should().Be(2);
    }

    [Fact]
    public async Task GetFirstTimePlayerIdsAsync_ShouldReturnEmpty_WhenNoPlayerIds()
    {
        // Arrange
        var gameId = 5;
        var playerIds = Array.Empty<int>();

        // Act
        var result = await _service.GetFirstTimePlayerIdsAsync(gameId, playerIds);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFirstTimePlayerIdsAsync_ShouldCheckEachPlayer()
    {
        // Arrange
        var gameId = 5;
        var playerIds = new[] { 1, 2, 3 };

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(It.IsAny<int>(), null))
            .ReturnsAsync(new List<Session>());

        // Act
        await _service.GetFirstTimePlayerIdsAsync(gameId, playerIds);

        // Assert
        _gameSessionRepositoryMock.Verify(x => x.GetSessionsByPlayerId(1, null), Times.Once);
        _gameSessionRepositoryMock.Verify(x => x.GetSessionsByPlayerId(2, null), Times.Once);
        _gameSessionRepositoryMock.Verify(x => x.GetSessionsByPlayerId(3, null), Times.Once);
    }

    [Fact]
    public async Task GetFirstTimePlayerIdsAsync_ShouldConsiderOtherGamesAsFirstTime()
    {
        // Arrange
        var gameId = 5;
        var playerIds = new[] { 1 };

        // Player has played other games but not game 5
        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(1, null))
            .ReturnsAsync(new List<Session>
            {
                CreateSession(1, 10), // Different game
                CreateSession(2, 20)  // Different game
            });

        // Act
        var result = await _service.GetFirstTimePlayerIdsAsync(gameId, playerIds);

        // Assert
        result.Should().ContainSingle().Which.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private static Session CreateSession(int sessionId, int gameId)
    {
        var session = new Session(gameId, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Test session");
        // Use reflection to set the Id since it's from HasId base class
        typeof(Session).GetProperty("Id")!.SetValue(session, sessionId);
        return session;
    }

    #endregion
}
