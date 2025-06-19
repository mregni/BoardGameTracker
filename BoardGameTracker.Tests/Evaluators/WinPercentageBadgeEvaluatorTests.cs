using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class WinPercentageBadgeEvaluatorTests
{
    private readonly WinPercentageBadgeEvaluator _evaluator;

    public WinPercentageBadgeEvaluatorTests()
    {
        _evaluator = new WinPercentageBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldReturnWinPercentage()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.WinPercentage);
    }

    [Fact]
    public async Task CanAwardBadge_WhenTotalSessionsLessThan5_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge {Level = BadgeLevel.Green};
        var session = new Session();
        var sessions = new List<Session>
        {
            new()
            {
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = true}
                }
            },
            new()
            {
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = false}
                }
            },
            new()
            {
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = false}
                }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenTotalSessionsExactly5_ShouldEvaluateWinPercentage()
    {
        var playerId = 1;
        var badge = new Badge {Level = BadgeLevel.Green};
        var session = new Session();

        var sessions = new List<Session>
        {
            new()
            {
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = true}
                }
            },
            new()
            {
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = true}
                }
            },
            new()
            {
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = false}
                }
            },
            new()
            {
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = false}
                }
            },
            new()
            {
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = false}
                }
            }
        };
        
        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 100, true)]
    [InlineData(BadgeLevel.Green, 30, true)]
    [InlineData(BadgeLevel.Green, 29, false)]
    [InlineData(BadgeLevel.Blue, 100, true)]
    [InlineData(BadgeLevel.Blue, 50, true)]
    [InlineData(BadgeLevel.Blue, 49, false)]
    [InlineData(BadgeLevel.Red, 100, true)]
    [InlineData(BadgeLevel.Red, 65, true)]
    [InlineData(BadgeLevel.Red, 64, false)]
    [InlineData(BadgeLevel.Gold, 100, true)]
    [InlineData(BadgeLevel.Gold, 80, true)]
    [InlineData(BadgeLevel.Gold, 79, false)]
    public async Task CanAwardBadge_WhenWinPercentageMatchesLevel_ShouldReturnExpectedResult(
        BadgeLevel level, int winPercentage, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge {Level = level};
        var session = new Session();

        var totalSessions = 100;
        var wonSessions = (int) (totalSessions * winPercentage / 100.0);
        var lostSessions = totalSessions - wonSessions;

        var sessionsLost = Enumerable.Range(0, lostSessions)
            .Select(_ => new Session(){
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = false}
                }
            })
            .ToList();
        var sessionsWon = Enumerable.Range(0, wonSessions)
            .Select(_ => new Session(){
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = true}
                }
            })
            .ToList();

        var sessions = new List<Session>();
        sessions.AddRange(sessionsLost);
        sessions.AddRange(sessionsWon);
        
        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task CanAwardBadge_WhenNoSessionsWon_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge {Level = BadgeLevel.Green};
        var session = new Session();
        var sessions = Enumerable.Range(0, 10)
            .Select(_ => new Session(){
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = false}
                }
            })
            .ToList();
        
        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenRepositoryCallsAreMade_ShouldCallWithCorrectParameters()
    {
        var playerId = 42;
        var badge = new Badge {Level = BadgeLevel.Green};
        var session = new Session();

        var sessionsLost = Enumerable.Range(0, 2)
            .Select(_ => new Session(){
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = false}
                }
            })
            .ToList();
        var sessionsWon = Enumerable.Range(0, 8)
            .Select(_ => new Session(){
                PlayerSessions = new List<PlayerSession>()
                {
                    new() {PlayerId = playerId, Won = true}
                }
            })
            .ToList();
        
        var sessions = new List<Session>();
        sessions.AddRange(sessionsLost);
        sessions.AddRange(sessionsWon);

        await _evaluator.CanAwardBadge(playerId, badge, session, sessions);
    }
}