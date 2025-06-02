using System;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Models.Charts;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Models;

public class ScoreRankTests
{
    [Fact]
    public void MakeHighestScoreRank_ShouldReturnScoreRankWithTopScoreKey_WhenValidPlayerSessionProvided()
    {
        var playerSession = new PlayerSession
        {
            PlayerId = 1,
            Score = 95.5,
            Won = true
        };

        var result = ScoreRank.MakeHighestScoreRank(playerSession);

        result.Should().NotBeNull();
        result.Key.Should().Be("top-score");
        result.Score.Should().Be(95.5);
        result.PlayerId.Should().Be(1);
    }

    [Fact]
    public void MakeHighestScoreRank_ShouldReturnNull_WhenPlayerSessionIsNull()
    {
        var result = ScoreRank.MakeHighestScoreRank(null);

        result.Should().BeNull();
    }

    [Fact]
    public void MakeHighestLosingRank_ShouldReturnScoreRankWithHighestLosingKey_WhenValidPlayerSessionProvided()
    {
        var playerSession = new PlayerSession
        {
            PlayerId = 2,
            Score = 78.3,
            Won = false
        };

        var result = ScoreRank.MakeHighestLosingRank(playerSession);

        result.Should().NotBeNull();
        result.Key.Should().Be("highest-losing");
        result.Score.Should().Be(78.3);
        result.PlayerId.Should().Be(2);
    }

    [Fact]
    public void MakeHighestLosingRank_ShouldReturnNull_WhenPlayerSessionIsNull()
    {
        var result = ScoreRank.MakeHighestLosingRank(null);

        result.Should().BeNull();
    }

    [Fact]
    public void MakeLowestWinningRank_ShouldReturnScoreRankWithLowestWinningKey_WhenValidPlayerSessionProvided()
    {
        var playerSession = new PlayerSession
        {
            PlayerId = 3,
            Score = 42.1,
            Won = true
        };

        var result = ScoreRank.MakeLowestWinningRank(playerSession);

        result.Should().NotBeNull();
        result.Key.Should().Be("lowest-winning");
        result.Score.Should().Be(42.1);
        result.PlayerId.Should().Be(3);
    }

    [Fact]
    public void MakeLowestWinningRank_ShouldReturnNull_WhenPlayerSessionIsNull()
    {
        var result = ScoreRank.MakeLowestWinningRank(null);

        result.Should().BeNull();
    }

    [Fact]
    public void MakeLowestScoreRank_ShouldReturnScoreRankWithLowestKey_WhenValidPlayerSessionProvided()
    {
        var playerSession = new PlayerSession
        {
            PlayerId = 4,
            Score = 15.7,
            Won = false
        };

        var result = ScoreRank.MakeLowestScoreRank(playerSession);

        result.Should().NotBeNull();
        result.Key.Should().Be("lowest");
        result.Score.Should().Be(15.7);
        result.PlayerId.Should().Be(4);
    }

    [Fact]
    public void MakeLowestScoreRank_ShouldReturnNull_WhenPlayerSessionIsNull()
    {
        var result = ScoreRank.MakeLowestScoreRank(null);

        result.Should().BeNull();
    }

    [Fact]
    public void MakeAverageRank_ShouldReturnScoreRankWithAverageKey_WhenValidAverageProvided()
    {
        const double average = 67.8;

        var result = ScoreRank.MakeAverageRank(average);

        result.Should().NotBeNull();
        result.Key.Should().Be("average");
        result.Score.Should().Be(67.8);
        result.PlayerId.Should().Be(0);
    }

    [Fact]
    public void MakeAverageRank_ShouldReturnNull_WhenAverageIsNull()
    {
        var result = ScoreRank.MakeAverageRank(null);

        result.Should().BeNull();
    }

    [Fact]
    public void MakeAverageRank_ShouldHandleZeroAverage_WhenAverageIsZero()
    {
        const double average = 0.0;

        var result = ScoreRank.MakeAverageRank(average);

        result.Should().NotBeNull();
        result.Key.Should().Be("average");
        result.Score.Should().Be(0.0);
        result.PlayerId.Should().Be(0);
    }

