using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class SessionWinEvaluatorTests
{
    private readonly SessionWinEvaluator _evaluator;
    private const int PlayerId = 1;

    public SessionWinEvaluatorTests()
    {
        _evaluator = new SessionWinEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeWins()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.Wins);
    }

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNoWins()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWins(0, 10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.Wins, "image", null);
        var sessions = CreateSessionsWithWins(50, 50);

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
    [InlineData(BadgeLevel.Green, 2, false)]
    [InlineData(BadgeLevel.Green, 3, true)]
    [InlineData(BadgeLevel.Green, 5, true)]
    [InlineData(BadgeLevel.Blue, 9, false)]
    [InlineData(BadgeLevel.Blue, 10, true)]
    [InlineData(BadgeLevel.Blue, 15, true)]
    [InlineData(BadgeLevel.Red, 24, false)]
    [InlineData(BadgeLevel.Red, 25, true)]
    [InlineData(BadgeLevel.Red, 35, true)]
    [InlineData(BadgeLevel.Gold, 49, false)]
    [InlineData(BadgeLevel.Gold, 50, true)]
    [InlineData(BadgeLevel.Gold, 60, true)]
    public async Task CanAwardBadge_ShouldEvaluateWinCountPerLevel(BadgeLevel level, int winCount, bool expected)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessionsWithWins(winCount, winCount + 5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyCountWinsForSpecifiedPlayer()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create sessions where player 1 has 2 wins but player 2 has many wins
        for (var i = 0; i < 5; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, i < 2); // Player 1 wins first 2
            session.AddPlayerSession(2, null, false, i >= 2); // Player 2 wins the rest
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Player 1 only has 2 wins, needs 3
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "wins_title", "wins_desc", BadgeType.Wins, "badge.png", level);
    }

    private static Session CreateSession(int gameId, int dayOffset)
    {
        var start = DateTime.UtcNow.AddDays(-dayOffset).AddHours(-2);
        var end = DateTime.UtcNow.AddDays(-dayOffset);
        return new Session(gameId, start, end, $"Session {dayOffset}");
    }

    private static List<Session> CreateSessionsWithWins(int winCount, int totalSessions)
    {
        var sessions = new List<Session>();
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
