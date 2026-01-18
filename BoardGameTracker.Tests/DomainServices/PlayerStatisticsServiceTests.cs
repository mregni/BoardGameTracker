using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Players;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.DomainServices;

public class PlayerStatisticsServiceTests
{
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly PlayerStatisticsService _service;

    public PlayerStatisticsServiceTests()
    {
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _service = new PlayerStatisticsService(_playerRepositoryMock.Object);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnBasicStatistics()
    {
        // Arrange
        var playerId = 1;

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(50);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(25);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(3000.0);
        _playerRepositoryMock.Setup(x => x.GetDistinctGameCount(playerId)).ReturnsAsync(15);
        _playerRepositoryMock.Setup(x => x.GetMostPlayedGames(playerId, 5)).ReturnsAsync(new List<Game>());

        // Act
        var result = await _service.CalculateStatisticsAsync(playerId);

        // Assert
        result.PlayCount.Should().Be(50);
        result.WinCount.Should().Be(25);
        result.TotalPlayedTime.Should().Be(3000.0);
        result.DistinctGameCount.Should().Be(15);
        result.MostPlayedGames.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnMostPlayedGames_WithWinningPercentage()
    {
        // Arrange
        var playerId = 1;
        var game1 = new Game("Game 1") { Id = 1 };
        game1.UpdateImage("game1.png");
        var game2 = new Game("Game 2") { Id = 2 };
        game2.UpdateImage("game2.png");

        var mostPlayedGames = new List<Game> { game1, game2 };

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(20);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(1000.0);
        _playerRepositoryMock.Setup(x => x.GetDistinctGameCount(playerId)).ReturnsAsync(5);
        _playerRepositoryMock.Setup(x => x.GetMostPlayedGames(playerId, 5)).ReturnsAsync(mostPlayedGames);

        // Game 1: 10 plays, 5 wins = 50% win rate
        _playerRepositoryMock.Setup(x => x.GetWinCount(playerId, 1)).ReturnsAsync(5);
        _playerRepositoryMock.Setup(x => x.GetPlayCount(playerId, 1)).ReturnsAsync(10);

        // Game 2: 8 plays, 4 wins = 50% win rate
        _playerRepositoryMock.Setup(x => x.GetWinCount(playerId, 2)).ReturnsAsync(4);
        _playerRepositoryMock.Setup(x => x.GetPlayCount(playerId, 2)).ReturnsAsync(8);

        // Act
        var result = await _service.CalculateStatisticsAsync(playerId);

        // Assert
        result.MostPlayedGames.Should().HaveCount(2);

        var firstGame = result.MostPlayedGames[0];
        firstGame.Id.Should().Be(1);
        firstGame.Title.Should().Be("Game 1");
        firstGame.Image.Should().Be("game1.png");
        firstGame.TotalWins.Should().Be(5);
        firstGame.TotalSessions.Should().Be(10);
        firstGame.WinningPercentage.Should().Be(50.0);

        var secondGame = result.MostPlayedGames[1];
        secondGame.Id.Should().Be(2);
        secondGame.TotalWins.Should().Be(4);
        secondGame.TotalSessions.Should().Be(8);
        secondGame.WinningPercentage.Should().Be(50.0);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnZeroWinningPercentage_WhenNoPlays()
    {
        // Arrange
        var playerId = 1;
        var game = new Game("Game with no plays") { Id = 1 };
        var mostPlayedGames = new List<Game> { game };

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetDistinctGameCount(playerId)).ReturnsAsync(1);
        _playerRepositoryMock.Setup(x => x.GetMostPlayedGames(playerId, 5)).ReturnsAsync(mostPlayedGames);
        _playerRepositoryMock.Setup(x => x.GetWinCount(playerId, 1)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetPlayCount(playerId, 1)).ReturnsAsync(0);

        // Act
        var result = await _service.CalculateStatisticsAsync(playerId);

        // Assert
        result.MostPlayedGames[0].WinningPercentage.Should().Be(0);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldHandleGameWithNullImage()
    {
        // Arrange
        var playerId = 1;
        var gameWithoutImage = new Game("Game without image") { Id = 1 };
        // No image set - should default to empty string

        var mostPlayedGames = new List<Game> { gameWithoutImage };

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(5);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(2);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(300.0);
        _playerRepositoryMock.Setup(x => x.GetDistinctGameCount(playerId)).ReturnsAsync(1);
        _playerRepositoryMock.Setup(x => x.GetMostPlayedGames(playerId, 5)).ReturnsAsync(mostPlayedGames);
        _playerRepositoryMock.Setup(x => x.GetWinCount(playerId, 1)).ReturnsAsync(2);
        _playerRepositoryMock.Setup(x => x.GetPlayCount(playerId, 1)).ReturnsAsync(5);

        // Act
        var result = await _service.CalculateStatisticsAsync(playerId);

        // Assert
        result.MostPlayedGames[0].Image.Should().Be(string.Empty);
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldCalculate100PercentWinRate()
    {
        // Arrange
        var playerId = 1;
        var game = new Game("Perfect game") { Id = 1 };
        var mostPlayedGames = new List<Game> { game };

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(600.0);
        _playerRepositoryMock.Setup(x => x.GetDistinctGameCount(playerId)).ReturnsAsync(1);
        _playerRepositoryMock.Setup(x => x.GetMostPlayedGames(playerId, 5)).ReturnsAsync(mostPlayedGames);
        _playerRepositoryMock.Setup(x => x.GetWinCount(playerId, 1)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetPlayCount(playerId, 1)).ReturnsAsync(10);

        // Act
        var result = await _service.CalculateStatisticsAsync(playerId);

        // Assert
        result.MostPlayedGames[0].WinningPercentage.Should().Be(100.0);
    }
}
