using System.Collections.Generic;
using System.Linq;
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
        _evaluator = new ConsistentScheduleBadgeEvaluator(_dateTimeProviderMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _dateTimeProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldUseTheConfiguredTimeZone_WhenUtcStartFallsOnAnotherDay()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturday = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc);
        var sessions = CreateSessionsForConsecutiveSaturdays(saturday, 10);

        SetupLocalTime(sessions, offsetHours: 12);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
        VerifyLocalTimeLookups(sessions[0]);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void BadgeType_ShouldBeConsistentSchedule()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.ConsistentSchedule);
        VerifyNoOtherCalls();
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

        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
        VerifyLocalTimeLookups(sessions[0], sessions);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenMissingSaturdayInMiddle()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        for (var i = 0; i < 5; i++)
        {
            sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-7 * i).Date.AddHours(14), saturdayDate.AddDays(-7 * i).Date.AddHours(16)).Build());
        }
        for (var i = 6; i < 11; i++)
        {
            sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-7 * i).Date.AddHours(14), saturdayDate.AddDays(-7 * i).Date.AddHours(16)).Build());
        }

        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
        VerifyLocalTimeLookups(sessions[0], sessions);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenOnlyTheTenthSaturdayIsMissing()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);
        var sessions = CreateSessionsForConsecutiveSaturdays(saturdayDate, 9);
        sessions.AddRange(CreateSessionsForConsecutiveSaturdays(saturdayDate.AddDays(-70), 1));
        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
        VerifyLocalTimeLookups(sessions[0], sessions);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountBackwards_FromTheEvaluatedSession()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);
        var sessions = CreateSessionsForConsecutiveSaturdays(saturdayDate, 10);
        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[^1], sessions);

        result.Should().BeFalse();
        VerifyLocalTimeLookups(sessions[^1], sessions);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldAward_WhenTheLocalDateIsSaturdayButTheUtcDateIsFriday()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var friday = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);
        var sessions = CreateSessionsForConsecutiveSaturdays(friday, 10);
        SetupLocalTime(sessions, offsetHours: 12);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
        VerifyLocalTimeLookups(sessions[0], sessions);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldConvertStartsAsUtc_WhenTheStoredKindIsUnspecified()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturday = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Unspecified);
        var sessions = CreateSessionsForConsecutiveSaturdays(saturday, 10);
        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
        VerifyLocalTimeLookups(sessions[0], sessions);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenOnlySaturdaySessionsButNotConsecutive()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        for (var i = 0; i < 10; i++)
        {
            sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-14 * i).Date.AddHours(14), saturdayDate.AddDays(-14 * i).Date.AddHours(16)).Build()); // Every 2 weeks
        }

        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
        VerifyLocalTimeLookups(sessions[0], sessions);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldWorkWithMultipleSessionsOnSameSaturday()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        for (var i = 0; i < 10; i++)
        {
            var saturday = saturdayDate.AddDays(-7 * i);
            sessions.Add(new SessionBuilder().Between(saturday.Date.AddHours(14), saturday.Date.AddHours(16)).Build());
            sessions.Add(new SessionBuilder().Between(saturday.Date.AddHours(18), saturday.Date.AddHours(20)).Build()); // Second session same day
        }

        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
        VerifyLocalTimeLookups(sessions[0], sessions);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldIgnoreNonSaturdaySessions()
    {
        var badge = TestBadges.Create(BadgeType.ConsistentSchedule, BadgeLevel.Green);
        var saturdayDate = GetNextDayOfWeek(DateTime.UtcNow, DayOfWeek.Saturday);

        var sessions = new List<Session>();

        for (var i = 0; i < 10; i++)
        {
            sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-7 * i).Date.AddHours(14), saturdayDate.AddDays(-7 * i).Date.AddHours(16)).Build());
        }

        sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-1).Date.AddHours(14), saturdayDate.AddDays(-1).Date.AddHours(16)).Build()); // Friday
        sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(-2).Date.AddHours(14), saturdayDate.AddDays(-2).Date.AddHours(16)).Build()); // Thursday
        sessions.Add(new SessionBuilder().Between(saturdayDate.AddDays(1).Date.AddHours(14), saturdayDate.AddDays(1).Date.AddHours(16)).Build());  // Sunday

        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
        VerifyLocalTimeLookups(sessions[0], sessions);
        VerifyNoOtherCalls();
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

        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
        VerifyLocalTimeLookups(sessions[0], sessions);
        VerifyNoOtherCalls();
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

        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyLocalTimeLookups(session);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Helper Methods

    private void SetupLocalTime(IEnumerable<Session> sessions, int offsetHours = 0)
    {
        foreach (var start in sessions.Select(x => x.Start).Distinct())
        {
            _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(start)).Returns(start.AddHours(offsetHours));
        }
    }

    private void VerifyLocalTimeLookups(Session evaluated, IEnumerable<Session>? history = null)
    {
        var starts = (history ?? []).Select(x => x.Start).Append(evaluated.Start);
        foreach (var lookups in starts.GroupBy(x => x))
        {
            _dateTimeProviderMock.Verify(
                x => x.ConvertToLocalTime(It.Is<DateTime>(utc => utc == lookups.Key && utc.Kind == DateTimeKind.Utc)),
                Times.Exactly(lookups.Count()));
        }
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
            sessions.Add(new SessionBuilder().Between(saturday.Date.AddHours(14), saturday.Date.AddHours(16)).Build());
        }
        return sessions;
    }

    #endregion
}
