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

public class DurationBadgeEvaluatorTests
{
    private readonly DurationBadgeEvaluator _evaluator;

    public DurationBadgeEvaluatorTests()
    {
        _evaluator = new DurationBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldReturnDuration()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.Duration);
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 300, true)]
    [InlineData(BadgeLevel.Green, 299, false)]
    [InlineData(BadgeLevel.Blue, 600, true)]
    [InlineData(BadgeLevel.Blue, 599, false)]
    [InlineData(BadgeLevel.Red, 3000, true)]
    [InlineData(BadgeLevel.Red, 2999, false)]
    [InlineData(BadgeLevel.Gold, 6000, true)]
    [InlineData(BadgeLevel.Gold, 5999, false)]
    public async Task CanAwardBadge_WhenTotalDurationMatchesLevel_ShouldReturnExpectedResult(
        BadgeLevel level, int totalMinutes, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = level };
        var session = new Session();
        var sessions = CreateSessionsWithTotalDuration(totalMinutes, playerId);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task CanAwardBadge_WhenNoWinningSessions_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>();

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenHigherLevelHasEnoughDuration_ShouldReturnTrueForLowerLevels()
    {
        var playerId = 1;
        var session = new Session();
        var sessions = CreateSessionsWithTotalDuration(6000, playerId);

        var greenBadge = new Badge { Level = BadgeLevel.Green };
        var blueBadge = new Badge { Level = BadgeLevel.Blue };
        var redBadge = new Badge { Level = BadgeLevel.Red };
        var goldBadge = new Badge { Level = BadgeLevel.Gold };

        var greenResult = await _evaluator.CanAwardBadge(playerId, greenBadge, session, sessions);
        var blueResult = await _evaluator.CanAwardBadge(playerId, blueBadge, session, sessions);
        var redResult = await _evaluator.CanAwardBadge(playerId, redBadge, session, sessions);
        var goldResult = await _evaluator.CanAwardBadge(playerId, goldBadge, session, sessions);

        greenResult.Should().BeTrue();
        blueResult.Should().BeTrue();
        redResult.Should().BeTrue();
        goldResult.Should().BeTrue();
    }

    private static List<Session> CreateSessionsWithTotalDuration(int totalMinutes, int playerId)
    {
        var sessions = new List<Session>();
        var baseDate = new DateTime(2025, 1, 1, 10, 0, 0);
        
        if (totalMinutes > 0)
        {
            sessions.Add(new Session
            {
                Start = baseDate,
                End = baseDate.AddMinutes(totalMinutes),
                PlayerSessions = new List<PlayerSession>()
                {
                    new() { Won = true, PlayerId = playerId},
                    new() { Won = false, PlayerId = 2}
                }
            });
        }

        return sessions;
    }
}