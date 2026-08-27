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

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenLessThan3Sessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithScores([100.0, 90.0]);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    public static TheoryData<double[], bool> ScorePatterns => new()
    {
        { [100.0, 90.0, 80.0], true },
        { [80.0, 90.0, 100.0], false },
        { [100.0, 100.0, 100.0], false },
        { [95.0, 100.0, 90.0], false },
        { [100.0, 80.0, 90.0], false },
        { [100.0, 90.0, 90.0], false },
        { [1000.0, 500.0, 100.0], true },
        { [100.1, 100.05, 100.0], true }
    };

    [Theory]
    [MemberData(nameof(ScorePatterns))]
    public async Task CanAwardBadge_ShouldEvaluateScoreProgression(double[] scoresMostRecentFirst, bool expected)
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithScores(scoresMostRecentFirst);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

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

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyConsiderSessionsOfCurrentGame()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 2; i++)
        {
            var session = CreateSession(GameId, i);
            session.AddPlayerSession(PlayerId, 100 - i * 10, false, false);
            sessions.Add(session);
        }

        for (var i = 2; i < 5; i++)
        {
            var session = CreateSession(GameId + i, i);
            session.AddPlayerSession(PlayerId, 100 - i * 10, false, false);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyUseThreeMostRecentSessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithScores([100.0, 90.0, 80.0, 95.0, 85.0]);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOrderSessionsByStartDate_NotListOrder()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var newest = CreateSession(GameId, 0);
        newest.AddPlayerSession(PlayerId, 100, false, false);
        var middle = CreateSession(GameId, 1);
        middle.AddPlayerSession(PlayerId, 90, false, false);
        var oldest = CreateSession(GameId, 2);
        oldest.AddPlayerSession(PlayerId, 80, false, false);
        var sessions = new List<Session> { oldest, newest, middle };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, newest, sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(BadgeLevel.Green)]
    [InlineData(BadgeLevel.Blue)]
    [InlineData(BadgeLevel.Red)]
    [InlineData(BadgeLevel.Gold)]
    public async Task CanAwardBadge_ShouldIgnoreBadgeLevel(BadgeLevel? level)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessionsWithScores([100.0, 90.0, 80.0]);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

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

    private static List<Session> CreateSessionsWithScores(double[] scoresMostRecentFirst)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < scoresMostRecentFirst.Length; i++)
        {
            var session = CreateSession(GameId, i);
            session.AddPlayerSession(PlayerId, scoresMostRecentFirst[i], false, false);
            sessions.Add(session);
        }
        return sessions;
    }
}
