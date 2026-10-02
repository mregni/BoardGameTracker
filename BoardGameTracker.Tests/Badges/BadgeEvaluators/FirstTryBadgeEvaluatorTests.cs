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
        var badge = TestBadges.Create(BadgeType.FirstTry, BadgeLevel.Green);
        var currentSession = new SessionBuilder().ForGame(1)
            .WithPlayer(PlayerId, won: won)
            .Build();
        var sessions = new List<Session> { currentSession };

        if (!firstPlay)
        {
            var earlierSession = new SessionBuilder().ForGame(1).DaysAgo(1)
                .WithPlayer(PlayerId)
                .Build();
            sessions.Add(earlierSession);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenHistoryContainsNoSessionForCurrentGame()
    {
        var badge = TestBadges.Create(BadgeType.FirstTry, BadgeLevel.Green);
        var currentSession = new SessionBuilder().ForGame(1)
            .WithWinner(PlayerId)
            .Build();

        var otherGameSession = new SessionBuilder().ForGame(2).DaysAgo(1)
            .WithPlayer(PlayerId)
            .Build();
        var sessions = new List<Session> { otherGameSession };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCheckGameIdCorrectly()
    {
        var badge = TestBadges.Create(BadgeType.FirstTry, BadgeLevel.Green);

        var otherGameSession1 = new SessionBuilder().ForGame(2).DaysAgo(2)
            .WithPlayer(PlayerId)
            .Build();

        var otherGameSession2 = new SessionBuilder().ForGame(3).DaysAgo(1)
            .WithPlayer(PlayerId)
            .Build();

        var currentSession = new SessionBuilder().ForGame(1)
            .WithWinner(PlayerId)
            .Build();

        var sessions = new List<Session> { currentSession, otherGameSession1, otherGameSession2 };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenMultipleGamesButFirstForCurrentGame()
    {
        var badge = TestBadges.Create(BadgeType.FirstTry, BadgeLevel.Green);

        var sessions = new List<Session>();
        for (var i = 0; i < 10; i++)
        {
            var otherSession = new SessionBuilder().ForGame(i + 10).DaysAgo(i + 1)
                .WithPlayer(PlayerId)
                .Build();
            sessions.Add(otherSession);
        }

        var currentSession = new SessionBuilder().ForGame(1)
            .WithWinner(PlayerId)
            .Build();
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
        var badge = TestBadges.Create(BadgeType.FirstTry, level);
        var session = new SessionBuilder().ForGame(1)
            .WithWinner(PlayerId)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldHandleMultiplePlayersInSession()
    {
        var badge = TestBadges.Create(BadgeType.FirstTry, BadgeLevel.Green);
        var session = new SessionBuilder().ForGame(1)
            .WithWinner(PlayerId)
            .WithPlayer(2)
            .WithPlayer(3)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

}
