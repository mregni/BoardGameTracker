using System;
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

    public MarathonRunnerBadgeEvaluatorTests()
    {
        _evaluator = new MarathonRunnerBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldReturnMarathonRunner()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.MarathonRunner);
    }

    [Theory]
    [InlineData(240, true)]
    [InlineData(239, false)]
    [InlineData(241, true)]
    [InlineData(0, false)]
    [InlineData(480, true)]
    public async Task CanAwardBadge_WhenSessionDurationMatchesThreshold_ShouldReturnExpectedResult(
        int durationMinutes, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            Start = new DateTime(2025, 1, 1, 10, 0, 0),
            End = new DateTime(2025, 1, 1, 10, 0, 0).AddMinutes(durationMinutes)
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task CanAwardBadge_WhenSessionIsExactly4Hours_ShouldReturnTrue()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            Start = new DateTime(2025, 1, 1, 10, 0, 0),
            End = new DateTime(2025, 1, 1, 14, 0, 0)
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_WhenSessionIsLessThan4Hours_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            Start = new DateTime(2025, 1, 1, 10, 0, 0),
            End = new DateTime(2025, 1, 1, 13, 59, 0)
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenSessionSpansMultipleDays_ShouldCalculateCorrectDuration()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            Start = new DateTime(2025, 1, 1, 22, 0, 0),
            End = new DateTime(2025, 1, 2, 2, 0, 0)
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeTrue();
    }
}