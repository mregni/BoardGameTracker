using System.Collections.Generic;
using System.Linq;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Leaderboard;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Leaderboard;

public class PlayerRankingTests
{
    private sealed class Row
    {
        public required string Name { get; init; }
        public int Wins { get; init; }
        public int Plays { get; init; }
        public int Rank { get; set; }
    }

    private static List<Row> Rank(LeaderboardRankBy rankBy, params Row[] rows)
    {
        return PlayerRanking.Rank(rows, rankBy, x => new PlayerStanding(x.Name, x.Wins, x.Plays), (x, rank) => x.Rank = rank);
    }

    [Fact]
    public void Rank_ShouldBreakATieOnWins_ByTheHigherWinRate()
    {
        var result = Rank(
            LeaderboardRankBy.Wins,
            new Row { Name = "Jules", Wins = 13, Plays = 30 },
            new Row { Name = "Bram", Wins = 13, Plays = 26 });

        result.Select(x => (x.Rank, x.Name)).Should().Equal((1, "Bram"), (2, "Jules"));
    }

    [Fact]
    public void Rank_ShouldShareTheRank_OnlyWhenWinsAndSessionsAreEqual()
    {
        var result = Rank(
            LeaderboardRankBy.Wins,
            new Row { Name = "Zoe", Wins = 5, Plays = 10 },
            new Row { Name = "Adam", Wins = 5, Plays = 10 },
            new Row { Name = "Mia", Wins = 4, Plays = 4 },
            new Row { Name = "Lou", Wins = 4, Plays = 8 });

        result.Select(x => (x.Rank, x.Name)).Should().Equal((1, "Adam"), (1, "Zoe"), (3, "Mia"), (4, "Lou"));
    }

    [Fact]
    public void Rank_ShouldPreferMoreSessions_WhenWinsAndWinRateAreEqual()
    {
        var result = Rank(
            LeaderboardRankBy.Wins,
            new Row { Name = "Ann", Wins = 0, Plays = 3 },
            new Row { Name = "Ben", Wins = 0, Plays = 5 });

        result.Select(x => (x.Rank, x.Name)).Should().Equal((1, "Ben"), (2, "Ann"));
    }

    [Fact]
    public void Rank_ShouldOrderByWinRateThenWins_WhenRankingByWinRate()
    {
        var result = Rank(
            LeaderboardRankBy.WinRate,
            new Row { Name = "Ann", Wins = 2, Plays = 4 },
            new Row { Name = "Ben", Wins = 4, Plays = 8 },
            new Row { Name = "Cas", Wins = 3, Plays = 3 });

        result.Select(x => (x.Rank, x.Name)).Should().Equal((1, "Cas"), (2, "Ben"), (3, "Ann"));
    }

    [Fact]
    public void Rank_ShouldOrderBySessionsThenWins_WhenRankingBySessions()
    {
        var result = Rank(
            LeaderboardRankBy.Sessions,
            new Row { Name = "Ann", Wins = 1, Plays = 9 },
            new Row { Name = "Ben", Wins = 2, Plays = 9 },
            new Row { Name = "Cas", Wins = 0, Plays = 12 });

        result.Select(x => (x.Rank, x.Name)).Should().Equal((1, "Cas"), (2, "Ben"), (3, "Ann"));
    }

    [Fact]
    public void Rank_ShouldReturnNothing_ForNoPlayers()
    {
        Rank(LeaderboardRankBy.Wins).Should().BeEmpty();
    }
}
