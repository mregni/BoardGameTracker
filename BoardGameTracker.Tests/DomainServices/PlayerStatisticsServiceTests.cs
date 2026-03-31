using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Players;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.DomainServices;

public class PlayerStatisticsServiceTests
{
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly Mock<ILogger<PlayerStatisticsService>> _loggerMock;
    private readonly PlayerStatisticsService _service;

    public PlayerStatisticsServiceTests()
    {
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _loggerMock = new Mock<ILogger<PlayerStatisticsService>>();
        _service = new PlayerStatisticsService(_playerRepositoryMock.Object, _loggerMock.Object);
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
        _playerRepositoryMock.Setup(x => x.GetMostPlayedGames(playerId, 5)).ReturnsAsync([]);

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
        var mostPlayedGames = new List<MostPlayedGame>
        {
            new() { Id = 1, Title = "Game 1", Image = "game1.png", TotalSessions = 10, TotalWins = 5, WinningPercentage = 50.0 },
            new() { Id = 2, Title = "Game 2", Image = "game2.png", TotalSessions = 8, TotalWins = 4, WinningPercentage = 50.0 }
        };

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(20);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(1000.0);
        _playerRepositoryMock.Setup(x => x.GetDistinctGameCount(playerId)).ReturnsAsync(5);
        _playerRepositoryMock.Setup(x => x.GetMostPlayedGames(playerId, 5)).ReturnsAsync(mostPlayedGames);

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
        var mostPlayedGames = new List<MostPlayedGame>
        {
            new() { Id = 1, Title = "Game with no plays", TotalSessions = 0, TotalWins = 0, WinningPercentage = 0 }
        };

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetDistinctGameCount(playerId)).ReturnsAsync(1);
        _playerRepositoryMock.Setup(x => x.GetMostPlayedGames(playerId, 5)).ReturnsAsync(mostPlayedGames);

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
        var mostPlayedGames = new List<MostPlayedGame>
        {
            new() { Id = 1, Title = "Game without image", Image = string.Empty, TotalSessions = 5, TotalWins = 2, WinningPercentage = 40.0 }
        };

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(5);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(2);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(300.0);
        _playerRepositoryMock.Setup(x => x.GetDistinctGameCount(playerId)).ReturnsAsync(1);
        _playerRepositoryMock.Setup(x => x.GetMostPlayedGames(playerId, 5)).ReturnsAsync(mostPlayedGames);

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
        var mostPlayedGames = new List<MostPlayedGame>
        {
            new() { Id = 1, Title = "Perfect game", TotalSessions = 10, TotalWins = 10, WinningPercentage = 100.0 }
        };

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(600.0);
        _playerRepositoryMock.Setup(x => x.GetDistinctGameCount(playerId)).ReturnsAsync(1);
        _playerRepositoryMock.Setup(x => x.GetMostPlayedGames(playerId, 5)).ReturnsAsync(mostPlayedGames);

        // Act
        var result = await _service.CalculateStatisticsAsync(playerId);

        // Assert
        result.MostPlayedGames[0].WinningPercentage.Should().Be(100.0);
    }
}
