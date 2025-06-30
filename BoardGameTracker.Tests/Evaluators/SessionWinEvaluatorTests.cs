using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class SessionWinEvaluatorTests
{
    private readonly SessionWinEvaluator _evaluator;

    public SessionWinEvaluatorTests()
    {
        _evaluator = new SessionWinEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldReturnWins()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.Wins);
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 3, true)]
    [InlineData(BadgeLevel.Green, 2, false)]
    [InlineData(BadgeLevel.Blue, 10, true)]
    [InlineData(BadgeLevel.Blue, 9, false)]
    [InlineData(BadgeLevel.Red, 25, true)]
    [InlineData(BadgeLevel.Red, 24, false)]
    [InlineData(BadgeLevel.Gold, 50, true)]
    [InlineData(BadgeLevel.Gold, 49, false)]
    public async Task CanAwardBadge_WhenWinCountMatchesLevel_ShouldReturnExpectedResult(
        BadgeLevel level, int winCount, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = level };
        var session = new Session();
        var sessions = CreateSessions(winCount, playerId);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task CanAwardBadge_WhenNoWins_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>();

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenHigherLevelHasEnoughWins_ShouldReturnTrueForLowerLevels()
    {
        var playerId = 1;
        var session = new Session();
        var sessions = CreateSessions(50, playerId);

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

    private static List<Session> CreateSessions(int count, int playerId)
    {
        var sessions = new List<Session>();

        for (var i = 0; i < count; i++)
        {
            sessions.Add(new Session
            {
                PlayerSessions = new List<PlayerSession>
                {
                    new() { Won = true, PlayerId = playerId}
                }
            });
        }

        return sessions;
    }
}