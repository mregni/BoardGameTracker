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
    private const int PlayerId = 1;

    public ConsistentScheduleBadgeEvaluatorTests()
    {
        _evaluator = new ConsistentScheduleBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeConsistentSchedule()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.ConsistentSchedule);
    }

    #region Consecutive Saturdays Tests

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(15, true)]
    public async Task CanAwardBadge_ShouldRequire10ConsecutiveSaturdays(int weekCount, bool expected)
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);
        var sessions = CreateSessionsForConsecutiveSaturdays(saturdayDate, weekCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenMissingSaturdayInMiddle()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        // Create sessions for 5 Saturdays, skip one, then 5 more
        for (var i = 0; i < 5; i++)
        {
            sessions.Add(CreateSessionOnDate(saturdayDate.AddDays(-7 * i)));
        }
        // Skip the 6th Saturday
        for (var i = 6; i < 11; i++)
        {
            sessions.Add(CreateSessionOnDate(saturdayDate.AddDays(-7 * i)));
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenOnlySaturdaySessionsButNotConsecutive()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        // Create 10 Saturday sessions but every other week
        for (var i = 0; i < 10; i++)
        {
            sessions.Add(CreateSessionOnDate(saturdayDate.AddDays(-14 * i))); // Every 2 weeks
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldWorkWithMultipleSessionsOnSameSaturday()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        // Create 2 sessions per Saturday for 10 weeks
        for (var i = 0; i < 10; i++)
        {
            var saturday = saturdayDate.AddDays(-7 * i);
            sessions.Add(CreateSessionOnDate(saturday));
            sessions.Add(CreateSessionOnDate(saturday.AddHours(4))); // Second session same day
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldIgnoreNonSaturdaySessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        // Create 10 consecutive Saturday sessions
        for (var i = 0; i < 10; i++)
        {
            sessions.Add(CreateSessionOnDate(saturdayDate.AddDays(-7 * i)));
        }

        // Add non-Saturday sessions (should be ignored)
        sessions.Add(CreateSessionOnDate(saturdayDate.AddDays(-1))); // Friday
        sessions.Add(CreateSessionOnDate(saturdayDate.AddDays(-2))); // Thursday
        sessions.Add(CreateSessionOnDate(saturdayDate.AddDays(1)));  // Sunday

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(BadgeLevel.Green)]
    [InlineData(BadgeLevel.Blue)]
    [InlineData(BadgeLevel.Red)]
    [InlineData(BadgeLevel.Gold)]
    public async Task CanAwardBadge_ShouldIgnoreBadgeLevel(BadgeLevel? level)
    {
        var badge = CreateBadge(level);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);
        var sessions = CreateSessionsForConsecutiveSaturdays(saturdayDate, 10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Friday)]
    public async Task CanAwardBadge_ShouldReturnFalse_ForNonSaturdayDays(DayOfWeek dayOfWeek)
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var date = GetNextDayOfWeek(DateTime.UtcNow, dayOfWeek);
        var session = CreateSessionOnDate(date);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "consistent_schedule_title", "consistent_schedule_desc", BadgeType.ConsistentSchedule, "badge.png", level);
    }

    private static Session CreateSessionOnDate(DateTime date)
    {
        var start = date.Date.AddHours(14); // 2 PM
        var end = date.Date.AddHours(16);   // 4 PM
        return new Session(1, start, end, $"Session on {date:yyyy-MM-dd}");
    }

    private static DateTime GetNextDayOfWeek(DateTime from, DayOfWeek dayOfWeek)
    {
        var daysUntil = ((int)dayOfWeek - (int)from.DayOfWeek + 7) % 7;
        if (daysUntil == 0 && from.DayOfWeek != dayOfWeek)
        {
            daysUntil = 7;
        }
        return from.AddDays(daysUntil);
    }

    private static List<Session> CreateSessionsForConsecutiveSaturdays(DateTime startingSaturday, int weekCount)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < weekCount; i++)
        {
            var saturday = startingSaturday.AddDays(-7 * i);
            sessions.Add(CreateSessionOnDate(saturday));
        }
        return sessions;
    }

    #endregion
}
