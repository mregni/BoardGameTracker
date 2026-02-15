using System.Threading.Tasks;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Compares;
using BoardGameTracker.Core.Compares.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.DomainServices;

public class CompareServiceTests
{
    private readonly Mock<ICompareRepository> _compareRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly Mock<ILogger<CompareService>> _loggerMock;
    private readonly CompareService _service;

    public CompareServiceTests()
    {
        _compareRepositoryMock = new Mock<ICompareRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _loggerMock = new Mock<ILogger<CompareService>>();

        _service = new CompareService(
            _compareRepositoryMock.Object,
            _playerRepositoryMock.Object,
            _loggerMock.Object);
    }

    #region GetPlayerComparison Tests

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnSessionCounts()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerOneId)).ReturnsAsync(50);
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerTwoId)).ReturnsAsync(30);

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.SessionCounts.PlayerOne.Should().Be(50);
        result.SessionCounts.PlayerTwo.Should().Be(30);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnTotalDuration()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerOneId)).ReturnsAsync(1500.5);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerTwoId)).ReturnsAsync(1200.0);

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.TotalDuration.PlayerOne.Should().Be(1500.5);
        result.TotalDuration.PlayerTwo.Should().Be(1200.0);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnWinCount()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerOneId)).ReturnsAsync(25);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerTwoId)).ReturnsAsync(15);

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.WinCount.PlayerOne.Should().Be(25);
        result.WinCount.PlayerTwo.Should().Be(15);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldCalculateWinPercentageCorrectly()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);

        // Player 1: 25 wins out of 50 sessions = 0.5
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerOneId)).ReturnsAsync(50);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerOneId)).ReturnsAsync(25);

        // Player 2: 15 wins out of 30 sessions = 0.5
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerTwoId)).ReturnsAsync(30);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerTwoId)).ReturnsAsync(15);

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.WinPercentage.PlayerOne.Should().Be(0.5);
        result.WinPercentage.PlayerTwo.Should().Be(0.5);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnZeroWinPercentage_WhenNoSessions()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerOneId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerOneId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerTwoId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerTwoId)).ReturnsAsync(0);

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.WinPercentage.PlayerOne.Should().Be(0);
        result.WinPercentage.PlayerTwo.Should().Be(0);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldCalculate100PercentWinRate()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);

        // Player 1: 10 wins out of 10 sessions = 1.0
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerOneId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerOneId)).ReturnsAsync(10);

        // Player 2: 0 wins out of 10 sessions = 0.0
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerTwoId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerTwoId)).ReturnsAsync(0);

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.WinPercentage.PlayerOne.Should().Be(1.0);
        result.WinPercentage.PlayerTwo.Should().Be(0.0);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnDirectWins()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);
        _compareRepositoryMock.Setup(x => x.GetDirectWins(playerOneId, playerTwoId))
            .ReturnsAsync(new CompareRow<int>(5, 3));

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.DirectWins.PlayerOne.Should().Be(5);
        result.DirectWins.PlayerTwo.Should().Be(3);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnMostWonGame_WhenExists()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        var mostWonGamePlayerOne = new MostWonGame { GameId = 1, Count = 10 };
        var mostWonGamePlayerTwo = new MostWonGame { GameId = 2, Count = 8 };
        SetupDefaultMocks(playerOneId, playerTwoId);
        _compareRepositoryMock.Setup(x => x.GetMostWonGame(playerOneId, playerTwoId))
            .ReturnsAsync(new CompareRow<MostWonGame?>(mostWonGamePlayerOne, mostWonGamePlayerTwo));

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.MostWonGame.PlayerOne.Should().NotBeNull();
        result.MostWonGame.PlayerOne!.GameId.Should().Be(1);
        result.MostWonGame.PlayerOne.Count.Should().Be(10);
        result.MostWonGame.PlayerTwo.Should().NotBeNull();
        result.MostWonGame.PlayerTwo!.GameId.Should().Be(2);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnNullMostWonGame_WhenNone()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);
        _compareRepositoryMock.Setup(x => x.GetMostWonGame(playerOneId, playerTwoId))
            .ReturnsAsync(new CompareRow<MostWonGame?>(null, null));

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.MostWonGame.PlayerOne.Should().BeNull();
        result.MostWonGame.PlayerTwo.Should().BeNull();
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnTogetherStats()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);
        _compareRepositoryMock.Setup(x => x.GetTotalSessionsTogether(playerOneId, playerTwoId)).ReturnsAsync(15);
        _compareRepositoryMock.Setup(x => x.GetMinutesPlayedTogether(playerOneId, playerTwoId)).ReturnsAsync(900.0);
        _compareRepositoryMock.Setup(x => x.GetLongestSessionTogether(playerOneId, playerTwoId)).ReturnsAsync(120);

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.TotalSessionsTogether.Should().Be(15);
        result.MinutesPlayed.Should().Be(900);
        result.LongestSessionTogether.Should().Be(120);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldCallAllRepositoryMethods()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);

        // Act
        await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerOneId), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerTwoId), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetPlayLengthInMinutes(playerOneId), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetPlayLengthInMinutes(playerTwoId), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTotalWinCount(playerOneId), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTotalWinCount(playerTwoId), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetDirectWins(playerOneId, playerTwoId), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetMostWonGame(playerOneId, playerTwoId), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetTotalSessionsTogether(playerOneId, playerTwoId), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetMinutesPlayedTogether(playerOneId, playerTwoId), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetPreferredGame(playerOneId, playerTwoId), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetLastWonGame(playerOneId, playerTwoId), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetLongestSessionTogether(playerOneId, playerTwoId), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetFirstGameTogether(playerOneId, playerTwoId), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetClosestGame(playerOneId, playerTwoId), Times.Once);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnCompleteComparison()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerOneId)).ReturnsAsync(100);
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerTwoId)).ReturnsAsync(80);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerOneId)).ReturnsAsync(5000.0);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerTwoId)).ReturnsAsync(4000.0);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerOneId)).ReturnsAsync(40);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerTwoId)).ReturnsAsync(32);
        _compareRepositoryMock.Setup(x => x.GetDirectWins(playerOneId, playerTwoId))
            .ReturnsAsync(new CompareRow<int>(10, 8));
        _compareRepositoryMock.Setup(x => x.GetMostWonGame(playerOneId, playerTwoId))
            .ReturnsAsync(new CompareRow<MostWonGame?>(null, null));
        _compareRepositoryMock.Setup(x => x.GetTotalSessionsTogether(playerOneId, playerTwoId)).ReturnsAsync(20);
        _compareRepositoryMock.Setup(x => x.GetMinutesPlayedTogether(playerOneId, playerTwoId)).ReturnsAsync(1500.0);
        _compareRepositoryMock.Setup(x => x.GetPreferredGame(playerOneId, playerTwoId)).ReturnsAsync((PreferredGame?)null);
        _compareRepositoryMock.Setup(x => x.GetLastWonGame(playerOneId, playerTwoId)).ReturnsAsync((LastWonGame?)null);
        _compareRepositoryMock.Setup(x => x.GetLongestSessionTogether(playerOneId, playerTwoId)).ReturnsAsync(180);
        _compareRepositoryMock.Setup(x => x.GetFirstGameTogether(playerOneId, playerTwoId)).ReturnsAsync((FirstGameTogether?)null);
        _compareRepositoryMock.Setup(x => x.GetClosestGame(playerOneId, playerTwoId)).ReturnsAsync((ClosestGame?)null);

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.SessionCounts.PlayerOne.Should().Be(100);
        result.SessionCounts.PlayerTwo.Should().Be(80);
        result.TotalDuration.PlayerOne.Should().Be(5000.0);
        result.TotalDuration.PlayerTwo.Should().Be(4000.0);
        result.WinCount.PlayerOne.Should().Be(40);
        result.WinCount.PlayerTwo.Should().Be(32);
        result.WinPercentage.PlayerOne.Should().Be(0.4); // 40/100
        result.WinPercentage.PlayerTwo.Should().Be(0.4); // 32/80
        result.DirectWins.PlayerOne.Should().Be(10);
        result.DirectWins.PlayerTwo.Should().Be(8);
        result.TotalSessionsTogether.Should().Be(20);
        result.MinutesPlayed.Should().Be(1500);
        result.LongestSessionTogether.Should().Be(180);
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultMocks(int playerOneId, int playerTwoId)
    {
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerOneId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerTwoId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerOneId)).ReturnsAsync(0.0);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerTwoId)).ReturnsAsync(0.0);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerOneId)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerTwoId)).ReturnsAsync(0);
        _compareRepositoryMock.Setup(x => x.GetDirectWins(playerOneId, playerTwoId))
            .ReturnsAsync(new CompareRow<int>(0, 0));
        _compareRepositoryMock.Setup(x => x.GetMostWonGame(playerOneId, playerTwoId))
            .ReturnsAsync(new CompareRow<MostWonGame?>(null, null));
        _compareRepositoryMock.Setup(x => x.GetTotalSessionsTogether(playerOneId, playerTwoId)).ReturnsAsync(0);
        _compareRepositoryMock.Setup(x => x.GetMinutesPlayedTogether(playerOneId, playerTwoId)).ReturnsAsync(0.0);
        _compareRepositoryMock.Setup(x => x.GetPreferredGame(playerOneId, playerTwoId)).ReturnsAsync((PreferredGame?)null);
        _compareRepositoryMock.Setup(x => x.GetLastWonGame(playerOneId, playerTwoId)).ReturnsAsync((LastWonGame?)null);
        _compareRepositoryMock.Setup(x => x.GetLongestSessionTogether(playerOneId, playerTwoId)).ReturnsAsync((int?)null);
        _compareRepositoryMock.Setup(x => x.GetFirstGameTogether(playerOneId, playerTwoId)).ReturnsAsync((FirstGameTogether?)null);
        _compareRepositoryMock.Setup(x => x.GetClosestGame(playerOneId, playerTwoId)).ReturnsAsync((ClosestGame?)null);
    }

    #endregion
}
