using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class DifferentGameBadgeEvaluatorTests
{
    private readonly DifferentGameBadgeEvaluator _evaluator;
    private const int PlayerId = 1;

    public DifferentGameBadgeEvaluatorTests()
    {
        _evaluator = new DifferentGameBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeDifferentGames()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.DifferentGames);
    }

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.DifferentGames, "image", null);
        var sessions = CreateSessionsWithDifferentGames(50);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCountDistinctGamesOnly()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        // Create 10 sessions but only 2 different games
        var sessions = new List<Session>();
        for (var i = 0; i < 10; i++)
        {
            var gameId = (i % 2) + 1; // Alternates between game 1 and 2
            sessions.Add(CreateSession(gameId, i));
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Only 2 different games, needs 3
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSessionListIsEmpty()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var currentSession = CreateSession(1, 0);
        var sessions = new List<Session>();

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, currentSession, sessions);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 2, false)]
    [InlineData(BadgeLevel.Green, 3, true)]
    [InlineData(BadgeLevel.Green, 5, true)]
    [InlineData(BadgeLevel.Blue, 9, false)]
    [InlineData(BadgeLevel.Blue, 10, true)]
    [InlineData(BadgeLevel.Blue, 15, true)]
    [InlineData(BadgeLevel.Red, 19, false)]
    [InlineData(BadgeLevel.Red, 20, true)]
    [InlineData(BadgeLevel.Red, 30, true)]
    [InlineData(BadgeLevel.Gold, 49, false)]
    [InlineData(BadgeLevel.Gold, 50, true)]
    [InlineData(BadgeLevel.Gold, 60, true)]
    public async Task CanAwardBadge_ShouldEvaluateGameCountPerLevel(BadgeLevel level, int gameCount, bool expected)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessionsWithDifferentGames(gameCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "different_games_title", "different_games_desc", BadgeType.DifferentGames, "badge.png", level);
    }

    private static Session CreateSession(int gameId, int dayOffset)
    {
        var start = DateTime.UtcNow.AddDays(-dayOffset).AddHours(-2);
        var end = DateTime.UtcNow.AddDays(-dayOffset);
        return new Session(gameId, start, end, $"Session {dayOffset}");
    }

    private static List<Session> CreateSessionsWithDifferentGames(int gameCount)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < gameCount; i++)
        {
            var gameId = i + 1; // Each session has a different game
            sessions.Add(CreateSession(gameId, i));
        }
        return sessions;
    }

    #endregion
}
