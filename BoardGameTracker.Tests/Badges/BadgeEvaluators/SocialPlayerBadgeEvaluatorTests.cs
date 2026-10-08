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

public class SocialPlayerBadgeEvaluatorTests
{
    private readonly SocialPlayerBadgeEvaluator _evaluator;
    private const int PlayerId = 1;

    public SocialPlayerBadgeEvaluatorTests()
    {
        _evaluator = new SocialPlayerBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeSocialPlayer()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.SocialPlayer);
    }

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSessionListIsEmpty()
    {
        var badge = TestBadges.Create(BadgeType.SocialPlayer, BadgeLevel.Green);
        var currentSession = new SessionBuilder().ForGame(1).Build();
        var sessions = new List<Session>();

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldNotCountSelf()
    {
        var badge = TestBadges.Create(BadgeType.SocialPlayer, BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create sessions where player only plays with themselves (solo)
        for (var i = 0; i < 10; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // No opponents
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountDistinctOpponentsOnly()
    {
        var badge = TestBadges.Create(BadgeType.SocialPlayer, BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 10 sessions but only with 3 different opponents
        for (var i = 0; i < 10; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .WithPlayer((i % 3) + 2)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Only 3 distinct opponents, needs 5
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountOpponentsAcrossMultipleSessions()
    {
        var badge = TestBadges.Create(BadgeType.SocialPlayer, BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 5 sessions, each with a unique opponent
        for (var i = 0; i < 5; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .WithPlayer(i + 2)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue(); // 5 distinct opponents
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountMultipleOpponentsPerSession()
    {
        var badge = TestBadges.Create(BadgeType.SocialPlayer, BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 2 sessions with multiple opponents each
        var session1 = new SessionBuilder().ForGame(1)
            .WithWinner(PlayerId)
            .WithPlayer(2)
            .WithPlayer(3)
            .WithPlayer(4)
            .Build();
        sessions.Add(session1);

        var session2 = new SessionBuilder().ForGame(1).DaysAgo(1)
            .WithWinner(PlayerId)
            .WithPlayer(5)
            .WithPlayer(6)
            .Build();
        sessions.Add(session2);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue(); // 5 distinct opponents (2,3,4,5,6)
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.SocialPlayer, "image", null);
        var sessions = CreateSessionsWithOpponents(50);

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
    [InlineData(BadgeLevel.Red, 30, true)]
    [InlineData(BadgeLevel.Gold, 49, false)]
    [InlineData(BadgeLevel.Gold, 50, true)]
    [InlineData(BadgeLevel.Gold, 60, true)]
    public async Task CanAwardBadge_ShouldEvaluateOpponentCountPerLevel(BadgeLevel level, int opponentCount, bool expected)
    {
        var badge = TestBadges.Create(BadgeType.SocialPlayer, level);
        var sessions = CreateSessionsWithOpponents(opponentCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    #endregion

    #region Helper Methods

    private static List<Session> CreateSessionsWithOpponents(int opponentCount)
    {
        var sessions = new List<Session>();

        // Create sessions with one opponent each
        for (var i = 0; i < opponentCount; i++)
        {
            var session = new SessionBuilder().ForGame(1).DaysAgo(i)
                .WithWinner(PlayerId)
                .WithPlayer(i + 2)
                .Build();
            sessions.Add(session);
        }

        return sessions;
    }

    #endregion
}
