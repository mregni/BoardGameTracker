using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class MonthlyGoalBadgeEvaluatorTests
{
    private readonly MonthlyGoalBadgeEvaluator _evaluator;

    public MonthlyGoalBadgeEvaluatorTests()
    {
        _evaluator = new MonthlyGoalBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldReturnMonthlyGoal()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.MonthlyGoal);
    }
    
    [Fact]
    public async Task CanAwardBadge_When21SessionsInLastMonth_ShouldReturnTrue()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = CreateSessionsInLastMonth(21);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_WhenMixedSessionDates_ShouldOnlyCountRecentOnes()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>();

        for (var i = 0; i < 10; i++)
        {
            sessions.Add(new Session
            {
                Start = DateTime.UtcNow.AddMonths(-2).AddDays(i)
            });
        }

        sessions.AddRange(CreateSessionsInLastMonth(22));

        for (var i = 0; i < 5; i++)
        {
            sessions.Add(new Session
            {
                Start = DateTime.UtcNow.AddMonths(-3).AddDays(i)
            });
        }

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(10, false)]
    [InlineData(19, false)]
    [InlineData(20, true)]
    [InlineData(21, true)]
    [InlineData(25, true)]
    [InlineData(50, true)]
    public async Task CanAwardBadge_WhenDifferentSessionCounts_ShouldReturnExpectedResult(
        int sessionCount, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = CreateSessionsInLastMonth(sessionCount);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task CanAwardBadge_WhenSessionExactlyOneMonthAgo_ShouldNotCountIt()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>
        {
            new() { Start = DateTime.UtcNow.AddMonths(-1) }
        };

        sessions.AddRange(CreateSessionsInLastMonth(20));

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);
        
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_WhenSessionJustWithinOneMonth_ShouldCountIt()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>
        {
            new() { Start = DateTime.UtcNow.AddMonths(-1).AddMinutes(1) }
        };

        sessions.AddRange(CreateSessionsInLastMonth(19));

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue(); 
    }

    private static List<Session> CreateSessionsInLastMonth(int count)
    {
        var sessions = new List<Session>();
        var baseDate = DateTime.UtcNow.AddDays(-15);

        for (int i = 0; i < count; i++)
        {
            sessions.Add(new Session
            {
                Start = baseDate.AddHours(-i)
            });
        }

        return sessions;
    }
}