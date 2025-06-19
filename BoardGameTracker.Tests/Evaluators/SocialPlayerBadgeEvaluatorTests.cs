using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class SocialPlayerBadgeEvaluatorTests
{
    private readonly SocialPlayerBadgeEvaluator _evaluator;

    public SocialPlayerBadgeEvaluatorTests()
    {
        _evaluator = new SocialPlayerBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldReturnSocialPlayer()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.SocialPlayer);
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
    public async Task CanAwardBadge_WhenDistinctPlayerCountMatchesLevel_ShouldReturnExpectedResult(
        BadgeLevel level, int distinctPlayerCount, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = level };
        var session = new Session();
        var sessions = CreateSessionsWithDistinctPlayers(playerId, distinctPlayerCount);

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
    public async Task CanAwardBadge_WhenOnlySoloSessions_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>
        {
            new()
            {
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId }
                }
            },
            new()
            {
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId }
                }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenSamePlayersInMultipleSessions_ShouldCountDistinctOnly()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>
        {
            new()
            {
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId },
                    new() { PlayerId = 2 },
                    new() { PlayerId = 3 }
                }
            },
            new()
            {
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId },
                    new() { PlayerId = 2 },
                    new() { PlayerId = 4 }
                }
            },
            new()
            {
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId },
                    new() { PlayerId = 3 },
                    new() { PlayerId = 5 },
                    new() { PlayerId = 6 }
                }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_WhenPlayerIdExcludedFromCount_ShouldNotCountSelf_ReturnTrue()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>
        {
            new()
            {
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId },
                    new() { PlayerId = 2 },
                    new() { PlayerId = 3 },
                    new() { PlayerId = 4 },
                    new() { PlayerId = 5 },
                    new() { PlayerId = 6 }
                }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task CanAwardBadge_WhenPlayerIdExcludedFromCount_ShouldNotCountSelf_ReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>
        {
            new()
            {
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId },
                    new() { PlayerId = 2 },
                    new() { PlayerId = 3 },
                    new() { PlayerId = 4 },
                    new() { PlayerId = 5 },
                }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    private static List<Session> CreateSessionsWithDistinctPlayers(int playerId, int distinctPlayerCount)
    {
        var sessions = new List<Session>();
        var playerSessions = new List<PlayerSession> { new() { PlayerId = playerId } };

        for (int i = 0; i < distinctPlayerCount; i++)
        {
            playerSessions.Add(new PlayerSession { PlayerId = playerId + i + 1 });
        }

        sessions.Add(new Session { PlayerSessions = playerSessions });
        return sessions;
    }
}