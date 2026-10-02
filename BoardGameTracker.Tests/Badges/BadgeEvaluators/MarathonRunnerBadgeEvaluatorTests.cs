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

public class MarathonRunnerBadgeEvaluatorTests
{
    private readonly MarathonRunnerBadgeEvaluator _evaluator;
    private const int PlayerId = 1;

    public MarathonRunnerBadgeEvaluatorTests()
    {
        _evaluator = new MarathonRunnerBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeMarathonRunner()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.MarathonRunner);
    }

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyCheckCurrentSession_NotPlayerHistory()
    {
        var badge = TestBadges.Create(BadgeType.MarathonRunner, BadgeLevel.Green);
        var currentSession = new SessionBuilder().LastingMinutes(100).Build(); // Short current session

        // Create a list with long sessions in history but short current session
        var sessions = new List<Session>
        {
            currentSession,
            new SessionBuilder().LastingMinutes(500).Build(), // 500 minutes
            new SessionBuilder().LastingMinutes(600).Build()  // 600 minutes
        };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse(); // Should only check current session
    }

    [Theory]
    [InlineData(239, false)]
    [InlineData(240, true)]
    [InlineData(241, true)]
    [InlineData(600, true)]
    public async Task CanAwardBadge_ShouldHandleVariousDurations(int durationMinutes, bool expectedResult)
    {
        var badge = TestBadges.Create(BadgeType.MarathonRunner, BadgeLevel.Green);
        var session = new SessionBuilder().LastingMinutes(durationMinutes).Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(BadgeLevel.Green)]
    [InlineData(BadgeLevel.Blue)]
    [InlineData(BadgeLevel.Red)]
    [InlineData(BadgeLevel.Gold)]
    public async Task CanAwardBadge_ShouldIgnoreBadgeLevel(BadgeLevel? level)
    {
        var badge = TestBadges.Create(BadgeType.MarathonRunner, level);
        var session = new SessionBuilder().LastingMinutes(240).Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    #endregion
}
