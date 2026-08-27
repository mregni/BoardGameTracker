using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class WinningStreakBadgeEvaluatorTests
{
    private readonly WinningStreakBadgeEvaluator _evaluator;
    private const int PlayerId = 1;

    public WinningStreakBadgeEvaluatorTests()
    {
        _evaluator = new WinningStreakBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeWinningStreak()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.WinningStreak);
    }

    #region Streak Breaking Tests

    [Fact]
    public async Task CanAwardBadge_ShouldStopCountingAtFirstLoss()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 3; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
        }

        var lossSession = CreateSession(1, 3);
        lossSession.AddPlayerSession(PlayerId, null, false, false);
        sessions.Add(lossSession);

        for (var i = 4; i < 14; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountFromMostRecentSession()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        var recentLoss = CreateSession(1, 0);
        recentLoss.AddPlayerSession(PlayerId, null, false, false);
        sessions.Add(recentLoss);

        for (var i = 1; i <= 10; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldBreakSameStartTiesByIdDescending()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var start = DateTime.UtcNow.AddHours(-2);
        var end = DateTime.UtcNow;
        var sessions = new List<Session>();

        var loss = new Session(1, start, end, "Loss session") { Id = 1 };
        loss.AddPlayerSession(PlayerId, null, false, false);
        sessions.Add(loss);

        for (var i = 0; i < 5; i++)
        {
            var win = new Session(1, start, end, "Win session") { Id = i + 2 };
            win.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(win);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.WinningStreak, "image", null);
        var sessions = CreateSessionsWithWinStreak(25);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNoWins()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 10; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, false);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSessionListIsEmpty()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var currentSession = CreateSession(1, 0);
        var sessions = new List<Session>();

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 4, false)]
    [InlineData(BadgeLevel.Green, 5, true)]
    [InlineData(BadgeLevel.Green, 8, true)]
    [InlineData(BadgeLevel.Blue, 9, false)]
    [InlineData(BadgeLevel.Blue, 10, true)]
    [InlineData(BadgeLevel.Red, 14, false)]
    [InlineData(BadgeLevel.Red, 15, true)]
    [InlineData(BadgeLevel.Gold, 24, false)]
    [InlineData(BadgeLevel.Gold, 25, true)]
    public async Task CanAwardBadge_ShouldEvaluateStreakLengthPerLevel(BadgeLevel level, int streakCount, bool expected)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessionsWithWinStreak(streakCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "winning_streak_title", "winning_streak_desc", BadgeType.WinningStreak, "badge.png", level);
    }

    private static Session CreateSession(int gameId, int dayOffset)
    {
        var start = DateTime.UtcNow.AddDays(-dayOffset).AddHours(-2);
        var end = DateTime.UtcNow.AddDays(-dayOffset);
        return new Session(gameId, start, end, $"Session {dayOffset}");
    }

    private static List<Session> CreateSessionsWithWinStreak(int streakCount)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < streakCount; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
        }
        return sessions;
    }

    #endregion
}
