using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class WinPercentageBadgeEvaluatorTests
{
    private readonly WinPercentageBadgeEvaluator _evaluator;
    private const int PlayerId = 1;

    public WinPercentageBadgeEvaluatorTests()
    {
        _evaluator = new WinPercentageBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeWinPercentage()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.WinPercentage);
    }

    #region Minimum Sessions Requirement Tests

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenLessThan5Sessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWinRate(4, 100); // 4 sessions, 100% win rate

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldEvaluate_WhenExactly5Sessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWinRate(5, 40); // 5 sessions, 40% win rate (2 wins)

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue(); // 40% >= 30%
    }

    #endregion

    #region Green Level Tests (30% win rate)

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnFalse_WhenWinRateIsLessThan30Percent()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWinRate(10, 20); // 2 wins out of 10 = 20%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenWinRateIsExactly30Percent()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWinRate(10, 30); // 3 wins out of 10 = 30%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenWinRateIsMoreThan30Percent()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWinRate(10, 50); // 5 wins out of 10 = 50%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Blue Level Tests (50% win rate)

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnFalse_WhenWinRateIsLessThan50Percent()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithWinRate(10, 40); // 4 wins out of 10 = 40%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenWinRateIsExactly50Percent()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithWinRate(10, 50); // 5 wins out of 10 = 50%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenWinRateIsMoreThan50Percent()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithWinRate(10, 70); // 7 wins out of 10 = 70%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Red Level Tests (65% win rate)

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnFalse_WhenWinRateIsLessThan65Percent()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithWinRate(20, 60); // 12 wins out of 20 = 60%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenWinRateIsExactly65Percent()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithWinRate(20, 65); // 13 wins out of 20 = 65%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenWinRateIsMoreThan65Percent()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithWinRate(20, 80); // 16 wins out of 20 = 80%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Gold Level Tests (80% win rate)

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnFalse_WhenWinRateIsLessThan80Percent()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithWinRate(10, 70); // 7 wins out of 10 = 70%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenWinRateIsExactly80Percent()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithWinRate(10, 80); // 8 wins out of 10 = 80%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenWinRateIsMoreThan80Percent()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithWinRate(10, 100); // 10 wins out of 10 = 100%

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.WinPercentage, "image", null);
        var sessions = CreateSessionsWithWinRate(10, 100);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenZeroWins()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWinRate(10, 0); // 0% win rate

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldHandleOddNumberOfSessions()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        // 7 sessions with 4 wins = 57.14% win rate
        var sessions = new List<Session>();
        for (var i = 0; i < 7; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, i < 4);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue(); // 57.14% >= 50%
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "win_percentage_title", "win_percentage_desc", BadgeType.WinPercentage, "badge.png", level);
    }

    private static Session CreateSession(int gameId, int dayOffset)
    {
        var start = DateTime.UtcNow.AddDays(-dayOffset).AddHours(-2);
        var end = DateTime.UtcNow.AddDays(-dayOffset);
        return new Session(gameId, start, end, $"Session {dayOffset}");
    }

    private static List<Session> CreateSessionsWithWinRate(int totalSessions, int winPercentage)
    {
        var sessions = new List<Session>();
        var winCount = (int)Math.Round(totalSessions * winPercentage / 100.0);

        for (var i = 0; i < totalSessions; i++)
        {
            var session = CreateSession(1, i);
            var won = i < winCount;
            session.AddPlayerSession(PlayerId, null, false, won);
            sessions.Add(session);
        }
        return sessions;
    }

    #endregion
}
