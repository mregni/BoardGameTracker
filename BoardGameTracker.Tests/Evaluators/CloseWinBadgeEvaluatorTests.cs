using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class CloseWinBadgeEvaluatorTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly CloseWinBadgeEvaluator _evaluator;
    private const int PlayerId = 1;
    private const int GameId = 1;

    public CloseWinBadgeEvaluatorTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _evaluator = new CloseWinBadgeEvaluator(_gameRepositoryMock.Object);
    }

    [Fact]
    public void BadgeType_ShouldBeCloseWin()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.CloseWin);
    }

    #region Basic Requirements Tests

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSoloSession()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenAnyScoreIsNull()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, null, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenPlayerDidNotWin()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 98, false, false);
        session.AddPlayerSession(2, 100, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenGameDoesNotSupportScoring()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var game = new Game("Test Game", hasScoring: false);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(GameId)).ReturnsAsync(game);

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 98, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenGameNotFound()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(GameId)).ReturnsAsync((Game?)null);

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 98, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Close Win Tests (Within 2 Points)

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenWinByExactly1Point_HighestWins()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);  // Winner with highest
        session.AddPlayerSession(2, 99, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenWinByExactly2Points_HighestWins()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 98, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenWinByMoreThan2Points()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 97, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenWinByExactly1Point_LowestWins()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 10, false, true);  // Winner with lowest (golf-style)
        session.AddPlayerSession(2, 11, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenWinByExactly2Points_LowestWins()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 10, false, true);
        session.AddPlayerSession(2, 12, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Multiple Players Tests

    [Fact]
    public async Task CanAwardBadge_ShouldCompareWithSecondPlace_NotAllOpponents()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 99, false, false);  // Second place (close)
        session.AddPlayerSession(3, 50, false, false);  // Third place (far)
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue(); // Close to second place
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNotCloseToSecondPlace()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 90, false, false);  // Second place (not close)
        session.AddPlayerSession(3, 50, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldHandleDecimalScores()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100.5, false, true);
        session.AddPlayerSession(2, 99.0, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue(); // 1.5 points difference
    }

    [Fact]
    public async Task CanAwardBadge_ShouldWorkWithAnyBadgeLevel()
    {
        SetupGameWithScoring();
        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 99, false, false);
        var sessions = new List<Session> { session };

        foreach (BadgeLevel level in Enum.GetValues(typeof(BadgeLevel)))
        {
            var badge = CreateBadge(level);
            var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);
            result.Should().BeTrue();
        }
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "close_win_title", "close_win_desc", BadgeType.CloseWin, "badge.png", level);
    }

    private static Session CreateSession()
    {
        var start = DateTime.UtcNow.AddHours(-2);
        var end = DateTime.UtcNow;
        return new Session(GameId, start, end, "Test session");
    }

    private void SetupGameWithScoring()
    {
        var game = new Game("Test Game", hasScoring: true);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(GameId)).ReturnsAsync(game);
    }

    #endregion
}
