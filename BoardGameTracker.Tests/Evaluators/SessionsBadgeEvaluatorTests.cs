using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class SessionsBadgeEvaluatorTests
{
    private readonly SessionsBadgeEvaluator _evaluator;

    public SessionsBadgeEvaluatorTests()
    {
        _evaluator = new SessionsBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeSessions()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.Sessions);
    }

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSessionListIsEmpty()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();
        var currentSession = CreateSession(1);

        var result = await _evaluator.CanAwardBadge(1, badge, currentSession, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.Sessions, "image", null);
        var sessions = CreateSessions(100);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 4, false)]
    [InlineData(BadgeLevel.Green, 5, true)]
    [InlineData(BadgeLevel.Green, 10, true)]
    [InlineData(BadgeLevel.Blue, 9, false)]
    [InlineData(BadgeLevel.Blue, 10, true)]
    [InlineData(BadgeLevel.Blue, 25, true)]
    [InlineData(BadgeLevel.Red, 49, false)]
    [InlineData(BadgeLevel.Red, 50, true)]
    [InlineData(BadgeLevel.Red, 75, true)]
    [InlineData(BadgeLevel.Gold, 99, false)]
    [InlineData(BadgeLevel.Gold, 100, true)]
    [InlineData(BadgeLevel.Gold, 150, true)]
    public async Task CanAwardBadge_ShouldEvaluateSessionCountPerLevel(BadgeLevel level, int sessionCount, bool expected)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessions(sessionCount);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "sessions_title", "sessions_desc", BadgeType.Sessions, "badge.png", level);
    }

    private static Session CreateSession(int gameId)
    {
        var start = DateTime.UtcNow.AddHours(-2);
        var end = DateTime.UtcNow;
        return new Session(gameId, start, end, "Test session");
    }

    private static List<Session> CreateSessions(int count)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < count; i++)
        {
            var start = DateTime.UtcNow.AddDays(-i).AddHours(-2);
            var end = DateTime.UtcNow.AddDays(-i);
            var session = new Session(1, start, end, $"Session {i + 1}");
            sessions.Add(session);
        }
        return sessions;
    }

    #endregion
}
