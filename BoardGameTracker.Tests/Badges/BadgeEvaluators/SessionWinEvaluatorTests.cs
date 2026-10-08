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
        var badge = TestBadges.Create(BadgeType.Wins, BadgeLevel.Green);
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
        var badge = TestBadges.Create(BadgeType.Wins, BadgeLevel.Green);
        var currentSession = new SessionBuilder().ForGame(1).Build();
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
        var badge = TestBadges.Create(BadgeType.Wins, level);
        var sessions = CreateSessionsWithWins(winCount, winCount + 5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyCountWinsForSpecifiedPlayer()
    {
        var badge = TestBadges.Create(BadgeType.Wins, BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create sessions where player 1 has 2 wins but player 2 has many wins
        for (var i = 0; i < 5; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithPlayer(PlayerId, won: i < 2)
                .WithPlayer(2, won: i >= 2)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Player 1 only has 2 wins, needs 3
    }

    #endregion

    #region Helper Methods

    private static List<Session> CreateSessionsWithWins(int winCount, int totalSessions)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < totalSessions; i++)
        {
            sessions.Add(new SessionBuilder().ForGame(1).DaysAgo(i).WithPlayer(PlayerId, won: i < winCount).Build());
        }
        return sessions;
    }

    #endregion
}
