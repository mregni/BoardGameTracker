using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

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

    #region Green Level Tests (300 minutes = 5 hours)

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnFalse_WhenWinningDurationIsLessThan300Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateWinningSessionsWithDuration(299);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenWinningDurationIsExactly300Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateWinningSessionsWithDuration(300);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenWinningDurationIsMoreThan300Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateWinningSessionsWithDuration(400);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Blue Level Tests (600 minutes = 10 hours)

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnFalse_WhenWinningDurationIsLessThan600Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateWinningSessionsWithDuration(599);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenWinningDurationIsExactly600Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateWinningSessionsWithDuration(600);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenWinningDurationIsMoreThan600Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateWinningSessionsWithDuration(800);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Red Level Tests (3000 minutes = 50 hours)

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnFalse_WhenWinningDurationIsLessThan3000Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateWinningSessionsWithDuration(2999);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenWinningDurationIsExactly3000Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateWinningSessionsWithDuration(3000);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenWinningDurationIsMoreThan3000Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateWinningSessionsWithDuration(4000);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Gold Level Tests (6000 minutes = 100 hours)

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnFalse_WhenWinningDurationIsLessThan6000Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateWinningSessionsWithDuration(5999);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenWinningDurationIsExactly6000Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateWinningSessionsWithDuration(6000);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenWinningDurationIsMoreThan6000Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateWinningSessionsWithDuration(7000);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

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
    public async Task CanAwardBadge_ShouldOnlyCountWinningSessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create sessions where player lost but played for 400 minutes total
        // and won for only 200 minutes
        for (var i = 0; i < 4; i++)
        {
            var session = CreateSessionWithDuration(100, i);
            session.AddPlayerSession(PlayerId, null, false, false); // Lost
            sessions.Add(session);
        }

        for (var i = 0; i < 2; i++)
        {
            var session = CreateSessionWithDuration(100, i + 4);
            session.AddPlayerSession(PlayerId, null, false, true); // Won
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Only 200 minutes of wins, needs 300
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNoWins()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 10; i++)
        {
            var session = CreateSessionWithDuration(100, i);
            session.AddPlayerSession(PlayerId, null, false, false); // All losses
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldSumDurationAcrossMultipleSessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 6 winning sessions of 50 minutes each = 300 minutes
        for (var i = 0; i < 6; i++)
        {
            var session = CreateSessionWithDuration(50, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 300)]
    [InlineData(BadgeLevel.Blue, 600)]
    [InlineData(BadgeLevel.Red, 3000)]
    [InlineData(BadgeLevel.Gold, 6000)]
    public async Task CanAwardBadge_ShouldReturnTrue_AtExactThreshold(BadgeLevel level, int minutes)
    {
        var badge = CreateBadge(level);
        var sessions = CreateWinningSessionsWithDuration(minutes);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 299)]
    [InlineData(BadgeLevel.Blue, 599)]
    [InlineData(BadgeLevel.Red, 2999)]
    [InlineData(BadgeLevel.Gold, 5999)]
    public async Task CanAwardBadge_ShouldReturnFalse_JustBelowThreshold(BadgeLevel level, int minutes)
    {
        var badge = CreateBadge(level);
        var sessions = CreateWinningSessionsWithDuration(minutes);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "duration_title", "duration_desc", BadgeType.Duration, "badge.png", level);
    }

    private static Session CreateSessionWithDuration(int durationMinutes, int dayOffset)
    {
        var start = DateTime.UtcNow.AddDays(-dayOffset).AddMinutes(-durationMinutes);
        var end = DateTime.UtcNow.AddDays(-dayOffset);
        return new Session(1, start, end, $"Session {dayOffset}");
    }

    private static List<Session> CreateWinningSessionsWithDuration(int totalMinutes)
    {
        var sessions = new List<Session>();
        var sessionDuration = 60; // 60 minutes per session
        var sessionCount = (int)Math.Ceiling(totalMinutes / (double)sessionDuration);
        var remainingMinutes = totalMinutes;

        for (var i = 0; i < sessionCount; i++)
        {
            var duration = Math.Min(sessionDuration, remainingMinutes);
            var session = CreateSessionWithDuration(duration, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
            remainingMinutes -= duration;
        }

        return sessions;
    }

    #endregion
}
