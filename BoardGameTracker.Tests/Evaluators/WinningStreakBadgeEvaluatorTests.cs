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

public class WinningStreakBadgeEvaluatorTests
{
    private readonly WinningStreakBadgeEvaluator _evaluator;

    public WinningStreakBadgeEvaluatorTests()
    {
        _evaluator = new WinningStreakBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldReturnWinningStreak()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.WinningStreak);
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 5, true)]
    [InlineData(BadgeLevel.Green, 4, false)]
    [InlineData(BadgeLevel.Blue, 10, true)]
    [InlineData(BadgeLevel.Blue, 9, false)]
    [InlineData(BadgeLevel.Red, 15, true)]
    [InlineData(BadgeLevel.Red, 14, false)]
    [InlineData(BadgeLevel.Gold, 25, true)]
    [InlineData(BadgeLevel.Gold, 24, false)]
    public async Task CanAwardBadge_WhenWinStreakMatchesLevel_ShouldReturnExpectedResult(
        BadgeLevel level, int winStreakCount, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = level };
        var session = new Session();
        var sessions = CreateSessionsWithWinStreak(playerId, winStreakCount, 0);

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
    public async Task CanAwardBadge_WhenCurrentStreakInterruptedByLoss_ShouldOnlyCountCurrentStreak()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>
        {
            new() { Start = new DateTime(2025, 1, 1), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 2), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 3), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 4), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = false } } },
            new() { Start = new DateTime(2025, 1, 5), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 6), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 7), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 8), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 9), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_WhenMostRecentSessionIsLoss_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>
        {
            new() { Start = new DateTime(2025, 1, 1), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 2), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 3), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 4), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 5), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = true } } },
            new() { Start = new DateTime(2025, 1, 6), PlayerSessions = new List<PlayerSession> { new() { PlayerId = playerId, Won = false } } }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenMultiplePlayersInSessions_ShouldOnlyConsiderTargetPlayer()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>
        {
            new()
            {
                Start = new DateTime(2025, 1, 1),
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId, Won = true },
                    new() { PlayerId = 2, Won = false }
                }
            },
            new()
            {
                Start = new DateTime(2025, 1, 2),
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId, Won = true },
                    new() { PlayerId = 2, Won = true }
                }
            },
            new()
            {
                Start = new DateTime(2025, 1, 3),
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId, Won = true },
                    new() { PlayerId = 3, Won = false }
                }
            },
            new()
            {
                Start = new DateTime(2025, 1, 4),
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId, Won = true },
                    new() { PlayerId = 2, Won = false }
                }
            },
            new()
            {
                Start = new DateTime(2025, 1, 5),
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId, Won = true },
                    new() { PlayerId = 3, Won = true }
                }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }
    private static List<Session> CreateSessionsWithWinStreak(int playerId, int winCount, int lossCount)
    {
        var sessions = new List<Session>();
        var currentDate = new DateTime(2025, 1, 1);

        for (int i = 0; i < winCount; i++)
        {
            sessions.Add(new Session
            {
                Start = currentDate.AddDays(i),
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId, Won = true }
                }
            });
        }

        for (int i = 0; i < lossCount; i++)
        {
            sessions.Add(new Session
            {
                Start = currentDate.AddDays(winCount + i),
                PlayerSessions = new List<PlayerSession>
                {
                    new() { PlayerId = playerId, Won = false }
                }
            });
        }

        return sessions;
    }
}