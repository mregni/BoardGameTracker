using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class ConsistentScheduleBadgeEvaluatorTests
{
    private readonly ConsistentScheduleBadgeEvaluator _evaluator;

    public ConsistentScheduleBadgeEvaluatorTests()
    {
        _evaluator = new ConsistentScheduleBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldReturnConsistentSchedule()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.ConsistentSchedule);
    }

    [Fact]
    public async Task CanAwardBadge_WhenCurrentSessionNotOnSaturday_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            Start = new DateTime(2025, 1, 19)
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, []);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_WhenSessionsWithDifferentTimes_ShouldCompareByDateOnly()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var sessionStart = new DateTime(2025, 1, 18, 10, 0, 0);
        var session = new Session { Start = sessionStart };
        
        var sessions = new List<Session>();
        for (var i = 0; i < 10; i++)
        {
            sessions.Add(new Session
            {
                Start = sessionStart.AddDays(-7 * i).AddHours(i)
            });
        }


        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_WhenExtraSessionsPresent_ShouldOnlyCheckRequiredSaturdays()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var sessionStart = new DateTime(2025, 1, 18);
        var session = new Session { Start = sessionStart };
        
        var sessions = new List<Session>();
        
        sessions.AddRange(CreateSessionsForSaturdays(sessionStart, 10));
        
        sessions.Add(new Session { Start = new DateTime(2025, 1, 19) });
        sessions.Add(new Session { Start = new DateTime(2025, 1, 20) });
        sessions.Add(new Session { Start = new DateTime(2025, 1, 25) });

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(5, false)]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(15, true)]
    public async Task CanAwardBadge_WhenVariousNumberOfSaturdays_ShouldReturnExpectedResult(int saturdayCount, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var sessionStart = new DateTime(2025, 1, 18);
        var session = new Session { Start = sessionStart };
        
        var sessions = CreateSessionsForSaturdays(sessionStart, saturdayCount);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task CanAwardBadge_WhenCurrentSessionStartTimeUsed_ShouldCalculateCorrectSaturdays()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var sessionStart = new DateTime(2025, 1, 25);
        var session = new Session { Start = sessionStart };
        
        var sessions = CreateSessionsForSaturdays(sessionStart, 10);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    private static List<Session> CreateSessionsForSaturdays(DateTime startSaturday, int count)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < count; i++)
        {
            sessions.Add(new Session
            {
                Start = startSaturday.AddDays(-7 * i)
            });
        }
        return sessions;
    }
}