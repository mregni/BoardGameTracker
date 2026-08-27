using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

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
        var badge = CreateBadge(BadgeLevel.Green);
        var currentSession = CreateSession(1, 0);
        var sessions = new List<Session>();

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldNotCountSelf()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create sessions where player only plays with themselves (solo)
        for (var i = 0; i < 10; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // No opponents
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountDistinctOpponentsOnly()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 10 sessions but only with 3 different opponents
        for (var i = 0; i < 10; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            session.AddPlayerSession((i % 3) + 2, null, false, false); // Opponents 2, 3, 4
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Only 3 distinct opponents, needs 5
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountOpponentsAcrossMultipleSessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 5 sessions, each with a unique opponent
        for (var i = 0; i < 5; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            session.AddPlayerSession(i + 2, null, false, false); // Different opponent each session
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue(); // 5 distinct opponents
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountMultipleOpponentsPerSession()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 2 sessions with multiple opponents each
        var session1 = CreateSession(1, 0);
        session1.AddPlayerSession(PlayerId, null, false, true);
        session1.AddPlayerSession(2, null, false, false);
        session1.AddPlayerSession(3, null, false, false);
        session1.AddPlayerSession(4, null, false, false);
        sessions.Add(session1);

        var session2 = CreateSession(1, 1);
        session2.AddPlayerSession(PlayerId, null, false, true);
        session2.AddPlayerSession(5, null, false, false);
        session2.AddPlayerSession(6, null, false, false);
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
        var badge = CreateBadge(level);
        var sessions = CreateSessionsWithOpponents(opponentCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "social_player_title", "social_player_desc", BadgeType.SocialPlayer, "badge.png", level);
    }

    private static Session CreateSession(int gameId, int dayOffset)
    {
        var start = DateTime.UtcNow.AddDays(-dayOffset).AddHours(-2);
        var end = DateTime.UtcNow.AddDays(-dayOffset);
        return new Session(gameId, start, end, $"Session {dayOffset}");
    }

    private static List<Session> CreateSessionsWithOpponents(int opponentCount)
    {
        var sessions = new List<Session>();

        // Create sessions with one opponent each
        for (var i = 0; i < opponentCount; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            session.AddPlayerSession(i + 2, null, false, false); // Different opponent each session (starting at id 2)
            sessions.Add(session);
        }

        return sessions;
    }

    #endregion
}
