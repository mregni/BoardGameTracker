using System.Threading.Tasks;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Compares;
using BoardGameTracker.Core.Compares.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.DomainServices;

public class PlayerComparisonServiceTests
{
    private readonly Mock<ICompareRepository> _compareRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly PlayerComparisonService _service;

    public PlayerComparisonServiceTests()
    {
        _compareRepositoryMock = new Mock<ICompareRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();

        _service = new PlayerComparisonService(
            _compareRepositoryMock.Object,
            _playerRepositoryMock.Object);
    }

    #region ComparePlayersAsync Tests

    [Fact]
    public async Task ComparePlayersAsync_ShouldReturnPlayerIds()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);

        // Act
        var result = await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        result.PlayerOneId.Should().Be(playerOneId);
        result.PlayerTwoId.Should().Be(playerTwoId);
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldReturnSessionCounts()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerOneId)).ReturnsAsync(50);
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerTwoId)).ReturnsAsync(30);

        // Act
        var result = await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        result.SessionCounts.PlayerOne.Should().Be(50);
        result.SessionCounts.PlayerTwo.Should().Be(30);
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldReturnTotalDuration()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerOneId)).ReturnsAsync(1500.5);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerTwoId)).ReturnsAsync(1200.0);

        // Act
        var result = await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        result.TotalDuration.PlayerOne.Should().Be(1500.5);
        result.TotalDuration.PlayerTwo.Should().Be(1200.0);
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldReturnWinCount()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerOneId)).ReturnsAsync(25);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerTwoId)).ReturnsAsync(15);

        // Act
        var result = await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        result.WinCount.PlayerOne.Should().Be(25);
        result.WinCount.PlayerTwo.Should().Be(15);
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldCalculateWinPercentageCorrectly()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);

        // Player 1: 25 wins out of 50 sessions = 50%
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerOneId)).ReturnsAsync(50);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerOneId)).ReturnsAsync(25);

        // Player 2: 15 wins out of 30 sessions = 50%
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerTwoId)).ReturnsAsync(30);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerTwoId)).ReturnsAsync(15);

        // Act
        var result = await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        result.WinPercentage.PlayerOne.Should().Be(50.0);
        result.WinPercentage.PlayerTwo.Should().Be(50.0);
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldReturnZeroWinPercentage_WhenNoSessions()
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
        var result = await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        result.WinPercentage.PlayerOne.Should().Be(0);
        result.WinPercentage.PlayerTwo.Should().Be(0);
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldCalculate100PercentWinRate()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);

        // Player 1: 10 wins out of 10 sessions = 100%
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerOneId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerOneId)).ReturnsAsync(10);

        // Player 2: 0 wins out of 10 sessions = 0%
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerTwoId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerTwoId)).ReturnsAsync(0);

        // Act
        var result = await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        result.WinPercentage.PlayerOne.Should().Be(100.0);
        result.WinPercentage.PlayerTwo.Should().Be(0.0);
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldReturnDirectWins()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        var directWins = new CompareRow<int>(5, 3);
        SetupDefaultMocks(playerOneId, playerTwoId);
        _compareRepositoryMock.Setup(x => x.GetDirectWins(playerOneId, playerTwoId)).ReturnsAsync(directWins);

        // Act
        var result = await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        result.DirectWins.PlayerOne.Should().Be(5);
        result.DirectWins.PlayerTwo.Should().Be(3);
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldReturnMostWonGame_WhenExists()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        var mostWonGamePlayerOne = new MostWonGame { GameId = 1, Count = 10 };
        var mostWonGamePlayerTwo = new MostWonGame { GameId = 2, Count = 8 };
        var mostWonGame = new CompareRow<MostWonGame?>(mostWonGamePlayerOne, mostWonGamePlayerTwo);
        SetupDefaultMocks(playerOneId, playerTwoId);
        _compareRepositoryMock.Setup(x => x.GetMostWonGame(playerOneId, playerTwoId)).ReturnsAsync(mostWonGame);

        // Act
        var result = await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        result.MostWonGame.PlayerOne.Should().NotBeNull();
        result.MostWonGame.PlayerOne!.GameId.Should().Be(1);
        result.MostWonGame.PlayerOne.Count.Should().Be(10);
        result.MostWonGame.PlayerTwo.Should().NotBeNull();
        result.MostWonGame.PlayerTwo!.GameId.Should().Be(2);
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldReturnNullMostWonGame_WhenNone()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        var mostWonGame = new CompareRow<MostWonGame?>(null, null);
        SetupDefaultMocks(playerOneId, playerTwoId);
        _compareRepositoryMock.Setup(x => x.GetMostWonGame(playerOneId, playerTwoId)).ReturnsAsync(mostWonGame);

        // Act
        var result = await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        result.MostWonGame.PlayerOne.Should().BeNull();
        result.MostWonGame.PlayerTwo.Should().BeNull();
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldCallAllRepositoryMethods()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);

        // Act
        await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerOneId), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerTwoId), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetPlayLengthInMinutes(playerOneId), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetPlayLengthInMinutes(playerTwoId), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTotalWinCount(playerOneId), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTotalWinCount(playerTwoId), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetDirectWins(playerOneId, playerTwoId), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetMostWonGame(playerOneId, playerTwoId), Times.Once);
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldReturnCompleteComparison()
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

        // Act
        var result = await _service.ComparePlayersAsync(playerOneId, playerTwoId);

        // Assert
        result.PlayerOneId.Should().Be(playerOneId);
        result.PlayerTwoId.Should().Be(playerTwoId);
        result.SessionCounts.PlayerOne.Should().Be(100);
        result.SessionCounts.PlayerTwo.Should().Be(80);
        result.TotalDuration.PlayerOne.Should().Be(5000.0);
        result.TotalDuration.PlayerTwo.Should().Be(4000.0);
        result.WinCount.PlayerOne.Should().Be(40);
        result.WinCount.PlayerTwo.Should().Be(32);
        result.WinPercentage.PlayerOne.Should().Be(40.0); // 40/100 * 100
        result.WinPercentage.PlayerTwo.Should().Be(40.0); // 32/80 * 100
        result.DirectWins.PlayerOne.Should().Be(10);
        result.DirectWins.PlayerTwo.Should().Be(8);
    }

    [Fact]
    public async Task ComparePlayersAsync_ShouldHandleSamePlayer()
    {
        // Arrange - comparing player to themselves
        var playerId = 1;
        SetupDefaultMocks(playerId, playerId);
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(50);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(3000.0);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(25);

        // Act
        var result = await _service.ComparePlayersAsync(playerId, playerId);

        // Assert - both sides should have the same values
        result.SessionCounts.PlayerOne.Should().Be(result.SessionCounts.PlayerTwo);
        result.TotalDuration.PlayerOne.Should().Be(result.TotalDuration.PlayerTwo);
        result.WinCount.PlayerOne.Should().Be(result.WinCount.PlayerTwo);
        result.WinPercentage.PlayerOne.Should().Be(result.WinPercentage.PlayerTwo);
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
    }

    #endregion
}
