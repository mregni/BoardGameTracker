using System;
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
    public async Task GetPlayerComparison_ShouldCalculateWinPercentageCorrectly()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        SetupDefaultMocks(playerOneId, playerTwoId);

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerOneId)).ReturnsAsync(50);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerOneId)).ReturnsAsync(25);

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerTwoId)).ReturnsAsync(30);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerTwoId)).ReturnsAsync(10);

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.WinPercentage.PlayerOne.Should().BeApproximately(0.5, 1e-9);
        result.WinPercentage.PlayerTwo.Should().BeApproximately(1.0 / 3.0, 1e-9);
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

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerOneId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerOneId)).ReturnsAsync(10);

        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerTwoId)).ReturnsAsync(10);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerTwoId)).ReturnsAsync(0);

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.WinPercentage.PlayerOne.Should().Be(1.0);
        result.WinPercentage.PlayerTwo.Should().Be(0.0);
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
        result.MostWonGame.PlayerTwo.Count.Should().Be(8);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnSharedGameDetails_WhenPresent()
    {
        // Arrange
        var playerOneId = 1;
        var playerTwoId = 2;
        var preferredGame = new PreferredGame { GameId = 7, SessionCount = 12 };
        var lastWonGame = new LastWonGame { PlayerId = playerOneId, GameId = 8 };
        var firstGameTogether = new FirstGameTogether { GameId = 9, StartDate = new DateTime(2023, 3, 10) };
        var closestGame = new ClosestGame { PlayerId = playerTwoId, GameId = 10, ScoringDifference = 1.5 };

        SetupDefaultMocks(playerOneId, playerTwoId);
        _compareRepositoryMock.Setup(x => x.GetPreferredGame(playerOneId, playerTwoId)).ReturnsAsync(preferredGame);
        _compareRepositoryMock.Setup(x => x.GetLastWonGame(playerOneId, playerTwoId)).ReturnsAsync(lastWonGame);
        _compareRepositoryMock.Setup(x => x.GetFirstGameTogether(playerOneId, playerTwoId)).ReturnsAsync(firstGameTogether);
        _compareRepositoryMock.Setup(x => x.GetClosestGame(playerOneId, playerTwoId)).ReturnsAsync(closestGame);

        // Act
        var result = await _service.GetPlayerComparison(playerOneId, playerTwoId);

        // Assert
        result.PreferredGame.Should().NotBeNull();
        result.PreferredGame!.GameId.Should().Be(7);
        result.PreferredGame.SessionCount.Should().Be(12);
        result.LastWonGame.Should().NotBeNull();
        result.LastWonGame!.PlayerId.Should().Be(playerOneId);
        result.LastWonGame.GameId.Should().Be(8);
        result.FirstGameTogether.Should().NotBeNull();
        result.FirstGameTogether!.GameId.Should().Be(9);
        result.FirstGameTogether.StartDate.Should().Be(new DateTime(2023, 3, 10));
        result.ClosestGame.Should().NotBeNull();
        result.ClosestGame!.PlayerId.Should().Be(playerTwoId);
        result.ClosestGame.GameId.Should().Be(10);
        result.ClosestGame.ScoringDifference.Should().BeApproximately(1.5, 1e-9);
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
        result.WinPercentage.PlayerOne.Should().BeApproximately(0.4, 1e-9);
        result.WinPercentage.PlayerTwo.Should().BeApproximately(0.4, 1e-9);
        result.DirectWins.PlayerOne.Should().Be(10);
        result.DirectWins.PlayerTwo.Should().Be(8);
        result.MostWonGame.PlayerOne.Should().BeNull();
        result.MostWonGame.PlayerTwo.Should().BeNull();
        result.TotalSessionsTogether.Should().Be(20);
        result.MinutesPlayed.Should().Be(1500);
        result.LongestSessionTogether.Should().Be(180);
        result.PreferredGame.Should().BeNull();
        result.LastWonGame.Should().BeNull();
        result.FirstGameTogether.Should().BeNull();
        result.ClosestGame.Should().BeNull();
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
