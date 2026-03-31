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

    #region Green Level Tests (5 different opponents)

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnFalse_WhenOpponentCountIsLessThan5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithOpponents(4);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenOpponentCountIsExactly5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithOpponents(5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenOpponentCountIsMoreThan5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithOpponents(10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Blue Level Tests (10 different opponents)

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnFalse_WhenOpponentCountIsLessThan10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithOpponents(9);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenOpponentCountIsExactly10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithOpponents(10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Red Level Tests (25 different opponents)

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnFalse_WhenOpponentCountIsLessThan25()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithOpponents(24);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenOpponentCountIsExactly25()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithOpponents(25);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Gold Level Tests (50 different opponents)

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnFalse_WhenOpponentCountIsLessThan50()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithOpponents(49);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenOpponentCountIsExactly50()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithOpponents(50);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

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
    [InlineData(BadgeLevel.Green, 5)]
    [InlineData(BadgeLevel.Blue, 10)]
    [InlineData(BadgeLevel.Red, 25)]
    [InlineData(BadgeLevel.Gold, 50)]
    public async Task CanAwardBadge_ShouldReturnTrue_AtExactThreshold(BadgeLevel level, int opponentCount)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessionsWithOpponents(opponentCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
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
