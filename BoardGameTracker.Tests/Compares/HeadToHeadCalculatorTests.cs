using System;
using System.Collections.Generic;
using System.Linq;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Compares;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Compares;

public class HeadToHeadCalculatorTests
{
    private const int PlayerOne = 1;
    private const int PlayerTwo = 2;
    private static readonly DateTime Day = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);
    private int _nextId = 1;

    private SessionTogether Session(
        int gameId,
        int daysAgo,
        bool oneWon = false,
        bool twoWon = false,
        double? oneScore = null,
        double? twoScore = null,
        int minutes = 60,
        bool hasScoring = true,
        int? otherWinner = null,
        bool? teamWon = null)
    {
        var start = Day.AddDays(-daysAgo);
        return new SessionTogether
        {
            IsCoop = teamWon.HasValue,
            TeamWon = teamWon,
            SessionId = _nextId++,
            GameId = gameId,
            HasScoring = hasScoring,
            Start = start,
            End = start.AddMinutes(minutes),
            PlayerOneWon = oneWon,
            PlayerTwoWon = twoWon,
            PlayerOneScore = oneScore,
            PlayerTwoScore = twoScore,
            OtherWinnerId = otherWinner
        };
    }

    [Theory]
    [InlineData(true, true, CompareOutcome.Both)]
    [InlineData(true, false, CompareOutcome.PlayerOne)]
    [InlineData(false, true, CompareOutcome.PlayerTwo)]
    [InlineData(false, false, CompareOutcome.Other)]
    public void Outcome_ShouldTellWhoWon(bool oneWon, bool twoWon, CompareOutcome expected)
    {
        HeadToHeadCalculator.Outcome(Session(1, 0, oneWon, twoWon)).Should().Be(expected);
    }

    [Fact]
    public void Fill_ShouldCountWinsForEachAndWonBySomeoneElse_AndCountBothWonForBoth()
    {
        var sessions = new List<SessionTogether>
        {
            Session(1, 0, oneWon: true),
            Session(1, 1, twoWon: true),
            Session(1, 2, oneWon: true, twoWon: true),
            Session(2, 3, otherWinner: 9),
            Session(2, 4),
            Session(2, 5, oneWon: true)
        };
        var result = new CompareResultDto();

        HeadToHeadCalculator.Fill(result, sessions, PlayerOne, PlayerTwo);

        result.DirectWins.PlayerOne.Should().Be(3);
        result.DirectWins.PlayerTwo.Should().Be(2);
        result.WonByOthers.Should().Be(2);
        result.TotalSessionsTogether.Should().Be(6);
        result.MinutesPlayed.Should().Be(360);
        result.PreferredGame.Should().BeEquivalentTo(new PreferredGame { GameId = 1, SessionCount = 3 });
        result.FirstGameTogether.Should().BeEquivalentTo(new FirstGameTogether { GameId = 2, StartDate = Day.AddDays(-5) });
        result.RecentSessions.Select(s => s.Outcome).Should().Equal(
            CompareOutcome.PlayerOne, CompareOutcome.PlayerTwo, CompareOutcome.Both,
            CompareOutcome.Other, CompareOutcome.Other, CompareOutcome.PlayerOne);
        result.RecentSessions[3].OtherWinnerId.Should().Be(9);
    }

    [Fact]
    public void Outcome_ShouldBeSameTeam_ForACoopSession_WhateverTheWinFlags()
    {
        HeadToHeadCalculator.Outcome(Session(1, 0, oneWon: true, twoWon: true, teamWon: true)).Should().Be(CompareOutcome.SameTeam);
        HeadToHeadCalculator.Outcome(Session(1, 0, teamWon: false)).Should().Be(CompareOutcome.SameTeam);
    }

    [Fact]
    public void Fill_ShouldCountCoopSessionsAsSameTeam_NeverAsAWinOrAsSomeoneElse()
    {
        var sessions = new List<SessionTogether>
        {
            Session(5, 0, oneWon: true, twoWon: true, teamWon: true),
            Session(5, 1, teamWon: false),
            Session(5, 2, oneWon: true, twoWon: true, teamWon: true),
            Session(1, 3, oneWon: true),
            Session(1, 4)
        };
        var result = new CompareResultDto();

        HeadToHeadCalculator.Fill(result, sessions, PlayerOne, PlayerTwo);

        result.DirectWins.PlayerOne.Should().Be(1);
        result.DirectWins.PlayerTwo.Should().Be(0);
        result.WonByOthers.Should().Be(1);
        result.SameTeam.Should().BeEquivalentTo(new CompareTeamRecord { Count = 3, Won = 2, Lost = 1 });
        result.TotalSessionsTogether.Should().Be(5);
        result.RecentSessions[0].Should().BeEquivalentTo(new { Outcome = CompareOutcome.SameTeam, TeamWon = (bool?)true });
        result.RecentSessions[1].TeamWon.Should().BeFalse();
        result.RecentSessions[3].TeamWon.Should().BeNull();
    }

    [Fact]
    public void Games_ShouldCountSameTeamPerGame_WithTheTeamResultAsTheLastResult()
    {
        var sessions = new List<SessionTogether>
        {
            Session(5, 0, oneWon: true, twoWon: true, teamWon: true),
            Session(5, 1, teamWon: false),
            Session(5, 2, oneWon: true, twoWon: true, teamWon: true),
            Session(5, 3, oneWon: true, oneScore: 10, twoScore: 5)
        };

        var game = HeadToHeadCalculator.Games(sessions).Single();

        game.Should().BeEquivalentTo(new CompareGame
        {
            GameId = 5,
            SessionCount = 4,
            PlayerOneWins = 1,
            PlayerTwoWins = 0,
            OtherWins = 0,
            SameTeamCount = 3,
            SameTeamWins = 2,
            PlayerOneAverageScore = 10,
            PlayerTwoAverageScore = 5,
            ScoredSessionCount = 1,
            LastPlayed = Day,
            LastOutcome = CompareOutcome.SameTeam,
            LastTeamWon = true
        });
    }

    [Fact]
    public void BestGames_ShouldOnlyCountCompetitiveSessions()
    {
        var games = new List<CompareGame>
        {
            new() { GameId = 1, SessionCount = 5, SameTeamCount = 3, PlayerOneWins = 2 },
            new() { GameId = 2, SessionCount = 6, SameTeamCount = 3, PlayerOneWins = 3 }
        };

        HeadToHeadCalculator.BestGames(games, CompareOutcome.PlayerOne).Should().Equal(2);
    }

    [Fact]
    public void Streak_ShouldNeitherBreakNorExtend_OnSameTeamSessions()
    {
        var sessions = new List<SessionTogether>
        {
            Session(1, 0, oneWon: true),
            Session(5, 1, oneWon: true, twoWon: true, teamWon: true),
            Session(1, 2, oneWon: true),
            Session(5, 3, teamWon: false),
            Session(1, 4, twoWon: true)
        };

        HeadToHeadCalculator.Streak(sessions).Should().BeEquivalentTo(new CompareStreak { Side = CompareOutcome.PlayerOne, Count = 2 });
    }

    [Fact]
    public void Closest_ShouldSkipCoopSessions()
    {
        HeadToHeadCalculator.Closest([Session(5, 0, teamWon: true, oneScore: 3, twoScore: 3)], PlayerOne, PlayerTwo).Should().BeNull();
    }

    [Fact]
    public void Fill_ShouldLeaveEverythingEmpty_WhenThePlayersNeverPlayedTogether()
    {
        var result = new CompareResultDto();

        HeadToHeadCalculator.Fill(result, [], PlayerOne, PlayerTwo);

        result.DirectWins.PlayerOne.Should().Be(0);
        result.DirectWins.PlayerTwo.Should().Be(0);
        result.WonByOthers.Should().Be(0);
        result.TotalSessionsTogether.Should().Be(0);
        result.MinutesPlayed.Should().Be(0);
        result.PreferredGame.Should().BeNull();
        result.FirstGameTogether.Should().BeNull();
        result.ClosestGame.Should().BeNull();
        result.Streak.Should().BeNull();
        result.Games.Should().BeEmpty();
        result.RecentSessions.Should().BeEmpty();
        result.BestGames.PlayerOne.Should().BeEmpty();
        result.BestGames.PlayerTwo.Should().BeEmpty();
    }

    [Fact]
    public void Fill_ShouldKeepOnlyTheTenMostRecentSessions_WithDurationAndScores()
    {
        var sessions = Enumerable.Range(0, 12).Select(day => Session(1, day, oneScore: day, twoScore: 100, minutes: 30 + day)).ToList();
        var result = new CompareResultDto();

        HeadToHeadCalculator.Fill(result, sessions, PlayerOne, PlayerTwo);

        result.RecentSessions.Should().HaveCount(HeadToHeadCalculator.RecentSessionCount);
        result.RecentSessions[0].Should().BeEquivalentTo(new CompareSession
        {
            SessionId = sessions[0].SessionId,
            GameId = 1,
            Start = Day,
            Minutes = 30,
            PlayerOneScore = 0,
            PlayerTwoScore = 100,
            Outcome = CompareOutcome.Other
        });
    }

    [Fact]
    public void Games_ShouldGroupPerGame_WithWinsAverageScoresAndTheLastResult()
    {
        var sessions = new List<SessionTogether>
        {
            Session(1, 0, twoWon: true, oneScore: 70, twoScore: 82),
            Session(1, 2, oneWon: true, oneScore: 77, twoScore: 70),
            Session(1, 4, oneScore: 80, twoScore: 81, otherWinner: 5),
            Session(1, 6, oneWon: true, twoWon: true, oneScore: 91),
            Session(2, 1, oneWon: true, hasScoring: false, oneScore: 10, twoScore: 3)
        };

        var games = HeadToHeadCalculator.Games(sessions);

        games.Should().HaveCount(2);
        games[0].Should().BeEquivalentTo(new CompareGame
        {
            GameId = 1,
            SessionCount = 4,
            PlayerOneWins = 2,
            PlayerTwoWins = 2,
            OtherWins = 1,
            PlayerOneAverageScore = 79.5,
            PlayerTwoAverageScore = 77.7,
            ScoredSessionCount = 4,
            LastPlayed = Day,
            LastOutcome = CompareOutcome.PlayerTwo
        });
        games[1].Should().BeEquivalentTo(new CompareGame
        {
            GameId = 2,
            SessionCount = 1,
            PlayerOneWins = 1,
            PlayerTwoWins = 0,
            OtherWins = 0,
            PlayerOneAverageScore = null,
            PlayerTwoAverageScore = null,
            LastPlayed = Day.AddDays(-1),
            LastOutcome = CompareOutcome.PlayerOne
        });
    }

    [Fact]
    public void Games_ShouldPutTheMostRecentGameFirst_WhenCountsAreEqual()
    {
        var sessions = new List<SessionTogether> { Session(7, 3), Session(8, 1) };

        HeadToHeadCalculator.Games(sessions).Select(g => g.GameId).Should().Equal(8, 7);
    }

    [Fact]
    public void BestGames_ShouldOnlyUseGamesPlayedAtLeastThreeTimes_WithALead()
    {
        var games = new List<CompareGame>
        {
            new() { GameId = 1, SessionCount = 6, PlayerOneWins = 5, PlayerTwoWins = 1 },
            new() { GameId = 2, SessionCount = 6, PlayerOneWins = 4, PlayerTwoWins = 2 },
            new() { GameId = 3, SessionCount = 11, PlayerOneWins = 4, PlayerTwoWins = 7 },
            new() { GameId = 4, SessionCount = 2, PlayerOneWins = 2, PlayerTwoWins = 0 },
            new() { GameId = 5, SessionCount = 3, PlayerOneWins = 1, PlayerTwoWins = 1 },
            new() { GameId = 6, SessionCount = 3, PlayerOneWins = 0, PlayerTwoWins = 3 }
        };

        HeadToHeadCalculator.BestGames(games, CompareOutcome.PlayerOne).Should().Equal(1, 2);
        HeadToHeadCalculator.BestGames(games, CompareOutcome.PlayerTwo).Should().Equal(3, 6);
    }

    [Fact]
    public void BestGames_ShouldKeepTheTopThree_AndBreakTiesOnSessionCount()
    {
        var games = Enumerable.Range(1, 5)
            .Select(id => new CompareGame { GameId = id, SessionCount = 3 + id, PlayerOneWins = 3, PlayerTwoWins = 0 })
            .ToList();

        HeadToHeadCalculator.BestGames(games, CompareOutcome.PlayerOne).Should().Equal(5, 4, 3);
    }

    [Theory]
    [InlineData(CompareOutcome.Both)]
    [InlineData(CompareOutcome.Other)]
    public void BestGames_ShouldBeEmpty_ForAnOutcomeThatIsNotAPlayer(CompareOutcome side)
    {
        var games = new List<CompareGame> { new() { GameId = 1, SessionCount = 5, PlayerOneWins = 5 } };

        HeadToHeadCalculator.BestGames(games, side).Should().BeEmpty();
    }

    [Fact]
    public void Streak_ShouldCountTheLatestWinsInARow_SkippingBothWon()
    {
        var sessions = new List<SessionTogether>
        {
            Session(1, 0, twoWon: true),
            Session(1, 1, oneWon: true, twoWon: true),
            Session(1, 2, twoWon: true),
            Session(1, 3, oneWon: true),
            Session(1, 4, twoWon: true)
        };

        HeadToHeadCalculator.Streak(sessions).Should().BeEquivalentTo(new CompareStreak { Side = CompareOutcome.PlayerTwo, Count = 2 });
    }

    [Fact]
    public void Streak_ShouldStop_WhenSomeoneElseWon()
    {
        var sessions = new List<SessionTogether>
        {
            Session(1, 0, oneWon: true),
            Session(1, 1, oneWon: true),
            Session(1, 2, otherWinner: 3),
            Session(1, 3, oneWon: true)
        };

        HeadToHeadCalculator.Streak(sessions).Should().BeEquivalentTo(new CompareStreak { Side = CompareOutcome.PlayerOne, Count = 2 });
    }

    [Fact]
    public void Streak_ShouldBeNull_ForASingleWinOrWhenTheLastGameWentToSomeoneElse()
    {
        HeadToHeadCalculator.Streak([Session(1, 0, oneWon: true), Session(1, 1, twoWon: true)]).Should().BeNull();
        HeadToHeadCalculator.Streak([Session(1, 0), Session(1, 1, oneWon: true), Session(1, 2, oneWon: true)]).Should().BeNull();
        HeadToHeadCalculator.Streak([]).Should().BeNull();
    }

    [Fact]
    public void Closest_ShouldPickTheSmallestScoreGap_OnlyInScoringGamesWithBothScores()
    {
        var sessions = new List<SessionTogether>
        {
            Session(1, 0, oneWon: true, oneScore: 80, twoScore: 70),
            Session(2, 1, twoWon: true, oneScore: 151, twoScore: 152),
            Session(3, 2, oneWon: true, hasScoring: false, oneScore: 5, twoScore: 5),
            Session(4, 3, oneWon: true, oneScore: 10)
        };

        HeadToHeadCalculator.Closest(sessions, PlayerOne, PlayerTwo)
            .Should().BeEquivalentTo(new ClosestGame { GameId = 2, ScoringDifference = 1, PlayerId = PlayerTwo });
    }

    [Fact]
    public void Closest_ShouldHaveNoWinner_WhenSomeoneElseWon()
    {
        var closest = HeadToHeadCalculator.Closest([Session(1, 0, oneScore: 20, twoScore: 20)], PlayerOne, PlayerTwo);

        closest!.PlayerId.Should().BeNull();
        closest.ScoringDifference.Should().Be(0);
    }

    [Fact]
    public void Closest_ShouldGiveTheWinToPlayerOne_WhenOnlyPlayerOneWon()
    {
        HeadToHeadCalculator.Closest([Session(1, 0, oneWon: true, oneScore: 3, twoScore: 1)], PlayerOne, PlayerTwo)!
            .PlayerId.Should().Be(PlayerOne);
    }
}
