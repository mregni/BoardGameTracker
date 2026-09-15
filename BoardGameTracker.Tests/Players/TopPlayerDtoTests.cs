using System;
using System.Collections.Generic;
using System.Linq;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Players;

public class TopPlayerDtoTests
{
    private const int PlayerId = 1;

    [Fact]
    public void CreateTopPlayer_ShouldCountPlaysWinsAndAverageScore()
    {
        var play = Group((true, 10), (false, 20), (false, null));

        var result = TopPlayerDto.CreateTopPlayer(play);

        result.PlayerId.Should().Be(PlayerId);
        result.PlayCount.Should().Be(3);
        result.Wins.Should().Be(1);
        result.WinPercentage.Should().BeApproximately(1 / 3d, 0.0001);
        result.AverageScore.Should().Be(15);
    }

    [Fact]
    public void CreateTopPlayer_ShouldReportEqualTrend_WhenThereIsNoHistoryBeforeTheWindow()
    {
        var play = Group((true, null), (false, null), (true, null));

        TopPlayerDto.CreateTopPlayer(play).Trend.Should().Be(Trend.Equal);
    }

    [Fact]
    public void CreateTopPlayer_ShouldReportUpTrend_WhenRecentWinRateBeatsTheEarlierOne()
    {
        var play = Group((false, null), (false, null), (false, null), (true, null), (true, null), (true, null), (true, null), (true, null));

        TopPlayerDto.CreateTopPlayer(play).Trend.Should().Be(Trend.Up);
    }

    [Fact]
    public void CreateTopPlayer_ShouldReportDownTrend_WhenRecentWinRateIsBelowTheEarlierOne()
    {
        var play = Group((true, null), (true, null), (false, null), (false, null), (false, null), (true, null), (false, null));

        TopPlayerDto.CreateTopPlayer(play).Trend.Should().Be(Trend.Down);
    }

    [Fact]
    public void CreateTopPlayer_ShouldReportEqualTrend_WhenWinRatesMatch()
    {
        var play = Group((true, null), (true, null), (false, null), (false, null), (false, null), (true, null), (false, null), (false, null), (true, null), (false, null));

        TopPlayerDto.CreateTopPlayer(play).Trend.Should().Be(Trend.Equal);
    }

    private static IGrouping<int, PlayerSession> Group(params (bool Won, double? Score)[] playsInChronologicalOrder)
    {
        var start = new DateTime(2024, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        var playerSessions = new List<PlayerSession>();
        for (var i = 0; i < playsInChronologicalOrder.Length; i++)
        {
            var (won, score) = playsInChronologicalOrder[i];
            var session = new Session(1, start.AddDays(i), start.AddDays(i).AddHours(1), string.Empty);
            session.AddPlayerSession(PlayerId, score, false, won);
            var playerSession = session.PlayerSessions.Single();
            typeof(PlayerSession).GetProperty(nameof(PlayerSession.Session))!.SetValue(playerSession, session);
            playerSessions.Add(playerSession);
        }

        return playerSessions.GroupBy(x => x.PlayerId).Single();
    }
}
