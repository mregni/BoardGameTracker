using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Compares;
using BoardGameTracker.Core.Compares.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Compares;

public class CompareServiceTests
{
    private const int PlayerOne = 1;
    private const int PlayerTwo = 2;
    private static readonly DateTime Day = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ICompareRepository> _compareRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly CompareService _compareService;

    public CompareServiceTests()
    {
        _compareRepositoryMock = new Mock<ICompareRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _compareService = new CompareService(_compareRepositoryMock.Object, _playerRepositoryMock.Object, new Mock<ILogger<CompareService>>().Object);
    }

    private void VerifyNoOtherCalls()
    {
        _compareRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
    }

    private void SetupPlayer(int playerId, int sessionCount, double duration, int winCount)
    {
        _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(sessionCount);
        _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(duration);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(winCount);
    }

    private void VerifyComparisonCalls()
    {
        foreach (var playerId in new[] { PlayerOne, PlayerTwo })
        {
            _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.GetPlayLengthInMinutes(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.GetTotalWinCount(playerId), Times.Once);
        }

        _compareRepositoryMock.Verify(x => x.GetSessionsTogether(PlayerOne, PlayerTwo), Times.Once);
        VerifyNoOtherCalls();
    }

    private static SessionTogether Session(int id, int gameId, int daysAgo, bool oneWon, bool twoWon, double? oneScore = null, double? twoScore = null) => new()
    {
        SessionId = id,
        GameId = gameId,
        HasScoring = true,
        Start = Day.AddDays(-daysAgo),
        End = Day.AddDays(-daysAgo).AddMinutes(45),
        PlayerOneWon = oneWon,
        PlayerTwoWon = twoWon,
        PlayerOneScore = oneScore,
        PlayerTwoScore = twoScore
    };

    [Fact]
    public async Task GetPlayerComparison_ShouldCombineOverallStatsWithTheHeadToHead_FromOneQuery()
    {
        SetupPlayer(PlayerOne, sessionCount: 10, duration: 500.5, winCount: 5);
        SetupPlayer(PlayerTwo, sessionCount: 8, duration: 400, winCount: 2);
        _compareRepositoryMock
            .Setup(x => x.GetSessionsTogether(PlayerOne, PlayerTwo))
            .ReturnsAsync(new List<SessionTogether>
            {
                Session(3, 10, 0, oneWon: true, twoWon: false, oneScore: 50, twoScore: 49),
                Session(2, 10, 1, oneWon: true, twoWon: false, oneScore: 60, twoScore: 40),
                Session(1, 11, 2, oneWon: false, twoWon: false)
            });

        var result = await _compareService.GetPlayerComparison(PlayerOne, PlayerTwo);

        result.SessionCounts.PlayerOne.Should().Be(10);
        result.SessionCounts.PlayerTwo.Should().Be(8);
        result.TotalDuration.PlayerOne.Should().Be(500.5);
        result.TotalDuration.PlayerTwo.Should().Be(400);
        result.WinCount.PlayerOne.Should().Be(5);
        result.WinCount.PlayerTwo.Should().Be(2);
        result.WinPercentage.PlayerOne.Should().Be(0.5);
        result.WinPercentage.PlayerTwo.Should().Be(0.25);
        result.DirectWins.PlayerOne.Should().Be(2);
        result.DirectWins.PlayerTwo.Should().Be(0);
        result.WonByOthers.Should().Be(1);
        result.TotalSessionsTogether.Should().Be(3);
        result.MinutesPlayed.Should().Be(135);
        result.Games.Should().HaveCount(2);
        result.ClosestGame.Should().BeEquivalentTo(new ClosestGame { GameId = 10, ScoringDifference = 1, PlayerId = PlayerOne });
        result.Streak.Should().BeEquivalentTo(new CompareStreak { Side = CompareOutcome.PlayerOne, Count = 2 });
        result.RecentSessions.Should().HaveCount(3);
        VerifyComparisonCalls();
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(4, 4, 1)]
    [InlineData(3, 1, 1.0 / 3)]
    public async Task GetPlayerComparison_ShouldComputeTheWinRateAsAFraction(int sessionCount, int winCount, double expected)
    {
        SetupPlayer(PlayerOne, sessionCount, 0, winCount);
        SetupPlayer(PlayerTwo, 0, 0, 0);
        _compareRepositoryMock.Setup(x => x.GetSessionsTogether(PlayerOne, PlayerTwo)).ReturnsAsync([]);

        var result = await _compareService.GetPlayerComparison(PlayerOne, PlayerTwo);

        result.WinPercentage.PlayerOne.Should().BeApproximately(expected, 0.0001);
        result.WinPercentage.PlayerTwo.Should().Be(0);
        result.TotalSessionsTogether.Should().Be(0);
        result.Games.Should().BeEmpty();
        VerifyComparisonCalls();
    }

    [Fact]
    public async Task GetDefaultPair_ShouldReturnThePairWithTheMostSessionsTogether()
    {
        var pair = new ComparePairDto { PlayerOneId = 3, PlayerTwoId = 7, SessionCount = 12 };
        _compareRepositoryMock.Setup(x => x.GetPairWithMostSessions()).ReturnsAsync(pair);

        var result = await _compareService.GetDefaultPair();

        result.Should().BeSameAs(pair);
        _compareRepositoryMock.Verify(x => x.GetPairWithMostSessions(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDefaultPair_ShouldReturnNull_WhenNobodyPlayedTogether()
    {
        _compareRepositoryMock.Setup(x => x.GetPairWithMostSessions()).ReturnsAsync((ComparePairDto?)null);

        var result = await _compareService.GetDefaultPair();

        result.Should().BeNull();
        _compareRepositoryMock.Verify(x => x.GetPairWithMostSessions(), Times.Once);
        VerifyNoOtherCalls();
    }
}
