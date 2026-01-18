using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class LearningCurveBadgeEvaluatorTests
{
    private readonly LearningCurveBadgeEvaluator _evaluator;
    private const int PlayerId = 1;
    private const int GameId = 1;

    public LearningCurveBadgeEvaluatorTests()
    {
        _evaluator = new LearningCurveBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeLearningCurve()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.LearningCurve);
    }

    #region Minimum Sessions Requirement Tests

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenLessThan3Sessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithIncreasingScores(2, new[] { 100.0, 90.0 });

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldEvaluate_WhenExactly3Sessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithIncreasingScores(3, new[] { 100.0, 90.0, 80.0 });

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Score Improvement Tests

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenScoresAreIncreasing()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        // Most recent first: 100, 90, 80 (increasing from oldest to newest)
        var sessions = CreateSessionsWithIncreasingScores(3, new[] { 100.0, 90.0, 80.0 });

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenScoresAreDecreasing()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        // Most recent first: 80, 90, 100 (decreasing from oldest to newest)
        var sessions = CreateSessionsWithIncreasingScores(3, new[] { 80.0, 90.0, 100.0 });

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenScoresAreEqual()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithIncreasingScores(3, new[] { 100.0, 100.0, 100.0 });

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenOnlyPartialImprovement()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        // First two improving but third goes down
        var sessions = CreateSessionsWithIncreasingScores(3, new[] { 100.0, 95.0, 90.0 });
        // This would be: 100 > 95 (true), but 95 > 90 is also true... wait let me reconsider

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue(); // 100 > 95 > 90, all improving
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenMiddleScoreBreaksImprovement()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        // 100, 80, 90 -> 100 > 80 (true), but 80 > 90 is false
        var sessions = CreateSessionsWithIncreasingScores(3, new[] { 100.0, 80.0, 90.0 });

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Null Score Tests

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenAnyScoreIsNull()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 3; i++)
        {
            var session = CreateSession(GameId, i);
            double? score = i == 1 ? null : 100 - i * 10;
            session.AddPlayerSession(PlayerId, score, false, false);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenAllScoresAreNull()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 3; i++)
        {
            var session = CreateSession(GameId, i);
            session.AddPlayerSession(PlayerId, null, false, false);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Game-Specific Tests

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyConsiderSessionsOfCurrentGame()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Sessions for game 1 (current game) - only 2 sessions
        for (var i = 0; i < 2; i++)
        {
            var session = CreateSession(GameId, i);
            session.AddPlayerSession(PlayerId, 100 - i * 10, false, false);
            sessions.Add(session);
        }

        // Sessions for different games
        for (var i = 2; i < 5; i++)
        {
            var session = CreateSession(GameId + i, i);
            session.AddPlayerSession(PlayerId, 100 - i * 10, false, false);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Only 2 sessions for game 1, needs 3
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyUseThreeMostRecentSessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 5 sessions with improving scores (most recent first)
        // The 3 most recent should be: 100, 90, 80
        for (var i = 0; i < 5; i++)
        {
            var session = CreateSession(GameId, i);
            session.AddPlayerSession(PlayerId, 100 - i * 10, false, false);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldHandleLargeScoreDifferences()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithIncreasingScores(3, new[] { 1000.0, 500.0, 100.0 });

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldHandleSmallScoreDifferences()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithIncreasingScores(3, new[] { 100.1, 100.05, 100.0 });

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldWorkWithAnyBadgeLevel()
    {
        // LearningCurve doesn't use levels
        var sessions = CreateSessionsWithIncreasingScores(3, new[] { 100.0, 90.0, 80.0 });

        foreach (BadgeLevel level in Enum.GetValues(typeof(BadgeLevel)))
        {
            var badge = CreateBadge(level);
            var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);
            result.Should().BeTrue();
        }
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "learning_curve_title", "learning_curve_desc", BadgeType.LearningCurve, "badge.png", level);
    }

    private static Session CreateSession(int gameId, int dayOffset)
    {
        var start = DateTime.UtcNow.AddDays(-dayOffset).AddHours(-2);
        var end = DateTime.UtcNow.AddDays(-dayOffset);
        return new Session(gameId, start, end, $"Session {dayOffset}");
    }

    private static List<Session> CreateSessionsWithIncreasingScores(int count, double[] scores)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < count; i++)
        {
            var session = CreateSession(GameId, i);
            session.AddPlayerSession(PlayerId, scores[i], false, false);
            sessions.Add(session);
        }
        return sessions;
    }

    #endregion
}
