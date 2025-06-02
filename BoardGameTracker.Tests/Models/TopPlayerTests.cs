using System;
using System.Collections.Generic;
using System.Linq;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Tests.Services;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Models;

public class TopPlayerTests
{
    [Fact]
    public void CreateTopPlayer_ShouldReturnTopPlayerWithCorrectStats_WhenValidGroupingProvided()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-3)}},
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-2)}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-1)}}
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.Should().NotBeNull();
        result.PlayerId.Should().Be(1);
        result.PlayCount.Should().Be(3);
        result.Wins.Should().Be(2);
        result.WinPercentage.Should().BeApproximately(0.6667, 0.001);
    }

    [Fact]
    public void CreateTopPlayer_ShouldSetTrendUp_WhenCurrentWinRateIsHigherThanPrevious()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-3)}},
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-2)}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-1)}}
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.Trend.Should().Be(Trend.Up);
        result.WinPercentage.Should().BeApproximately(0.3333, 0.001);
    }

    [Fact]
    public void CreateTopPlayer_ShouldSetTrendDown_WhenCurrentWinRateIsLowerThanPrevious()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-3)}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-2)}},
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-1)}}
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.Trend.Should().Be(Trend.Down);
        result.WinPercentage.Should().BeApproximately(0.6666, 0.002);
    }

    [Fact]
    public void CreateTopPlayer_ShouldSetTrendEqual_WhenCurrentWinRateEqualsPrevious()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-3)}},
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.Trend.Should().Be(Trend.Equal);
        result.WinPercentage.Should().Be(1);
    }

    [Fact]
    public void CreateTopPlayer_ShouldHandleSingleSession_WhenOnlyOneSessionProvided()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now}}
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.PlayerId.Should().Be(1);
        result.PlayCount.Should().Be(1);
        result.Wins.Should().Be(1);
        result.WinPercentage.Should().Be(1.0);
        result.Trend.Should().Be(Trend.Equal);
    }

    [Fact]
    public void CreateTopPlayer_ShouldHandleAllWins_WhenAllSessionsAreWins()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-2)}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-1)}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now}}
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.PlayerId.Should().Be(1);
        result.PlayCount.Should().Be(3);
        result.Wins.Should().Be(3);
        result.WinPercentage.Should().Be(1.0);
        result.Trend.Should().Be(Trend.Equal);
    }

    [Fact]
    public void CreateTopPlayer_ShouldHandleAllLosses_WhenAllSessionsAreLosses()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-2)}},
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-1)}},
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now}}
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.PlayerId.Should().Be(1);
        result.PlayCount.Should().Be(3);
        result.Wins.Should().Be(0);
        result.WinPercentage.Should().Be(0.0);
        result.Trend.Should().Be(Trend.Equal);
    }

    [Fact]
    public void CreateTopPlayer_ShouldOrderBySessionStartDescending_WhenCalculatingTrend()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-5)}},
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-1)}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-3)}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now}}
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.WinPercentage.Should().Be(0.5);
        result.Trend.Should().Be(Trend.Up);
    }

    [Fact]
    public void CreateTopPlayer_ShouldUseFirstPlayerIdFromGrouping_WhenMultipleSessionsProvided()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 5, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-1)}},
            new() {PlayerId = 5, Won = false, Session = new Session {Start = DateTime.Now}}
        };
        var grouping = CreateGrouping(5, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.PlayerId.Should().Be(5);
    }

    [Theory]
    [InlineData(1, 0, 0.0)]
    [InlineData(2, 1, 0.5)]
    [InlineData(3, 2, 0.6667)]
    [InlineData(4, 4, 1.0)]
    [InlineData(10, 7, 0.7)]
    public void CreateTopPlayer_ShouldCalculateCorrectWinPercentage_WithDifferentWinCounts(int totalPlays, int wins,
        double expectedPercentage)
    {
        var playerSessions = new List<PlayerSession>();
        for (int i = 0; i < totalPlays; i++)
        {
            playerSessions.Add(new PlayerSession
            {
                PlayerId = 1,
                Won = i < wins,
                Session = new Session {Start = DateTime.Now.AddDays(-i)}
            });
        }

        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.PlayCount.Should().Be(totalPlays);
        result.Wins.Should().Be(wins);
        result.WinPercentage.Should().BeApproximately(expectedPercentage, 0.001);
    }

    [Fact]
    public void CreateTopPlayer_ShouldHandleTwoSessions_WhenCalculatingTrend()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-1)}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now}}
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.PlayCount.Should().Be(2);
        result.Wins.Should().Be(1);
        result.WinPercentage.Should().Be(0.5);
        result.Trend.Should().Be(Trend.Up);
    }

    [Fact]
    public void CreateTopPlayer_ShouldExcludeMostRecentSession_WhenCalculatingPreviousWinRate()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-4)}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-3)}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-2)}},
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now}}
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.WinPercentage.Should().Be(0.75);
        result.Trend.Should().Be(Trend.Down);
    }

    [Fact]
    public void CreateTopPlayer_ShouldHandleIdenticalDates_WhenSessionsHaveSameStartTime()
    {
        var sameDate = DateTime.Now;
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = true, Session = new Session {Start = sameDate}},
            new() {PlayerId = 1, Won = false, Session = new Session {Start = sameDate}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = sameDate}}
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.PlayCount.Should().Be(3);
        result.Wins.Should().Be(2);
        result.WinPercentage.Should().BeApproximately(0.6667, 0.001);
    }

    [Fact]
    public void CreateTopPlayer_ShouldHandleMixedWinLossPattern_WhenCalculatingTrend()
    {
        var playerSessions = new List<PlayerSession>
        {
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-5)}},
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-4)}},
            new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-3)}},
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-2)}},
            new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now}}
        };
        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.WinPercentage.Should().Be(0.4);
        result.Trend.Should().Be(Trend.Down);
    }

    [Theory]
    [InlineData(Trend.Up)]
    [InlineData(Trend.Down)]
    [InlineData(Trend.Equal)]
    public void CreateTopPlayer_ShouldSetCorrectTrend_WithDifferentScenarios(Trend expectedTrend)
    {
        List<PlayerSession> playerSessions = expectedTrend switch
        {
            Trend.Up => new List<PlayerSession>
            {
                new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now.AddDays(-1)}},
                new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now}}
            },
            Trend.Down => new List<PlayerSession>
            {
                new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-1)}},
                new() {PlayerId = 1, Won = false, Session = new Session {Start = DateTime.Now}}
            },
            _ => new List<PlayerSession>
            {
                new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now.AddDays(-1)}},
                new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now}}
            }
        };

        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.Trend.Should().Be(expectedTrend);
    }

    [Fact]
    public void CreateTopPlayer_ShouldHandleLargeDataSet_WhenManySessionsProvided()
    {
        var playerSessions = new List<PlayerSession>();
        for (int i = 0; i < 100; i++)
        {
            playerSessions.Add(new PlayerSession
            {
                PlayerId = 1,
                Won = i % 3 == 0,
                Session = new Session {Start = DateTime.Now.AddDays(-i)}
            });
        }

        var grouping = CreateGrouping(1, playerSessions);

        var result = TopPlayer.CreateTopPlayer(grouping);

        result.PlayCount.Should().Be(100);
        result.Wins.Should().Be(34);
        result.WinPercentage.Should().Be(0.34);
    }

    private static IGrouping<int, PlayerSession> CreateGrouping(int playerId, IEnumerable<PlayerSession> sessions)
    {
        return new TestGrouping<int, PlayerSession>(playerId, sessions);
    }
}