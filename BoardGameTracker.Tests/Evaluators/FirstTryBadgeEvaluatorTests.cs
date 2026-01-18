using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class FirstTryBadgeEvaluatorTests
{
    private readonly FirstTryBadgeEvaluator _evaluator;
    private const int PlayerId = 1;

    public FirstTryBadgeEvaluatorTests()
    {
        _evaluator = new FirstTryBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeFirstTry()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.FirstTry);
    }

    #region First Play Win Tests

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenFirstPlayAndWon()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession(gameId: 1);
        session.AddPlayerSession(PlayerId, null, false, won: true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenFirstPlayButLost()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession(gameId: 1);
        session.AddPlayerSession(PlayerId, null, false, won: false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNotFirstPlayButWon()
    {
        var badge = CreateBadge(BadgeLevel.Green);

        // First session of the game (played earlier)
        var firstSession = CreateSession(gameId: 1, dayOffset: 1);
        firstSession.AddPlayerSession(PlayerId, null, false, won: false);

        // Second session of the same game (current) - won this time
        var currentSession = CreateSession(gameId: 1, dayOffset: 0);
        currentSession.AddPlayerSession(PlayerId, null, false, won: true);

        var sessions = new List<Session> { currentSession, firstSession };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse(); // Not first try
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNotFirstPlayAndLost()
    {
        var badge = CreateBadge(BadgeLevel.Green);

        // First session of the game
        var firstSession = CreateSession(gameId: 1, dayOffset: 1);
        firstSession.AddPlayerSession(PlayerId, null, false, won: true);

        // Second session - lost
        var currentSession = CreateSession(gameId: 1, dayOffset: 0);
        currentSession.AddPlayerSession(PlayerId, null, false, won: false);

        var sessions = new List<Session> { currentSession, firstSession };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Different Games Tests

    [Fact]
    public async Task CanAwardBadge_ShouldCheckGameIdCorrectly()
    {
        var badge = CreateBadge(BadgeLevel.Green);

        // Sessions for different games
        var otherGameSession1 = CreateSession(gameId: 2, dayOffset: 2);
        otherGameSession1.AddPlayerSession(PlayerId, null, false, won: false);

        var otherGameSession2 = CreateSession(gameId: 3, dayOffset: 1);
        otherGameSession2.AddPlayerSession(PlayerId, null, false, won: false);

        // First and only session for game 1 - won
        var currentSession = CreateSession(gameId: 1, dayOffset: 0);
        currentSession.AddPlayerSession(PlayerId, null, false, won: true);

        var sessions = new List<Session> { currentSession, otherGameSession1, otherGameSession2 };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeTrue(); // First try for this specific game
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenMultipleGamesButFirstForCurrentGame()
    {
        var badge = CreateBadge(BadgeLevel.Green);

        // Player has played 10 sessions of other games
        var sessions = new List<Session>();
        for (var i = 0; i < 10; i++)
        {
            var otherSession = CreateSession(gameId: i + 10, dayOffset: i + 1);
            otherSession.AddPlayerSession(PlayerId, null, false, won: false);
            sessions.Add(otherSession);
        }

        // First session for game 1 - won
        var currentSession = CreateSession(gameId: 1, dayOffset: 0);
        currentSession.AddPlayerSession(PlayerId, null, false, won: true);
        sessions.Insert(0, currentSession);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldWorkWithAnyBadgeLevel()
    {
        // FirstTry doesn't use levels (single achievement)
        var session = CreateSession(gameId: 1);
        session.AddPlayerSession(PlayerId, null, false, won: true);
        var sessions = new List<Session> { session };

        foreach (BadgeLevel level in Enum.GetValues(typeof(BadgeLevel)))
        {
            var badge = CreateBadge(level);
            var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);
            result.Should().BeTrue();
        }
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.FirstTry, "image", null);
        var session = CreateSession(gameId: 1);
        session.AddPlayerSession(PlayerId, null, false, won: true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldHandleMultiplePlayersInSession()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession(gameId: 1);
        session.AddPlayerSession(PlayerId, null, false, won: true); // Player 1 won
        session.AddPlayerSession(2, null, false, won: false); // Player 2 lost
        session.AddPlayerSession(3, null, false, won: false); // Player 3 lost
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "first_try_title", "first_try_desc", BadgeType.FirstTry, "badge.png", level);
    }

    private static Session CreateSession(int gameId, int dayOffset = 0)
    {
        var start = DateTime.UtcNow.AddDays(-dayOffset).AddHours(-2);
        var end = DateTime.UtcNow.AddDays(-dayOffset);
        return new Session(gameId, start, end, $"Session for game {gameId}");
    }

    #endregion
}
