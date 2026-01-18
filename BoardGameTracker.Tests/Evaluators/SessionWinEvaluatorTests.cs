using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class SessionWinEvaluatorTests
{
    private readonly SessionWinEvaluator _evaluator;
    private const int PlayerId = 1;

    public SessionWinEvaluatorTests()
    {
        _evaluator = new SessionWinEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeWins()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.Wins);
    }

    #region Green Level Tests (3 wins)

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnFalse_WhenWinCountIsLessThan3()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWins(2, 5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenWinCountIsExactly3()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWins(3, 5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenWinCountIsMoreThan3()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWins(5, 5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Blue Level Tests (10 wins)

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnFalse_WhenWinCountIsLessThan10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithWins(9, 15);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenWinCountIsExactly10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithWins(10, 15);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenWinCountIsMoreThan10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSessionsWithWins(15, 20);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Red Level Tests (25 wins)

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnFalse_WhenWinCountIsLessThan25()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithWins(24, 30);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenWinCountIsExactly25()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithWins(25, 30);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenWinCountIsMoreThan25()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSessionsWithWins(35, 40);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Gold Level Tests (50 wins)

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnFalse_WhenWinCountIsLessThan50()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithWins(49, 60);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenWinCountIsExactly50()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithWins(50, 60);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenWinCountIsMoreThan50()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSessionsWithWins(60, 70);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNoWins()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSessionsWithWins(0, 10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.Wins, "image", null);
        var sessions = CreateSessionsWithWins(50, 50);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 3)]
    [InlineData(BadgeLevel.Blue, 10)]
    [InlineData(BadgeLevel.Red, 25)]
    [InlineData(BadgeLevel.Gold, 50)]
    public async Task CanAwardBadge_ShouldReturnTrue_AtExactThreshold(BadgeLevel level, int winCount)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessionsWithWins(winCount, winCount + 5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 2)]
    [InlineData(BadgeLevel.Blue, 9)]
    [InlineData(BadgeLevel.Red, 24)]
    [InlineData(BadgeLevel.Gold, 49)]
    public async Task CanAwardBadge_ShouldReturnFalse_JustBelowThreshold(BadgeLevel level, int winCount)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSessionsWithWins(winCount, winCount + 5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyCountWinsForSpecifiedPlayer()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create sessions where player 1 has 2 wins but player 2 has many wins
        for (var i = 0; i < 5; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, i < 2); // Player 1 wins first 2
            session.AddPlayerSession(2, null, false, i >= 2); // Player 2 wins the rest
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Player 1 only has 2 wins, needs 3
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "wins_title", "wins_desc", BadgeType.Wins, "badge.png", level);
    }

    private static Session CreateSession(int gameId, int dayOffset)
    {
        var start = DateTime.UtcNow.AddDays(-dayOffset).AddHours(-2);
        var end = DateTime.UtcNow.AddDays(-dayOffset);
        return new Session(gameId, start, end, $"Session {dayOffset}");
    }

    private static List<Session> CreateSessionsWithWins(int winCount, int totalSessions)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < totalSessions; i++)
        {
            var session = CreateSession(1, i);
            var won = i < winCount;
            session.AddPlayerSession(PlayerId, null, false, won);
            sessions.Add(session);
        }
        return sessions;
    }

    #endregion
}
