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

    #region Green Level Tests (3 different games)

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnFalse_WhenGameCountIsLessThan3()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithDifferentGames(2);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenGameCountIsExactly3()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithDifferentGames(3);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenGameCountIsMoreThan3()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithDifferentGames(5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Blue Level Tests (10 different games)

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnFalse_WhenGameCountIsLessThan10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithDifferentGames(9);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenGameCountIsExactly10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithDifferentGames(10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenGameCountIsMoreThan10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithDifferentGames(15);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Red Level Tests (20 different games)

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnFalse_WhenGameCountIsLessThan20()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithDifferentGames(19);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenGameCountIsExactly20()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithDifferentGames(20);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenGameCountIsMoreThan20()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithDifferentGames(30);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Gold Level Tests (50 different games)

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnFalse_WhenGameCountIsLessThan50()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithDifferentGames(49);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenGameCountIsExactly50()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithDifferentGames(50);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenGameCountIsMoreThan50()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithDifferentGames(60);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

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
    public async Task CanAwardBadge_ShouldReturnTrue_WhenManySessionsOfFewGames()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        // Create 30 sessions with 3 different games (10 sessions per game)
        var sessions = new List<Session>();
        for (var i = 0; i < 30; i++)
        {
            var gameId = (i % 3) + 1;
            sessions.Add(CreateSession(gameId, i));
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue(); // 3 different games
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 3)]
    [InlineData(BadgeLevel.Blue, 10)]
    [InlineData(BadgeLevel.Red, 20)]
    [InlineData(BadgeLevel.Gold, 50)]
    public async Task CanAwardBadge_ShouldReturnTrue_AtExactThreshold(BadgeLevel level, int gameCount)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessionsWithDifferentGames(gameCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 2)]
    [InlineData(BadgeLevel.Blue, 9)]
    [InlineData(BadgeLevel.Red, 19)]
    [InlineData(BadgeLevel.Gold, 49)]
    public async Task CanAwardBadge_ShouldReturnFalse_JustBelowThreshold(BadgeLevel level, int gameCount)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessionsWithDifferentGames(gameCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
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
