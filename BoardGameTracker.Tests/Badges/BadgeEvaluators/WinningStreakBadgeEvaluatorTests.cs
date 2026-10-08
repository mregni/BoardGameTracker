using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Badges.BadgeEvaluators;

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
        var badge = TestBadges.Create(BadgeType.WinningStreak, BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 3; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .Build();
            sessions.Add(session);
        }

        var lossSession = new SessionBuilder().ForGame(1).DaysAgo(3)
            .WithPlayer(PlayerId)
            .Build();
        sessions.Add(lossSession);

        for (var i = 4; i < 14; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountFromMostRecentSession()
    {
        var badge = TestBadges.Create(BadgeType.WinningStreak, BadgeLevel.Green);
        var sessions = new List<Session>();

        var recentLoss = new SessionBuilder().ForGame(1)
            .WithPlayer(PlayerId)
            .Build();
        sessions.Add(recentLoss);

        for (var i = 1; i <= 10; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldBreakSameStartTiesByIdDescending()
    {
        var badge = TestBadges.Create(BadgeType.WinningStreak, BadgeLevel.Green);
        var end = DateTime.UtcNow;
        var sessions = new List<Session>
        {
            new SessionBuilder().ForGame(1).EndingAt(end).WithId(1).WithPlayer(PlayerId).Build(),
        };

        for (var i = 0; i < 5; i++)
        {
            sessions.Add(new SessionBuilder().ForGame(1).EndingAt(end).WithId(i + 2).WithWinner(PlayerId).Build());
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
        var badge = TestBadges.Create(BadgeType.WinningStreak, BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 10; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithPlayer(PlayerId)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSessionListIsEmpty()
    {
        var badge = TestBadges.Create(BadgeType.WinningStreak, BadgeLevel.Green);
        var currentSession = new SessionBuilder().ForGame(1).Build();
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
        var badge = TestBadges.Create(BadgeType.WinningStreak, level);
        var sessions = CreateSessionsWithWinStreak(streakCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    #endregion

    #region Helper Methods

    private static List<Session> CreateSessionsWithWinStreak(int streakCount)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < streakCount; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .Build();
            sessions.Add(session);
        }
        return sessions;
    }

    #endregion
}
