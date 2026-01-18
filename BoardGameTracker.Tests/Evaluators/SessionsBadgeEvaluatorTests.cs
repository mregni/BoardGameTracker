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

    #region Green Level Tests (5 sessions)

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnFalse_WhenSessionCountIsLessThan5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessions(4);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenSessionCountIsExactly5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessions(5);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenSessionCountIsMoreThan5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessions(10);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Blue Level Tests (10 sessions)

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnFalse_WhenSessionCountIsLessThan10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessions(9);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenSessionCountIsExactly10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessions(10);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenSessionCountIsMoreThan10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessions(25);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Red Level Tests (50 sessions)

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnFalse_WhenSessionCountIsLessThan50()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessions(49);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenSessionCountIsExactly50()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessions(50);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenSessionCountIsMoreThan50()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessions(75);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Gold Level Tests (100 sessions)

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnFalse_WhenSessionCountIsLessThan100()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessions(99);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenSessionCountIsExactly100()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessions(100);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenSessionCountIsMoreThan100()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessions(150);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

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
    [InlineData(BadgeLevel.Green, 5)]
    [InlineData(BadgeLevel.Blue, 10)]
    [InlineData(BadgeLevel.Red, 50)]
    [InlineData(BadgeLevel.Gold, 100)]
    public async Task CanAwardBadge_ShouldReturnTrue_AtExactThreshold(BadgeLevel level, int sessionCount)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessions(sessionCount);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 4)]
    [InlineData(BadgeLevel.Blue, 9)]
    [InlineData(BadgeLevel.Red, 49)]
    [InlineData(BadgeLevel.Gold, 99)]
    public async Task CanAwardBadge_ShouldReturnFalse_JustBelowThreshold(BadgeLevel level, int sessionCount)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessions(sessionCount);

        var result = await _evaluator.CanAwardBadge(1, badge, sessions[0], sessions);

        result.Should().BeFalse();
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
