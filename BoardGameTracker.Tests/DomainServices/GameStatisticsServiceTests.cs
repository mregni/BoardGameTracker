using System;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.DomainServices;

public class GameStatisticsServiceTests
{
    private readonly Mock<IGameSessionRepository> _gameSessionRepositoryMock;
    private readonly Mock<IGameStatisticsRepository> _gameStatisticsRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly GameStatisticsService _service;

    public GameStatisticsServiceTests()
    {
        _gameSessionRepositoryMock = new Mock<IGameSessionRepository>();
        _gameStatisticsRepositoryMock = new Mock<IGameStatisticsRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();

        _service = new GameStatisticsService(
            _gameSessionRepositoryMock.Object,
            _gameStatisticsRepositoryMock.Object,
            _playerRepositoryMock.Object);
    }

    #region CalculateStatisticsAsync Tests

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnPlayCount()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameSessionRepositoryMock.Setup(x => x.GetPlayCount(gameId)).ReturnsAsync(25);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.PlayCount.Should().Be(25);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnTotalPlayedTime()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameSessionRepositoryMock.Setup(x => x.GetTotalPlayedTime(gameId)).ReturnsAsync(1500.5);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.TotalPlayedTime.Should().Be(1500.5);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnPricePerPlay()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetPricePerPlay(gameId)).ReturnsAsync(2.50);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.PricePerPlay.Should().Be(2.50);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnNullPricePerPlay_WhenNotAvailable()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetPricePerPlay(gameId)).ReturnsAsync((double?)null);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.PricePerPlay.Should().BeNull();
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnHighScore()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetHighestScore(gameId)).ReturnsAsync(185.5);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.HighScore.Should().Be(185.5);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnNullHighScore_WhenNotAvailable()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetHighestScore(gameId)).ReturnsAsync((double?)null);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.HighScore.Should().BeNull();
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnAveragePlayTime()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetAveragePlayTime(gameId)).ReturnsAsync(75.5);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.AveragePlayTime.Should().Be(75.5);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnAverageScore()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetAverageScore(gameId)).ReturnsAsync(120.5);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.AverageScore.Should().Be(120.5);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnLastPlayed()
    {
        // Arrange
        var gameId = 1;
        var lastPlayed = new DateTime(2024, 1, 15, 18, 30, 0);
        SetupDefaultRepositoryMocks(gameId);
        _gameSessionRepositoryMock.Setup(x => x.GetLastPlayedDateTime(gameId)).ReturnsAsync(lastPlayed);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.LastPlayed.Should().Be(lastPlayed);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnNullLastPlayed_WhenNeverPlayed()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameSessionRepositoryMock.Setup(x => x.GetLastPlayedDateTime(gameId)).ReturnsAsync((DateTime?)null);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.LastPlayed.Should().BeNull();
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnExpansionCount()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetExpansionCount(gameId)).ReturnsAsync(3);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.ExpansionCount.Should().Be(3);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnMostWinsPlayer_WhenPlayerExists()
    {
        // Arrange
        var gameId = 1;
        var playerId = 5;
        var player = new Player("John Doe", "john.jpg") { Id = playerId };

        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostWins(gameId)).ReturnsAsync(player);
        _playerRepositoryMock.Setup(x => x.GetWinCount(playerId, gameId)).ReturnsAsync(15);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.MostWinsPlayer.Should().NotBeNull();
        result.MostWinsPlayer!.Id.Should().Be(playerId);
        result.MostWinsPlayer.Name.Should().Be("John Doe");
        result.MostWinsPlayer.Image.Should().Be("john.jpg");
        result.MostWinsPlayer.TotalWins.Should().Be(15);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnNullMostWinsPlayer_WhenNoPlayerExists()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostWins(gameId)).ReturnsAsync((Player?)null);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.MostWinsPlayer.Should().BeNull();
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldUseEmptyString_WhenPlayerImageIsNull()
    {
        // Arrange
        var gameId = 1;
        var playerId = 5;
        var player = new Player("Jane Doe") { Id = playerId }; // No image

        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostWins(gameId)).ReturnsAsync(player);
        _playerRepositoryMock.Setup(x => x.GetWinCount(playerId, gameId)).ReturnsAsync(10);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.MostWinsPlayer.Should().NotBeNull();
        result.MostWinsPlayer!.Image.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldCallAllRepositoryMethods()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);

        // Act
        await _service.CalculateStatisticsAsync(gameId);

        // Assert
        _gameSessionRepositoryMock.Verify(x => x.GetPlayCount(gameId), Times.Once);
        _gameSessionRepositoryMock.Verify(x => x.GetTotalPlayedTime(gameId), Times.Once);
        _gameSessionRepositoryMock.Verify(x => x.GetLastPlayedDateTime(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetPricePerPlay(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestScore(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetAveragePlayTime(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetAverageScore(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetExpansionCount(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMostWins(gameId), Times.Once);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldNotCallGetWinCount_WhenNoMostWinsPlayer()
    {
        // Arrange
        var gameId = 1;
        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostWins(gameId)).ReturnsAsync((Player?)null);

        // Act
        await _service.CalculateStatisticsAsync(gameId);

        // Assert
        _playerRepositoryMock.Verify(x => x.GetWinCount(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnCompleteStatistics()
    {
        // Arrange
        var gameId = 1;
        var lastPlayed = new DateTime(2024, 6, 15);
        var player = new Player("Winner", "winner.jpg") { Id = 10 };

        _gameSessionRepositoryMock.Setup(x => x.GetPlayCount(gameId)).ReturnsAsync(50);
        _gameSessionRepositoryMock.Setup(x => x.GetTotalPlayedTime(gameId)).ReturnsAsync(3000.0);
        _gameSessionRepositoryMock.Setup(x => x.GetLastPlayedDateTime(gameId)).ReturnsAsync(lastPlayed);
        _gameStatisticsRepositoryMock.Setup(x => x.GetPricePerPlay(gameId)).ReturnsAsync(1.50);
        _gameStatisticsRepositoryMock.Setup(x => x.GetHighestScore(gameId)).ReturnsAsync(250.0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetAveragePlayTime(gameId)).ReturnsAsync(60.0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetAverageScore(gameId)).ReturnsAsync(150.0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetExpansionCount(gameId)).ReturnsAsync(5);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostWins(gameId)).ReturnsAsync(player);
        _playerRepositoryMock.Setup(x => x.GetWinCount(10, gameId)).ReturnsAsync(20);

        // Act
        var result = await _service.CalculateStatisticsAsync(gameId);

        // Assert
        result.PlayCount.Should().Be(50);
        result.TotalPlayedTime.Should().Be(3000.0);
        result.LastPlayed.Should().Be(lastPlayed);
        result.PricePerPlay.Should().Be(1.50);
        result.HighScore.Should().Be(250.0);
        result.AveragePlayTime.Should().Be(60.0);
        result.AverageScore.Should().Be(150.0);
        result.ExpansionCount.Should().Be(5);
        result.MostWinsPlayer.Should().NotBeNull();
        result.MostWinsPlayer!.Id.Should().Be(10);
        result.MostWinsPlayer.Name.Should().Be("Winner");
        result.MostWinsPlayer.TotalWins.Should().Be(20);
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultRepositoryMocks(int gameId)
    {
        _gameSessionRepositoryMock.Setup(x => x.GetPlayCount(gameId)).ReturnsAsync(0);
        _gameSessionRepositoryMock.Setup(x => x.GetTotalPlayedTime(gameId)).ReturnsAsync(0);
        _gameSessionRepositoryMock.Setup(x => x.GetLastPlayedDateTime(gameId)).ReturnsAsync((DateTime?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetPricePerPlay(gameId)).ReturnsAsync((double?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetHighestScore(gameId)).ReturnsAsync((double?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetAveragePlayTime(gameId)).ReturnsAsync(0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetAverageScore(gameId)).ReturnsAsync((double?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetExpansionCount(gameId)).ReturnsAsync((int?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostWins(gameId)).ReturnsAsync((Player?)null);
    }

    #endregion
}
