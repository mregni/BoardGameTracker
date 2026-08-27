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

namespace BoardGameTracker.Tests.Services;

public class CompareServiceTests
{
    private readonly Mock<ICompareRepository> _compareRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly Mock<ILogger<CompareService>> _loggerMock;
    private readonly CompareService _compareService;

    public CompareServiceTests()
    {
        _compareRepositoryMock = new Mock<ICompareRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _loggerMock = new Mock<ILogger<CompareService>>();
        _compareService = new CompareService(_compareRepositoryMock.Object, _playerRepositoryMock.Object, _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _compareRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
    }

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

    private void SetupCompareRepositoryDefaults(int playerOne, int playerTwo)
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

    private void VerifyAllRepositoryCalls(int playerOne, int playerTwo)
    {
        _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerOne), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerTwo), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetPlayLengthInMinutes(playerOne), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetPlayLengthInMinutes(playerTwo), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTotalWinCount(playerOne), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTotalWinCount(playerTwo), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetDirectWins(playerOne, playerTwo), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetMostWonGame(playerOne, playerTwo), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetTotalSessionsTogether(playerOne, playerTwo), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetMinutesPlayedTogether(playerOne, playerTwo), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetPreferredGame(playerOne, playerTwo), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetLastWonGame(playerOne, playerTwo), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetLongestSessionTogether(playerOne, playerTwo), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetFirstGameTogether(playerOne, playerTwo), Times.Once);
        _compareRepositoryMock.Verify(x => x.GetClosestGame(playerOne, playerTwo), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldMapAllRepositoryValues()
    {
        var playerOne = 1;
        var playerTwo = 2;

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.5, winCount: 5);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 8, duration: 400.0, winCount: 3);
        SetupCompareRepositoryDefaults(playerOne, playerTwo);

        _compareRepositoryMock
            .Setup(x => x.GetDirectWins(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<int>(7, 3));

        _compareRepositoryMock
            .Setup(x => x.GetMostWonGame(playerOne, playerTwo))
            .ReturnsAsync(new CompareRow<MostWonGame?>(
                new MostWonGame { GameId = 1, Count = 5 },
                new MostWonGame { GameId = 2, Count = 3 }));

        _compareRepositoryMock
            .Setup(x => x.GetTotalSessionsTogether(playerOne, playerTwo))
            .ReturnsAsync(15);

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(600.0);

        _compareRepositoryMock
            .Setup(x => x.GetPreferredGame(playerOne, playerTwo))
            .ReturnsAsync(new PreferredGame { GameId = 5, SessionCount = 10 });

        _compareRepositoryMock
            .Setup(x => x.GetLastWonGame(playerOne, playerTwo))
            .ReturnsAsync(new LastWonGame { PlayerId = playerOne, GameId = 3 });

        _compareRepositoryMock
            .Setup(x => x.GetLongestSessionTogether(playerOne, playerTwo))
            .ReturnsAsync(180);

        _compareRepositoryMock
            .Setup(x => x.GetFirstGameTogether(playerOne, playerTwo))
            .ReturnsAsync(new FirstGameTogether { GameId = 7, StartDate = new DateTime(2020, 1, 15) });

        _compareRepositoryMock
            .Setup(x => x.GetClosestGame(playerOne, playerTwo))
            .ReturnsAsync(new ClosestGame { PlayerId = playerTwo, GameId = 4, ScoringDifference = 1.5 });

        var result = await _compareService.GetPlayerComparison(playerOne, playerTwo);

        result.Should().NotBeNull();
        result.SessionCounts.PlayerOne.Should().Be(10);
        result.SessionCounts.PlayerTwo.Should().Be(8);
        result.TotalDuration.PlayerOne.Should().Be(500.5);
        result.TotalDuration.PlayerTwo.Should().Be(400.0);
        result.WinCount.PlayerOne.Should().Be(5);
        result.WinCount.PlayerTwo.Should().Be(3);
        result.WinPercentage.PlayerOne.Should().Be(0.5);
        result.WinPercentage.PlayerTwo.Should().Be(0.375);
        result.DirectWins.PlayerOne.Should().Be(7);
        result.DirectWins.PlayerTwo.Should().Be(3);
        result.MostWonGame.PlayerOne!.GameId.Should().Be(1);
        result.MostWonGame.PlayerOne.Count.Should().Be(5);
        result.MostWonGame.PlayerTwo!.GameId.Should().Be(2);
        result.MostWonGame.PlayerTwo.Count.Should().Be(3);
        result.TotalSessionsTogether.Should().Be(15);
        result.MinutesPlayed.Should().Be(600);
        result.PreferredGame!.GameId.Should().Be(5);
        result.PreferredGame.SessionCount.Should().Be(10);
        result.LastWonGame!.PlayerId.Should().Be(playerOne);
        result.LastWonGame.GameId.Should().Be(3);
        result.LongestSessionTogether.Should().Be(180);
        result.FirstGameTogether!.GameId.Should().Be(7);
        result.FirstGameTogether.StartDate.Should().Be(new DateTime(2020, 1, 15));
        result.ClosestGame!.PlayerId.Should().Be(playerTwo);
        result.ClosestGame.GameId.Should().Be(4);
        result.ClosestGame.ScoringDifference.Should().Be(1.5);

        VerifyAllRepositoryCalls(playerOne, playerTwo);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnNullsAndZeroes_WhenPlayersShareNoData()
    {
        var playerOne = 1;
        var playerTwo = 2;

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 0, duration: 0, winCount: 0);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 0, duration: 0, winCount: 0);
        SetupCompareRepositoryDefaults(playerOne, playerTwo);

        var result = await _compareService.GetPlayerComparison(playerOne, playerTwo);

        result.WinPercentage.PlayerOne.Should().Be(0);
        result.WinPercentage.PlayerTwo.Should().Be(0);
        result.TotalSessionsTogether.Should().Be(0);
        result.MinutesPlayed.Should().Be(0);
        result.PreferredGame.Should().BeNull();
        result.LastWonGame.Should().BeNull();
        result.LongestSessionTogether.Should().BeNull();
        result.FirstGameTogether.Should().BeNull();
        result.ClosestGame.Should().BeNull();

        VerifyAllRepositoryCalls(playerOne, playerTwo);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(10, 5, 0.5)]
    [InlineData(8, 3, 0.375)]
    [InlineData(5, 2, 0.4)]
    public async Task GetPlayerComparison_ShouldComputeWinPercentage(int sessionCount, int winCount, double expectedPercentage)
    {
        var playerOne = 1;
        var playerTwo = 2;

        SetupPlayerRepositoryMocks(playerOne, sessionCount, duration: 100.0, winCount);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 4, duration: 100.0, winCount: 1);
        SetupCompareRepositoryDefaults(playerOne, playerTwo);

        var result = await _compareService.GetPlayerComparison(playerOne, playerTwo);

        result.WinPercentage.PlayerOne.Should().Be(expectedPercentage);
        result.WinPercentage.PlayerTwo.Should().Be(0.25);

        VerifyAllRepositoryCalls(playerOne, playerTwo);
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldTruncateMinutesPlayedToInteger()
    {
        var playerOne = 1;
        var playerTwo = 2;

        SetupPlayerRepositoryMocks(playerOne, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupPlayerRepositoryMocks(playerTwo, sessionCount: 10, duration: 500.0, winCount: 5);
        SetupCompareRepositoryDefaults(playerOne, playerTwo);

        _compareRepositoryMock
            .Setup(x => x.GetMinutesPlayedTogether(playerOne, playerTwo))
            .ReturnsAsync(750.7);

        var result = await _compareService.GetPlayerComparison(playerOne, playerTwo);

        result.MinutesPlayed.Should().Be(750);

        VerifyAllRepositoryCalls(playerOne, playerTwo);
    }
}
