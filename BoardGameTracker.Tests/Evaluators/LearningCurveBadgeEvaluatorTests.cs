using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class LearningCurveBadgeEvaluatorTests
{
    private readonly LearningCurveBadgeEvaluator _evaluator;

    public LearningCurveBadgeEvaluatorTests()
    {
        _evaluator = new LearningCurveBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldReturnLearningCurve()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.LearningCurve);
    }

    [Fact]
    public async Task CanAwardBadge_WhenLessThan3Sessions_ShouldReturnFalse()
    {
        var playerId = 1;
        var gameId = 100;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session { GameId = gameId };
        var sessions = CreateSessionsWithScores(playerId, gameId, [10.0, 8.0]);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenAnyScoreIsNull_ShouldReturnFalse()
    {
        var playerId = 1;
        var gameId = 100;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session { GameId = gameId };
        var sessions = new List<Session>
        {
            CreateSessionWithScore(playerId, gameId, new DateTime(2025, 1, 1), 10.0),
            CreateSessionWithScore(playerId, gameId, new DateTime(2025, 1, 2), null),
            CreateSessionWithScore(playerId, gameId, new DateTime(2025, 1, 3), 6.0)
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(new[] { 6.0, 8.0, 10.0 }, true)]
    [InlineData(new[] { 2.0, 7.0, 10.0 }, true)]
    [InlineData(new[] { 10.0, 8.0, 8.0 }, false)]
    [InlineData(new[] { 10.0, 10.0, 6.0 }, false)]
    [InlineData(new[] { 8.0, 10.0, 6.0 }, false)]
    [InlineData(new[] { 10, 8.0, 6.0 }, false)]
    [InlineData(new[] { 10.0, 6.0, 8.0 }, false)]
    public async Task CanAwardBadge_WhenDifferentScorePatterns_ShouldReturnExpectedResult(
        double[] scores, bool expectedResult)
    {
        var playerId = 1;
        var gameId = 100;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session { GameId = gameId };
        var sessions = CreateSessionsWithScores(playerId, gameId, scores);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task CanAwardBadge_WhenMoreThan3Sessions_ShouldOnlyConsiderFirst3()
    {
        var playerId = 1;
        var gameId = 100;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session { GameId = gameId };
        var sessions = CreateSessionsWithScores(playerId, gameId, [10.0, 8.0, 6.0, 12.0, 15.0]);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_WhenSessionsOutOfOrder_ShouldOrderByStartTime()
    {
        var playerId = 1;
        var gameId = 100;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session { GameId = gameId };
        var sessions = new List<Session>
        {
            CreateSessionWithScore(playerId, gameId, new DateTime(2025, 1, 3), 6.0),
            CreateSessionWithScore(playerId, gameId, new DateTime(2025, 1, 1), 10.0),  
            CreateSessionWithScore(playerId, gameId, new DateTime(2025, 1, 2), 8.0),
            CreateSessionWithScore(playerId, gameId, new DateTime(2025, 1, 4), 12.0)
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenMultiplePlayersInSessions_ShouldOnlyConsiderTargetPlayer()
    {
        var playerId = 1;
        var gameId = 100;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session { GameId = gameId };
        var sessions = new List<Session>
        {
            new()
            {
                GameId = gameId,
                Start = new DateTime(2025, 1, 1),
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId, Score = 6.0 },
                    new() { PlayerId = 2, Score = 5.0 }
                }
            },
            new()
            {
                GameId = gameId,
                Start = new DateTime(2025, 1, 2),
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId, Score = 8.0 },
                    new() { PlayerId = 2, Score = 7.0 }
                }
            },
            new()
            {
                GameId = gameId,
                Start = new DateTime(2025, 1, 3),
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId, Score = 10.0 },
                    new() { PlayerId = 2, Score = 9.0 }
                }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    private static List<Session> CreateSessionsWithScores(int playerId, int gameId, double[] scores)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < scores.Length; i++)
        {
            sessions.Add(CreateSessionWithScore(playerId, gameId, new DateTime(2025, 1, i + 1), scores[i]));
        }
        return sessions;
    }

    private static Session CreateSessionWithScore(int playerId, int gameId, DateTime start, double? score)
    {
        return new Session
        {
            GameId = gameId,
            Start = start,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = score }
            }
        };
    }
}