    [Fact]
    public void Make_ShouldUseZeroScore_WhenPlayerSessionScoreIsNull()
    {
        var playerSession = new PlayerSession
        {
            PlayerId = 5,
            Score = null,
            Won = true
        };

        var result = ScoreRank.MakeHighestScoreRank(playerSession);

        result.Should().NotBeNull();
        result.Score.Should().Be(0);
        result.PlayerId.Should().Be(5);
    }

    [Theory]
    [InlineData("MakeHighestScoreRank", "top-score")]
    [InlineData("MakeHighestLosingRank", "highest-losing")]
    [InlineData("MakeLowestWinningRank", "lowest-winning")]
    [InlineData("MakeLowestScoreRank", "lowest")]
    public void AllMakeMethods_ShouldReturnCorrectKey_WhenValidPlayerSessionProvided(string methodName,
        string expectedKey)
    {
        var playerSession = new PlayerSession
        {
            PlayerId = 1,
            Score = 50.0,
            Won = true
        };

        var result = methodName switch
        {
            "MakeHighestScoreRank" => ScoreRank.MakeHighestScoreRank(playerSession),
            "MakeHighestLosingRank" => ScoreRank.MakeHighestLosingRank(playerSession),
            "MakeLowestWinningRank" => ScoreRank.MakeLowestWinningRank(playerSession),
            "MakeLowestScoreRank" => ScoreRank.MakeLowestScoreRank(playerSession),
            _ => throw new ArgumentException($"Unknown method: {methodName}")
        };

        result.Should().NotBeNull();
        result.Key.Should().Be(expectedKey);
    }

    [Fact]
    public void AllPlayerSessionMethods_ShouldReturnNull_WhenPlayerSessionIsNull()
    {
        var methods = new[]
        {
            ScoreRank.MakeHighestScoreRank,
            ScoreRank.MakeHighestLosingRank,
            ScoreRank.MakeLowestWinningRank,
            ScoreRank.MakeLowestScoreRank
        };

        foreach (var method in methods)
        {
            var result = method(null);
            result.Should().BeNull();
        }
    }

    [Fact]
    public void MakeAverageRank_ShouldNotSetPlayerId_WhenCreatingAverageRank()
    {
        const double average = 75.5;

        var result = ScoreRank.MakeAverageRank(average);

        result.Should().NotBeNull();
        result.PlayerId.Should().Be(0);
    }

    [Fact]
    public void Make_ShouldCreateDifferentInstancesForSameInput_WhenCalledMultipleTimes()
    {
        var playerSession = new PlayerSession
        {
            PlayerId = 1,
            Score = 50.0,
            Won = true
        };

        var result1 = ScoreRank.MakeHighestScoreRank(playerSession);
        var result2 = ScoreRank.MakeHighestScoreRank(playerSession);

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Should().NotBeSameAs(result2);
        result1.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public void Make_ShouldHandlePlayerSessionWithAllProperties_WhenCompletePlayerSessionProvided()
    {
        var playerSession = new PlayerSession
        {
            PlayerId = 10,
            Score = 85.7,
            Won = true,
            FirstPlay = false,
            Player = new Player {Id = 10, Name = "Test Player"},
            SessionId = 5,
            Session = new Session {Id = 5, Start = DateTime.Now}
        };

        var result = ScoreRank.MakeHighestScoreRank(playerSession);

        result.Should().NotBeNull();
        result.Key.Should().Be("top-score");
        result.Score.Should().Be(85.7);
        result.PlayerId.Should().Be(10);
    }

    [Fact]
    public void Make_ShouldIgnoreWonProperty_WhenCreatingScoreRank()
    {
        var winningSession = new PlayerSession {PlayerId = 1, Score = 50.0, Won = true};
        var losingSession = new PlayerSession {PlayerId = 2, Score = 50.0, Won = false};

        var winningResult = ScoreRank.MakeHighestScoreRank(winningSession);
        var losingResult = ScoreRank.MakeHighestScoreRank(losingSession);

        winningResult.Should().NotBeNull();
        losingResult.Should().NotBeNull();
        winningResult.Key.Should().Be(losingResult.Key);
        winningResult.Score.Should().Be(losingResult.Score);
    }
}