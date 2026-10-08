using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Badges.BadgeEvaluators;

public class ConsistentScheduleBadgeEvaluatorTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly ConsistentScheduleBadgeEvaluator _evaluator;
    private const int PlayerId = 1;

    public ConsistentScheduleBadgeEvaluatorTests()
    {
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(It.IsAny<DateTime>())).Returns((DateTime utc) => utc);
        _evaluator = new ConsistentScheduleBadgeEvaluator(_dateTimeProviderMock.Object);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldUseTheConfiguredTimeZone_WhenUtcStartFallsOnAnotherDay()
    {
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(It.IsAny<DateTime>())).Returns((DateTime utc) => utc.AddHours(12));
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturday = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc);
        var sessions = CreateSessionsForConsecutiveSaturdays(saturday, 10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
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
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);
        var sessions = CreateSessionsForConsecutiveSaturdays(saturdayDate, weekCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenMissingSaturdayInMiddle()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        // Create sessions for 5 Saturdays, skip one, then 5 more
        for (var i = 0; i < 5; i++)
        {
            sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-7 * i).Date.AddHours(14), saturdayDate.AddDays(-7 * i).Date.AddHours(16)).Build());
        }
        // Skip the 6th Saturday
        for (var i = 6; i < 11; i++)
        {
            sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-7 * i).Date.AddHours(14), saturdayDate.AddDays(-7 * i).Date.AddHours(16)).Build());
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenOnlySaturdaySessionsButNotConsecutive()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        // Create 10 Saturday sessions but every other week
        for (var i = 0; i < 10; i++)
        {
            sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-14 * i).Date.AddHours(14), saturdayDate.AddDays(-14 * i).Date.AddHours(16)).Build()); // Every 2 weeks
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldWorkWithMultipleSessionsOnSameSaturday()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        // Create 2 sessions per Saturday for 10 weeks
        for (var i = 0; i < 10; i++)
        {
            var saturday = saturdayDate.AddDays(-7 * i);
            sessions.Add(new SessionBuilder().Between(saturday.Date.AddHours(14), saturday.Date.AddHours(16)).Build());
            sessions.Add(new SessionBuilder().Between(saturday.AddHours(4).Date.AddHours(14), saturday.AddHours(4).Date.AddHours(16)).Build()); // Second session same day
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldIgnoreNonSaturdaySessions()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        // Create 10 consecutive Saturday sessions
        for (var i = 0; i < 10; i++)
        {
            sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-7 * i).Date.AddHours(14), saturdayDate.AddDays(-7 * i).Date.AddHours(16)).Build());
        }

        // Add non-Saturday sessions (should be ignored)
        sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-1).Date.AddHours(14), saturdayDate.AddDays(-1).Date.AddHours(16)).Build()); // Friday
        sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-2).Date.AddHours(14), saturdayDate.AddDays(-2).Date.AddHours(16)).Build()); // Thursday
        sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(1).Date.AddHours(14), saturdayDate.AddDays(1).Date.AddHours(16)).Build());  // Sunday

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
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, level);
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
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var date = GetNextDayOfWeek(DateTime.UtcNow, dayOfWeek);
        var session = new SessionBuilder().Between(date.Date.AddHours(14), date.Date.AddHours(16)).Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

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
            sessions.Add(new SessionBuilder().Between(saturday.Date.AddHours(14), saturday.Date.AddHours(16)).Build());
        }
        return sessions;
    }

    #endregion
}
