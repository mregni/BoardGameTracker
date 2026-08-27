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

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public async Task CanAwardBadge_ShouldRequireWinOnFirstPlay(bool firstPlay, bool won, bool expected)
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var currentSession = CreateSession(gameId: 1, dayOffset: 0);
        currentSession.AddPlayerSession(PlayerId, null, false, won);
        var sessions = new List<Session> { currentSession };

        if (!firstPlay)
        {
            var earlierSession = CreateSession(gameId: 1, dayOffset: 1);
            earlierSession.AddPlayerSession(PlayerId, null, false, false);
            sessions.Add(earlierSession);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenHistoryContainsNoSessionForCurrentGame()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var currentSession = CreateSession(gameId: 1);
        currentSession.AddPlayerSession(PlayerId, null, false, won: true);

        var otherGameSession = CreateSession(gameId: 2, dayOffset: 1);
        otherGameSession.AddPlayerSession(PlayerId, null, false, won: false);
        var sessions = new List<Session> { otherGameSession };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCheckGameIdCorrectly()
    {
        var badge = CreateBadge(BadgeLevel.Green);

        var otherGameSession1 = CreateSession(gameId: 2, dayOffset: 2);
        otherGameSession1.AddPlayerSession(PlayerId, null, false, won: false);

        var otherGameSession2 = CreateSession(gameId: 3, dayOffset: 1);
        otherGameSession2.AddPlayerSession(PlayerId, null, false, won: false);

        var currentSession = CreateSession(gameId: 1, dayOffset: 0);
        currentSession.AddPlayerSession(PlayerId, null, false, won: true);

        var sessions = new List<Session> { currentSession, otherGameSession1, otherGameSession2 };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenMultipleGamesButFirstForCurrentGame()
    {
        var badge = CreateBadge(BadgeLevel.Green);

        var sessions = new List<Session>();
        for (var i = 0; i < 10; i++)
        {
            var otherSession = CreateSession(gameId: i + 10, dayOffset: i + 1);
            otherSession.AddPlayerSession(PlayerId, null, false, won: false);
            sessions.Add(otherSession);
        }

        var currentSession = CreateSession(gameId: 1, dayOffset: 0);
        currentSession.AddPlayerSession(PlayerId, null, false, won: true);
        sessions.Insert(0, currentSession);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

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
        session.AddPlayerSession(PlayerId, null, false, won: true);
        session.AddPlayerSession(2, null, false, won: false);
        session.AddPlayerSession(3, null, false, won: false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

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
}
