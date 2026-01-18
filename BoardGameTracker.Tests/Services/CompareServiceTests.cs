using System;
using System.Threading.Tasks;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Compares;
using BoardGameTracker.Core.Compares.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class CompareServiceTests
{
    private readonly Mock<ICompareRepository> _compareRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly CompareService _compareService;

    public CompareServiceTests()
    {
        _compareRepositoryMock = new Mock<ICompareRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _compareService = new CompareService(_compareRepositoryMock.Object, _playerRepositoryMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _compareRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnCompareResult_WithAllData()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.5, winCount: 5);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 8, duration: 400.0, winCount: 3);
        SetupCompareRepositoryMocks(playerOne, playerTwo);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.Should().NotBeNull();
        result.SessionCounts.PlayerOne.Should().Be(10);
        result.SessionCounts.PlayerTwo.Should().Be(8);
        result.TotalDuration.PlayerOne.Should().Be(500.5);
        result.TotalDuration.PlayerTwo.Should().Be(400.0);
        result.WinCount.PlayerOne.Should().Be(5);
        result.WinCount.PlayerTwo.Should().Be(3);
        result.WinPercentage.PlayerOne.Should().Be(0.5); // 5/10
        result.WinPercentage.PlayerTwo.Should().Be(0.375); // 3/8
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnZeroWinPercentage_WhenPlayerHasNoSessions()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 0, duration: 0, winCount: 0);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 5, duration: 200.0, winCount: 2);
        SetupCompareRepositoryMocks(playerOne, playerTwo);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.WinPercentage.PlayerOne.Should().Be(0);
        result.WinPercentage.PlayerTwo.Should().Be(0.4); // 2/5
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnDirectWins_FromRepository()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;
        var expectedDirectWins = new CompareRow<int>(7, 3);

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.0, winCount: 7);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 10, duration: 500.0, winCount: 3);

        _compareRepositoryMock
            .Setup(x => x.GetDirectWins(playerOne, playerTwo))
            .ReturnsAsync(expectedDirectWins);
        SetupOtherCompareRepositoryMocks(playerOne, playerTwo);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.DirectWins.PlayerOne.Should().Be(7);
        result.DirectWins.PlayerTwo.Should().Be(3);
        _compareRepositoryMock.Verify(x => x.GetDirectWins(playerOne, playerTwo), Times.Once);
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnMostWonGame_FromRepository()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;
        var mostWonGame = new CompareRow<MostWonGame?>(
            new MostWonGame { GameId = 1, Count = 5 },
            new MostWonGame { GameId = 2, Count = 3 });

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 10, duration: 500.0, winCount: 3);

        _compareRepositoryMock
            .Setup(x => x.GetMostWonGame(playerOne, playerTwo))
            .ReturnsAsync(mostWonGame);
        SetupOtherCompareRepositoryMocksExceptMostWonGame(playerOne, playerTwo);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.MostWonGame.PlayerOne.Should().NotBeNull();
        result.MostWonGame.PlayerOne!.GameId.Should().Be(1);
        result.MostWonGame.PlayerOne.Count.Should().Be(5);
        result.MostWonGame.PlayerTwo.Should().NotBeNull();
        result.MostWonGame.PlayerTwo!.GameId.Should().Be(2);
        result.MostWonGame.PlayerTwo.Count.Should().Be(3);
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnTotalSessionsTogether()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;
        var expectedSessionsTogether = 15;

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 20, duration: 1000.0, winCount: 10);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 18, duration: 900.0, winCount: 8);

        _compareRepositoryMock
            .Setup(x => x.GetTotalSessionsTogether(playerOne, playerTwo))
            .ReturnsAsync(expectedSessionsTogether);
        SetupOtherCompareRepositoryMocksExceptSessionsTogether(playerOne, playerTwo);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.TotalSessionsTogether.Should().Be(15);
        _compareRepositoryMock.Verify(x => x.GetTotalSessionsTogether(playerOne, playerTwo), Times.Once);
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnMinutesPlayed_AsInteger()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;
        var minutesPlayedTogether = 750.7; // Should be truncated to 750

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 10, duration: 500.0, winCount: 5);

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(minutesPlayedTogether);
        SetupOtherCompareRepositoryMocksExceptMinutesPlayed(playerOne, playerTwo);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.MinutesPlayed.Should().Be(750);
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnPreferredGame()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;
        var preferredGame = new PreferredGame { GameId = 5, SessionCount = 10 };

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 10, duration: 500.0, winCount: 5);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync(preferredGame);
        SetupOtherCompareRepositoryMocksExceptPreferredGame(playerOne, playerTwo);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.PreferredGame.Should().NotBeNull();
        result.PreferredGame!.GameId.Should().Be(5);
        result.PreferredGame.SessionCount.Should().Be(10);
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnNullPreferredGame_WhenNoSharedGames()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 10, duration: 500.0, winCount: 5);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync((PreferredGame?)null);
        SetupOtherCompareRepositoryMocksExceptPreferredGame(playerOne, playerTwo);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.PreferredGame.Should().BeNull();
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnLastWonGame()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;
        var lastWonGame = new LastWonGame { PlayerId = playerOne, GameId = 3 };

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupCompareRepositoryMocksWithLastWonGame(playerOne, playerTwo, lastWonGame);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.LastWonGame.Should().NotBeNull();
        result.LastWonGame!.PlayerId.Should().Be(playerOne);
        result.LastWonGame.GameId.Should().Be(3);
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnLongestSessionTogether()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;
        var longestSession = 180; // 3 hours

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 10, duration: 500.0, winCount: 5);

        _compareRepositoryMock
            .Setup(x => x.GetLongestSessionTogether(playerOne, playerTwo))
            .ReturnsAsync(longestSession);
        SetupOtherCompareRepositoryMocksExceptLongestSession(playerOne, playerTwo);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.LongestSessionTogether.Should().Be(180);
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnFirstGameTogether()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;
        var firstGame = new FirstGameTogether { GameId = 7, StartDate = new DateTime(2020, 1, 15) };

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupCompareRepositoryMocksWithFirstGameTogether(playerOne, playerTwo, firstGame);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.FirstGameTogether.Should().NotBeNull();
        result.FirstGameTogether!.GameId.Should().Be(7);
        result.FirstGameTogether.StartDate.Should().Be(new DateTime(2020, 1, 15));
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldReturnClosestGame()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;
        var closestGame = new ClosestGame { PlayerId = playerTwo, GameId = 4, ScoringDifference = 1.5 };

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupCompareRepositoryMocksWithClosestGame(playerOne, playerTwo, closestGame);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerOne, playerTwo);

        // Assert
        result.ClosestGame.Should().NotBeNull();
        result.ClosestGame!.PlayerId.Should().Be(playerTwo);
        result.ClosestGame.GameId.Should().Be(4);
        result.ClosestGame.ScoringDifference.Should().Be(1.5);
    }

    [Fact]
    public async Task GetPlayerComparisation_ShouldHandleSamePlayerComparison()
    {
        // Arrange
        var playerId = 1;

        SetupPlayerRepositoryMocks(playerId, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupCompareRepositoryMocks(playerId, playerId);

        // Act
        var result = await _compareService.GetPlayerComparisation(playerId, playerId);

        // Assert
        result.SessionCounts.PlayerOne.Should().Be(result.SessionCounts.PlayerTwo);
        result.WinCount.PlayerOne.Should().Be(result.WinCount.PlayerTwo);
        result.TotalDuration.PlayerOne.Should().Be(result.TotalDuration.PlayerTwo);
    }

    #region Helper Methods

    private void SetupPlayerRepositoryMocks(int playerId, int sessionCount, double duration, int winCount)
    {
        _playerRepositoryMock
            .Setup(x => x.GetTotalPlayCount(playerId))
            .ReturnsAsync(sessionCount);

        _playerRepositoryMock
            .Setup(x => x.GetPlayLengthInMinutes(playerId))
            .ReturnsAsync(duration);

        _playerRepositoryMock
            .Setup(x => x.GetTotalWinCount(playerId))
            .ReturnsAsync(winCount);
    }

    private void SetupCompareRepositoryMocks(int playerOne, int playerTwo)
    {
        _compareRepositoryMock
            .Setup(x => x.GetDirectWins(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<int>(0, 0));

        _compareRepositoryMock
            .Setup(x => x.GetMostWonGame(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<MostWonGame?>(new MostWonGame(), new MostWonGame()));

        _compareRepositoryMock
            .Setup(x => x.GetTotalSessionsTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync((PreferredGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLastWonGame(playerOne, playerTwo))
            .ReturnsAsync((LastWonGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLongestSessionTogether(playerOne, playerTwo))
            .ReturnsAsync((int?)null);

        _compareRepositoryMock
            .Setup(x => x.GetFirstGameTogether(playerOne, playerTwo))
            .ReturnsAsync((FirstGameTogether?)null);

        _compareRepositoryMock
            .Setup(x => x.GetClosestGame(playerOne, playerTwo))
            .ReturnsAsync((ClosestGame?)null);
    }

    private void SetupOtherCompareRepositoryMocks(int playerOne, int playerTwo)
    {
        _compareRepositoryMock
            .Setup(x => x.GetMostWonGame(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<MostWonGame?>(new MostWonGame(), new MostWonGame()));

        _compareRepositoryMock
            .Setup(x => x.GetTotalSessionsTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync((PreferredGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLastWonGame(playerOne, playerTwo))
            .ReturnsAsync((LastWonGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLongestSessionTogether(playerOne, playerTwo))
            .ReturnsAsync((int?)null);

        _compareRepositoryMock
            .Setup(x => x.GetFirstGameTogether(playerOne, playerTwo))
            .ReturnsAsync((FirstGameTogether?)null);

        _compareRepositoryMock
            .Setup(x => x.GetClosestGame(playerOne, playerTwo))
            .ReturnsAsync((ClosestGame?)null);
    }

    private void SetupOtherCompareRepositoryMocksExceptMostWonGame(int playerOne, int playerTwo)
    {
        _compareRepositoryMock
            .Setup(x => x.GetDirectWins(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<int>(0, 0));

        _compareRepositoryMock
            .Setup(x => x.GetTotalSessionsTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync((PreferredGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLastWonGame(playerOne, playerTwo))
            .ReturnsAsync((LastWonGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLongestSessionTogether(playerOne, playerTwo))
            .ReturnsAsync((int?)null);

        _compareRepositoryMock
            .Setup(x => x.GetFirstGameTogether(playerOne, playerTwo))
            .ReturnsAsync((FirstGameTogether?)null);

        _compareRepositoryMock
            .Setup(x => x.GetClosestGame(playerOne, playerTwo))
            .ReturnsAsync((ClosestGame?)null);
    }

    private void SetupOtherCompareRepositoryMocksExceptSessionsTogether(int playerOne, int playerTwo)
    {
        _compareRepositoryMock
            .Setup(x => x.GetDirectWins(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<int>(0, 0));

        _compareRepositoryMock
            .Setup(x => x.GetMostWonGame(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<MostWonGame?>(new MostWonGame(), new MostWonGame()));

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync((PreferredGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLastWonGame(playerOne, playerTwo))
            .ReturnsAsync((LastWonGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLongestSessionTogether(playerOne, playerTwo))
            .ReturnsAsync((int?)null);

        _compareRepositoryMock
            .Setup(x => x.GetFirstGameTogether(playerOne, playerTwo))
            .ReturnsAsync((FirstGameTogether?)null);

        _compareRepositoryMock
            .Setup(x => x.GetClosestGame(playerOne, playerTwo))
            .ReturnsAsync((ClosestGame?)null);
    }

    private void SetupOtherCompareRepositoryMocksExceptMinutesPlayed(int playerOne, int playerTwo)
    {
        _compareRepositoryMock
            .Setup(x => x.GetDirectWins(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<int>(0, 0));

        _compareRepositoryMock
            .Setup(x => x.GetMostWonGame(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<MostWonGame?>(new MostWonGame(), new MostWonGame()));

        _compareRepositoryMock
            .Setup(x => x.GetTotalSessionsTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync((PreferredGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLastWonGame(playerOne, playerTwo))
            .ReturnsAsync((LastWonGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLongestSessionTogether(playerOne, playerTwo))
            .ReturnsAsync((int?)null);

        _compareRepositoryMock
            .Setup(x => x.GetFirstGameTogether(playerOne, playerTwo))
            .ReturnsAsync((FirstGameTogether?)null);

        _compareRepositoryMock
            .Setup(x => x.GetClosestGame(playerOne, playerTwo))
            .ReturnsAsync((ClosestGame?)null);
    }

    private void SetupOtherCompareRepositoryMocksExceptPreferredGame(int playerOne, int playerTwo)
    {
        _compareRepositoryMock
            .Setup(x => x.GetDirectWins(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<int>(0, 0));

        _compareRepositoryMock
            .Setup(x => x.GetMostWonGame(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<MostWonGame?>(new MostWonGame(), new MostWonGame()));

        _compareRepositoryMock
            .Setup(x => x.GetTotalSessionsTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetLastWonGame(playerOne, playerTwo))
            .ReturnsAsync((LastWonGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLongestSessionTogether(playerOne, playerTwo))
            .ReturnsAsync((int?)null);

        _compareRepositoryMock
            .Setup(x => x.GetFirstGameTogether(playerOne, playerTwo))
            .ReturnsAsync((FirstGameTogether?)null);

        _compareRepositoryMock
            .Setup(x => x.GetClosestGame(playerOne, playerTwo))
            .ReturnsAsync((ClosestGame?)null);
    }

    private void SetupOtherCompareRepositoryMocksExceptLongestSession(int playerOne, int playerTwo)
    {
        _compareRepositoryMock
            .Setup(x => x.GetDirectWins(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<int>(0, 0));

        _compareRepositoryMock
            .Setup(x => x.GetMostWonGame(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<MostWonGame?>(new MostWonGame(), new MostWonGame()));

        _compareRepositoryMock
            .Setup(x => x.GetTotalSessionsTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync((PreferredGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLastWonGame(playerOne, playerTwo))
            .ReturnsAsync((LastWonGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetFirstGameTogether(playerOne, playerTwo))
            .ReturnsAsync((FirstGameTogether?)null);

        _compareRepositoryMock
            .Setup(x => x.GetClosestGame(playerOne, playerTwo))
            .ReturnsAsync((ClosestGame?)null);
    }

    private void SetupCompareRepositoryMocksWithLastWonGame(int playerOne, int playerTwo, LastWonGame lastWonGame)
    {
        _compareRepositoryMock
            .Setup(x => x.GetDirectWins(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<int>(0, 0));

        _compareRepositoryMock
            .Setup(x => x.GetMostWonGame(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<MostWonGame?>(new MostWonGame(), new MostWonGame()));

        _compareRepositoryMock
            .Setup(x => x.GetTotalSessionsTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync((PreferredGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLastWonGame(playerOne, playerTwo))
            .ReturnsAsync(lastWonGame);

        _compareRepositoryMock
            .Setup(x => x.GetLongestSessionTogether(playerOne, playerTwo))
            .ReturnsAsync((int?)null);

        _compareRepositoryMock
            .Setup(x => x.GetFirstGameTogether(playerOne, playerTwo))
            .ReturnsAsync((FirstGameTogether?)null);

        _compareRepositoryMock
            .Setup(x => x.GetClosestGame(playerOne, playerTwo))
            .ReturnsAsync((ClosestGame?)null);
    }

    private void SetupCompareRepositoryMocksWithFirstGameTogether(int playerOne, int playerTwo, FirstGameTogether firstGame)
    {
        _compareRepositoryMock
            .Setup(x => x.GetDirectWins(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<int>(0, 0));

        _compareRepositoryMock
            .Setup(x => x.GetMostWonGame(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<MostWonGame?>(new MostWonGame(), new MostWonGame()));

        _compareRepositoryMock
            .Setup(x => x.GetTotalSessionsTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync((PreferredGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLastWonGame(playerOne, playerTwo))
            .ReturnsAsync((LastWonGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLongestSessionTogether(playerOne, playerTwo))
            .ReturnsAsync((int?)null);

        _compareRepositoryMock
            .Setup(x => x.GetFirstGameTogether(playerOne, playerTwo))
            .ReturnsAsync(firstGame);

        _compareRepositoryMock
            .Setup(x => x.GetClosestGame(playerOne, playerTwo))
            .ReturnsAsync((ClosestGame?)null);
    }

    private void SetupCompareRepositoryMocksWithClosestGame(int playerOne, int playerTwo, ClosestGame closestGame)
    {
        _compareRepositoryMock
            .Setup(x => x.GetDirectWins(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<int>(0, 0));

        _compareRepositoryMock
            .Setup(x => x.GetMostWonGame(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<MostWonGame?>(new MostWonGame(), new MostWonGame()));

        _compareRepositoryMock
            .Setup(x => x.GetTotalSessionsTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(0);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync((PreferredGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLastWonGame(playerOne, playerTwo))
            .ReturnsAsync((LastWonGame?)null);

        _compareRepositoryMock
            .Setup(x => x.GetLongestSessionTogether(playerOne, playerTwo))
            .ReturnsAsync((int?)null);

        _compareRepositoryMock
            .Setup(x => x.GetFirstGameTogether(playerOne, playerTwo))
            .ReturnsAsync((FirstGameTogether?)null);

        _compareRepositoryMock
            .Setup(x => x.GetClosestGame(playerOne, playerTwo))
            .ReturnsAsync(closestGame);
    }

    #endregion
}
