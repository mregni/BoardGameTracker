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

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenLessThan5Sessions()
    {
        var badge = TestBadges.Create(BadgeType.WinPercentage, BadgeLevel.Green);
        var sessions = CreateSessionsWithWins(4, 4);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldEvaluate_WhenExactly5Sessions()
    {
        var badge = TestBadges.Create(BadgeType.WinPercentage, BadgeLevel.Green);
        var sessions = CreateSessionsWithWins(2, 5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 2, 10, false)]
    [InlineData(BadgeLevel.Green, 3, 10, true)]
    [InlineData(BadgeLevel.Green, 5, 10, true)]
    [InlineData(BadgeLevel.Blue, 4, 10, false)]
    [InlineData(BadgeLevel.Blue, 5, 10, true)]
    [InlineData(BadgeLevel.Blue, 7, 10, true)]
    [InlineData(BadgeLevel.Blue, 4, 7, true)]
    [InlineData(BadgeLevel.Red, 12, 20, false)]
    [InlineData(BadgeLevel.Red, 16, 25, false)]
    [InlineData(BadgeLevel.Red, 13, 20, true)]
    [InlineData(BadgeLevel.Red, 16, 20, true)]
    [InlineData(BadgeLevel.Gold, 7, 10, false)]
    [InlineData(BadgeLevel.Gold, 8, 10, true)]
    [InlineData(BadgeLevel.Gold, 10, 10, true)]
    public async Task CanAwardBadge_ShouldEvaluateWinPercentagePerLevel(BadgeLevel level, int winCount, int totalSessions, bool expected)
    {
        var badge = TestBadges.Create(BadgeType.WinPercentage, level);
        var sessions = CreateSessionsWithWins(winCount, totalSessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.WinPercentage, "image", null);
        var sessions = CreateSessionsWithWins(10, 10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenZeroWins()
    {
        var badge = TestBadges.Create(BadgeType.WinPercentage, BadgeLevel.Green);
        var sessions = CreateSessionsWithWins(0, 10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    private static List<Session> CreateSessionsWithWins(int winCount, int totalSessions)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < totalSessions; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithPlayer(PlayerId, won: i < winCount)
                .Build();
            sessions.Add(session);
        }
        return sessions;
    }
}
