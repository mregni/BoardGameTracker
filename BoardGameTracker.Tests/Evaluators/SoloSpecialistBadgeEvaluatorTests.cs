using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class SoloSpecialistBadgeEvaluatorTests
{
    private readonly SoloSpecialistBadgeEvaluator _evaluator;

    public SoloSpecialistBadgeEvaluatorTests()
    {
        _evaluator = new SoloSpecialistBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldReturnSoloSpecialist()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.SoloSpecialist);
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 5, true)]
    [InlineData(BadgeLevel.Green, 4, false)]
    [InlineData(BadgeLevel.Blue, 10, true)]
    [InlineData(BadgeLevel.Blue, 9, false)]
    [InlineData(BadgeLevel.Red, 25, true)]
    [InlineData(BadgeLevel.Red, 24, false)]
    [InlineData(BadgeLevel.Gold, 50, true)]
    [InlineData(BadgeLevel.Gold, 49, false)]
    public async Task CanAwardBadge_WhenSoloSessionCountMatchesLevel_ShouldReturnExpectedResult(
        BadgeLevel level, int soloSessionCount, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = level };
        var session = new Session();
        var sessions = CreateSessionsWithSoloCount(soloSessionCount, 3);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task CanAwardBadge_WhenNoSessions_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>();

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenOnlyMultiplayerSessions_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = CreateSessionsWithSoloCount(0, 10);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenOnlySoloSessions_ShouldCountCorrectly()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = CreateSessionsWithSoloCount(10, 0);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_WhenHigherLevelHasEnoughSessions_ShouldReturnTrueForLowerLevels()
    {
        var playerId = 1;
        var session = new Session();
        var sessions = CreateSessionsWithSoloCount(50, 0);

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

    [Fact]
    public async Task CanAwardBadge_WhenSessionsHaveVaryingPlayerCounts_ShouldOnlyCountSoloSessions()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>
        {
            new() { PlayerSessions = new List<PlayerSession> { new() } },
            new() { PlayerSessions = new List<PlayerSession> { new(), new() } },
            new() { PlayerSessions = new List<PlayerSession> { new() } },
            new() { PlayerSessions = new List<PlayerSession> { new(), new(), new() } },
            new() { PlayerSessions = new List<PlayerSession> { new() } },
            new() { PlayerSessions = new List<PlayerSession> { new() } },
            new() { PlayerSessions = new List<PlayerSession> { new() } }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    private static List<Session> CreateSessionsWithSoloCount(int soloCount, int multiplayerCount)
    {
        var sessions = new List<Session>();

        for (var i = 0; i < soloCount; i++)
        {
            sessions.Add(new Session
            {
                PlayerSessions = new List<PlayerSession> { new() }
            });
        }

        for (var i = 0; i < multiplayerCount; i++)
        {
            sessions.Add(new Session
            {
                PlayerSessions = new List<PlayerSession> 
                { 
                    new(), 
                    new() 
                }
            });
        }

        return sessions;
    }
}