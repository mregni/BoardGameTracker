using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

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

    #region Duration Threshold Tests (240 minutes = 4 hours)

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSessionDurationIsLessThan240Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSessionWithDuration(239);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenSessionDurationIsExactly240Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSessionWithDuration(240);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenSessionDurationIsMoreThan240Minutes()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSessionWithDuration(300);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyCheckCurrentSession_NotPlayerHistory()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var currentSession = CreateSessionWithDuration(100); // Short current session

        // Create a list with long sessions in history but short current session
        var sessions = new List<Session>
        {
            currentSession,
            CreateSessionWithDuration(500), // 500 minutes
            CreateSessionWithDuration(600)  // 600 minutes
        };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse(); // Should only check current session
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_ForVeryLongSession()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSessionWithDuration(600); // 10 hours
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(239, false)]
    [InlineData(240, true)]
    [InlineData(241, true)]
    [InlineData(300, true)]
    [InlineData(360, true)]
    [InlineData(480, true)]
    public async Task CanAwardBadge_ShouldHandleVariousDurations(int durationMinutes, bool expectedResult)
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSessionWithDuration(durationMinutes);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldWorkWithAnyBadgeLevel()
    {
        // MarathonRunner doesn't use levels, so it should work with any level
        var session = CreateSessionWithDuration(240);
        var sessions = new List<Session> { session };

        foreach (BadgeLevel level in Enum.GetValues(typeof(BadgeLevel)))
        {
            var badge = CreateBadge(level);
            var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);
            result.Should().BeTrue();
        }
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenBadgeLevelIsNull()
    {
        // MarathonRunner doesn't check the badge level, only the session duration
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.MarathonRunner, "image", null);
        var session = CreateSessionWithDuration(240);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "marathon_runner_title", "marathon_runner_desc", BadgeType.MarathonRunner, "badge.png", level);
    }

    private static Session CreateSessionWithDuration(int durationMinutes)
    {
        var start = DateTime.UtcNow.AddMinutes(-durationMinutes);
        var end = DateTime.UtcNow;
        return new Session(1, start, end, "Marathon session");
    }

    #endregion
}
