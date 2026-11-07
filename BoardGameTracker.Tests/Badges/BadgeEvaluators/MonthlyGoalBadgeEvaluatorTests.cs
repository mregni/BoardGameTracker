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

public class MonthlyGoalBadgeEvaluatorTests
{
    private const int PlayerId = 1;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly MonthlyGoalBadgeEvaluator _evaluator;

    public MonthlyGoalBadgeEvaluatorTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _evaluator = new MonthlyGoalBadgeEvaluator(_dateTimeProviderMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _dateTimeProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void BadgeType_ShouldBeMonthlyGoal()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.MonthlyGoal);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldAward_WhenTheSessionMonthHasEnoughSessions()
    {
        var sessions = CreateSessions(new DateTime(2023, 3, 1, 18, 0, 0, DateTimeKind.Utc), BadgeEvaluatorConstants.MonthlyGoalSessionsRequired);

        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, TestBadges.Create(BadgeType.MonthlyGoal), sessions.Last(), sessions);

        result.Should().BeTrue();
        VerifyLocalTimeLookups(sessions.Last(), sessions);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldNotAward_WhenTheSessionMonthHasTooFewSessions()
    {
        var sessions = CreateSessions(new DateTime(2023, 3, 1, 18, 0, 0, DateTimeKind.Utc), BadgeEvaluatorConstants.MonthlyGoalSessionsRequired - 1);

        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, TestBadges.Create(BadgeType.MonthlyGoal), sessions.Last(), sessions);

        result.Should().BeFalse();
        VerifyLocalTimeLookups(sessions.Last(), sessions);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyCountSessionsInTheSameMonthAsTheEvaluatedSession()
    {
        var march = CreateSessions(new DateTime(2023, 3, 1, 18, 0, 0, DateTimeKind.Utc), BadgeEvaluatorConstants.MonthlyGoalSessionsRequired - 1);
        var april = CreateSessions(new DateTime(2023, 4, 1, 18, 0, 0, DateTimeKind.Utc), 1);
        var all = march.Concat(april).ToList();

        SetupLocalTime(all);

        var result = await _evaluator.CanAwardBadge(PlayerId, TestBadges.Create(BadgeType.MonthlyGoal), april[0], all);

        result.Should().BeFalse();
        VerifyLocalTimeLookups(april[0], all);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldUseTheLocalMonth()
    {
        var lastDayOfMarchUtc = new DateTime(2023, 3, 31, 23, 0, 0, DateTimeKind.Utc);
        var sessions = CreateSessions(new DateTime(2023, 4, 1, 18, 0, 0, DateTimeKind.Utc), BadgeEvaluatorConstants.MonthlyGoalSessionsRequired - 1);
        var lateSession = new SessionBuilder().Between(lastDayOfMarchUtc, lastDayOfMarchUtc.AddHours(1)).WithPlayer(PlayerId).Build();
        sessions.Add(lateSession);

        SetupLocalTime(sessions, offsetHours: 2);

        var result = await _evaluator.CanAwardBadge(PlayerId, TestBadges.Create(BadgeType.MonthlyGoal), lateSession, sessions);

        result.Should().BeTrue();
        VerifyLocalTimeLookups(lateSession, sessions);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldAward_WhenTheSessionMonthHasMoreSessionsThanRequired()
    {
        var sessions = CreateSessions(new DateTime(2023, 3, 1, 18, 0, 0, DateTimeKind.Utc), BadgeEvaluatorConstants.MonthlyGoalSessionsRequired + 1);
        SetupLocalTime(sessions);

        var result = await _evaluator.CanAwardBadge(PlayerId, TestBadges.Create(BadgeType.MonthlyGoal), sessions.Last(), sessions);

        result.Should().BeTrue();
        VerifyLocalTimeLookups(sessions.Last(), sessions);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldNotCountTheSameMonthOfAnotherYear()
    {
        var thisYear = CreateSessions(new DateTime(2023, 3, 1, 18, 0, 0, DateTimeKind.Utc), BadgeEvaluatorConstants.MonthlyGoalSessionsRequired - 1);
        var lastYear = CreateSessions(new DateTime(2022, 3, 1, 18, 0, 0, DateTimeKind.Utc), 1);
        var all = thisYear.Concat(lastYear).ToList();
        SetupLocalTime(all);

        var result = await _evaluator.CanAwardBadge(PlayerId, TestBadges.Create(BadgeType.MonthlyGoal), thisYear[0], all);

        result.Should().BeFalse();
        VerifyLocalTimeLookups(thisYear[0], all);
        VerifyNoOtherCalls();
    }

    private void SetupLocalTime(IEnumerable<Session> sessions, int offsetHours = 0)
    {
        foreach (var start in sessions.Select(x => x.Start).Distinct())
        {
            _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(start)).Returns(start.AddHours(offsetHours));
        }
    }

    private void VerifyLocalTimeLookups(Session evaluated, IEnumerable<Session> history)
    {
        foreach (var lookups in history.Select(x => x.Start).Append(evaluated.Start).GroupBy(x => x))
        {
            _dateTimeProviderMock.Verify(x => x.ConvertToLocalTime(lookups.Key), Times.Exactly(lookups.Count()));
        }
    }

    private static List<Session> CreateSessions(DateTime firstStart, int count)
    {
        return Enumerable.Range(0, count)
            .Select(offset => firstStart.AddHours(offset))
            .Select(start => new SessionBuilder().Between(start, start.AddMinutes(30)).WithPlayer(PlayerId).Build())
            .ToList();
    }
}
