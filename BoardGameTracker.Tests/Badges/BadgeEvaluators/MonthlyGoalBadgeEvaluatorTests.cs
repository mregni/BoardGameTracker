using System;
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
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(It.IsAny<DateTime>())).Returns((DateTime utc) => utc);
        _evaluator = new MonthlyGoalBadgeEvaluator(_dateTimeProviderMock.Object);
    }

    [Fact]
    public void BadgeType_ShouldBeMonthlyGoal()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.MonthlyGoal);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldAward_WhenTheSessionMonthHasEnoughSessions()
    {
        var sessions = CreateSessions(new DateTime(2023, 3, 1, 18, 0, 0, DateTimeKind.Utc), BadgeEvaluatorConstants.MonthlyGoalSessionsRequired);

        var result = await _evaluator.CanAwardBadge(PlayerId, TestBadges.Create(BadgeType.MonthlyGoal), sessions.Last(), sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldNotAward_WhenTheSessionMonthHasTooFewSessions()
    {
        var sessions = CreateSessions(new DateTime(2023, 3, 1, 18, 0, 0, DateTimeKind.Utc), BadgeEvaluatorConstants.MonthlyGoalSessionsRequired - 1);

        var result = await _evaluator.CanAwardBadge(PlayerId, TestBadges.Create(BadgeType.MonthlyGoal), sessions.Last(), sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyCountSessionsInTheSameMonthAsTheEvaluatedSession()
    {
        var march = CreateSessions(new DateTime(2023, 3, 1, 18, 0, 0, DateTimeKind.Utc), BadgeEvaluatorConstants.MonthlyGoalSessionsRequired - 1);
        var april = CreateSessions(new DateTime(2023, 4, 1, 18, 0, 0, DateTimeKind.Utc), 1);
        var all = march.Concat(april).ToList();

        var result = await _evaluator.CanAwardBadge(PlayerId, TestBadges.Create(BadgeType.MonthlyGoal), april[0], all);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldUseTheLocalMonth()
    {
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(It.IsAny<DateTime>())).Returns((DateTime utc) => utc.AddHours(2));
        var lastDayOfMarchUtc = new DateTime(2023, 3, 31, 23, 0, 0, DateTimeKind.Utc);
        var sessions = CreateSessions(new DateTime(2023, 4, 1, 18, 0, 0, DateTimeKind.Utc), BadgeEvaluatorConstants.MonthlyGoalSessionsRequired - 1);
        var lateSession = new SessionBuilder().Between(lastDayOfMarchUtc, lastDayOfMarchUtc.AddHours(1)).WithPlayer(PlayerId).Build();
        sessions.Add(lateSession);

        var result = await _evaluator.CanAwardBadge(PlayerId, TestBadges.Create(BadgeType.MonthlyGoal), lateSession, sessions);

        result.Should().BeTrue();
    }

    private static List<Session> CreateSessions(DateTime firstStart, int count)
    {
        return Enumerable.Range(0, count)
            .Select(offset => firstStart.AddHours(offset))
            .Select(start => new SessionBuilder().Between(start, start.AddMinutes(30)).WithPlayer(PlayerId).Build())
            .ToList();
    }
}
