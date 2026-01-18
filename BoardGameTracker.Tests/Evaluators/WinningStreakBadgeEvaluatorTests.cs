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

    #region Green Level Tests (5 consecutive wins)

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnFalse_WhenStreakIsLessThan5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWinStreak(4);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenStreakIsExactly5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWinStreak(5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenStreakIsMoreThan5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWinStreak(8);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Blue Level Tests (10 consecutive wins)

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnFalse_WhenStreakIsLessThan10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithWinStreak(9);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenStreakIsExactly10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithWinStreak(10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Red Level Tests (15 consecutive wins)

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnFalse_WhenStreakIsLessThan15()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithWinStreak(14);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenStreakIsExactly15()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithWinStreak(15);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Gold Level Tests (25 consecutive wins)

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnFalse_WhenStreakIsLessThan25()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithWinStreak(24);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenStreakIsExactly25()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithWinStreak(25);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Streak Breaking Tests

    [Fact]
    public async Task CanAwardBadge_ShouldStopCountingAtFirstLoss()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 3 recent wins
        for (var i = 0; i < 3; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
        }

        // Then a loss (breaks the streak)
        var lossSession = CreateSession(1, 3);
        lossSession.AddPlayerSession(PlayerId, null, false, false);
        sessions.Add(lossSession);

        // Then 10 more wins (don't count because streak was broken)
        for (var i = 4; i < 14; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Only 3 consecutive wins from most recent
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountFromMostRecentSession()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Most recent session is a loss
        var recentLoss = CreateSession(1, 0);
        recentLoss.AddPlayerSession(PlayerId, null, false, false);
        sessions.Add(recentLoss);

        // Followed by 10 wins
        for (var i = 1; i <= 10; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Streak is 0 because most recent is a loss
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

    [Theory]
    [InlineData(BadgeLevel.Green, 5)]
    [InlineData(BadgeLevel.Blue, 10)]
    [InlineData(BadgeLevel.Red, 15)]
    [InlineData(BadgeLevel.Gold, 25)]
    public async Task CanAwardBadge_ShouldReturnTrue_AtExactThreshold(BadgeLevel level, int streakCount)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessionsWithWinStreak(streakCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
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
