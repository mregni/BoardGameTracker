using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Badges.BadgeEvaluators;

public class SoloSpecialistBadgeEvaluatorTests
{
    private readonly SoloSpecialistBadgeEvaluator _evaluator;
    private const int PlayerId = 1;

    public SoloSpecialistBadgeEvaluatorTests()
    {
        _evaluator = new SoloSpecialistBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeSoloSpecialist()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.SoloSpecialist);
    }

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSessionListIsEmpty()
    {
        var badge = TestBadges.Create(BadgeType.SoloSpecialist, BadgeLevel.Green);
        var currentSession = new SessionBuilder().ForGame(1).Build();
        var sessions = new List<Session>();

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.SoloSpecialist, "image", null);
        var sessions = CreateSoloSessions(50);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyCountSoloSessions()
    {
        var badge = TestBadges.Create(BadgeType.SoloSpecialist, BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 3 solo sessions
        for (var i = 0; i < 3; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .Build();
            sessions.Add(session);
        }

        // Create 5 multi-player sessions
        for (var i = 3; i < 8; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .WithPlayer(2)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Only 3 solo sessions, needs 5
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNoSoloSessions()
    {
        var badge = TestBadges.Create(BadgeType.SoloSpecialist, BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 10 multi-player sessions
        for (var i = 0; i < 10; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .WithPlayer(2)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 4, false)]
    [InlineData(BadgeLevel.Green, 5, true)]
    [InlineData(BadgeLevel.Green, 10, true)]
    [InlineData(BadgeLevel.Blue, 9, false)]
    [InlineData(BadgeLevel.Blue, 10, true)]
    [InlineData(BadgeLevel.Blue, 15, true)]
    [InlineData(BadgeLevel.Red, 24, false)]
    [InlineData(BadgeLevel.Red, 25, true)]
    [InlineData(BadgeLevel.Red, 35, true)]
    [InlineData(BadgeLevel.Gold, 49, false)]
    [InlineData(BadgeLevel.Gold, 50, true)]
    [InlineData(BadgeLevel.Gold, 60, true)]
    public async Task CanAwardBadge_ShouldEvaluateSoloSessionCountPerLevel(BadgeLevel level, int soloCount, bool expected)
    {
        var badge = TestBadges.Create(BadgeType.SoloSpecialist, level);
        var sessions = CreateSoloSessions(soloCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    #endregion

    #region Helper Methods

    private static List<Session> CreateSoloSessions(int count)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < count; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .Build();
            sessions.Add(session);
        }
        return sessions;
    }

    #endregion
}
