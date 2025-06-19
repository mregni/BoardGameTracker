using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class FirstTryBadgeEvaluatorTests
{
    private readonly FirstTryBadgeEvaluator _evaluator;

    public FirstTryBadgeEvaluatorTests()
    {
        _evaluator = new FirstTryBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldReturnFirstTry()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.FirstTry);
    }

    [Theory]
    [InlineData(1, true, true)]
    [InlineData(1, false, false)]
    [InlineData(2, true, false)]
    [InlineData(2, false, false)]
    [InlineData(5, true, false)]
    [InlineData(5, false, false)]
    public async Task CanAwardBadge_WhenDifferentScenariosParameterized_ShouldReturnExpectedResult(
        int sessionCount, bool playerWon, bool expectedResult)
    {
        var playerId = 1;
        var gameId = 100;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = gameId,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Won = playerWon }
            }
        };
        
        var dbSessions = new List<Session>();
        for (var i = 0; i < sessionCount; i++)
        {
            dbSessions.Add(new Session () { GameId = gameId});
        }

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, dbSessions);

        result.Should().Be(expectedResult);
    }
}