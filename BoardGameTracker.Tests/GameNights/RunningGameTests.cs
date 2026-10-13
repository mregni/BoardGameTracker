using System;
using BoardGameTracker.Common.Entities;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class RunningGameTests
{
    private static readonly DateTime Start = new(2026, 10, 16, 22, 5, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldKeepTheNightGameStartAndDistinctPlayers()
    {
        var runningGame = RunningGame.Create(3, 9, Start, [1, 2, 2, 4]);

        runningGame.GameNightId.Should().Be(3);
        runningGame.GameId.Should().Be(9);
        runningGame.Start.Should().Be(Start);
        runningGame.GetPlayerIds().Should().Equal(1, 2, 4);
    }

    [Fact]
    public void Create_ShouldThrow_WhenNobodyPlays()
    {
        var act = () => RunningGame.Create(3, 9, Start, []);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0, 9, 1)]
    [InlineData(3, 0, 1)]
    [InlineData(3, 9, 0)]
    public void Create_ShouldThrow_WhenAnIdIsNotPositive(int gameNightId, int gameId, int playerId)
    {
        var act = () => RunningGame.Create(gameNightId, gameId, Start, [playerId]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenStartIsMissing()
    {
        var act = () => RunningGame.Create(3, 9, default, [1]);

        act.Should().Throw<ArgumentException>();
    }
}
