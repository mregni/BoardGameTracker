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

public class DurationBadgeEvaluatorTests
{
    private readonly DurationBadgeEvaluator _evaluator;
    private const int PlayerId = 1;

    public DurationBadgeEvaluatorTests()
    {
        _evaluator = new DurationBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeDuration()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.Duration);
    }

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.Duration, "image", null);
        var sessions = CreateWinningSessionsWithDuration(6000);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountLostSessionsAsWell()
    {
        var badge = TestBadges.Create(BadgeType.Duration, BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 2; i++)
        {
            var session = new SessionBuilder().DaysAgo(i).LastingMinutes(100)
                .WithPlayer(PlayerId)
                .Build();
            sessions.Add(session);
        }

        var session2 = new SessionBuilder().DaysAgo(2).LastingMinutes(100)
            .WithWinner(PlayerId)
            .Build();
        sessions.Add(session2);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldIgnoreSessionsWithoutThePlayer()
    {
        var badge = TestBadges.Create(BadgeType.Duration, BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 3; i++)
        {
            var session = new SessionBuilder().DaysAgo(i).LastingMinutes(100)
                .WithWinner(PlayerId + 1)
                .Build();
            sessions.Add(session);
        }

        var own = new SessionBuilder().DaysAgo(3).LastingMinutes(100)
            .WithPlayer(PlayerId)
            .Build();
        sessions.Add(own);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, own, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldSumDurationAcrossMultipleSessions()
    {
        var badge = TestBadges.Create(BadgeType.Duration, BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 6 winning sessions of 50 minutes each = 300 minutes
        for (var i = 0; i < 6; i++)
        {
            var session = new SessionBuilder().DaysAgo(i).LastingMinutes(50)
                .WithWinner(PlayerId)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSessionListIsEmpty()
    {
        var badge = TestBadges.Create(BadgeType.Duration, BadgeLevel.Green);
        var currentSession = new SessionBuilder().LastingMinutes(100).Build();
        var sessions = new List<Session>();

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 299, false)]
    [InlineData(BadgeLevel.Green, 300, true)]
    [InlineData(BadgeLevel.Green, 400, true)]
    [InlineData(BadgeLevel.Blue, 599, false)]
    [InlineData(BadgeLevel.Blue, 600, true)]
    [InlineData(BadgeLevel.Blue, 800, true)]
    [InlineData(BadgeLevel.Red, 2999, false)]
    [InlineData(BadgeLevel.Red, 3000, true)]
    [InlineData(BadgeLevel.Red, 4000, true)]
    [InlineData(BadgeLevel.Gold, 5999, false)]
    [InlineData(BadgeLevel.Gold, 6000, true)]
    [InlineData(BadgeLevel.Gold, 7000, true)]
    public async Task CanAwardBadge_ShouldEvaluatePlayedDurationPerLevel(BadgeLevel level, int minutes, bool expected)
    {
        var badge = TestBadges.Create(BadgeType.Duration, level);
        var sessions = CreateWinningSessionsWithDuration(minutes);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    #endregion

    #region Helper Methods

    private static List<Session> CreateWinningSessionsWithDuration(int totalMinutes)
    {
        var sessions = new List<Session>();
        var sessionDuration = 60; // 60 minutes per session
        var sessionCount = (int)Math.Ceiling(totalMinutes / (double)sessionDuration);
        var remainingMinutes = totalMinutes;

        for (var i = 0; i < sessionCount; i++)
        {
            var duration = Math.Min(sessionDuration, remainingMinutes);
            var session = new SessionBuilder().DaysAgo(i).LastingMinutes(duration)
                .WithWinner(PlayerId)
                .Build();
            sessions.Add(session);
            remainingMinutes -= duration;
        }

        return sessions;
    }

    #endregion
}
