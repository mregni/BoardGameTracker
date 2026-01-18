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

public class CloseLossBadgeEvaluatorTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly CloseLossBadgeEvaluator _evaluator;
    private const int PlayerId = 1;
    private const int GameId = 1;

    public CloseLossBadgeEvaluatorTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _evaluator = new CloseLossBadgeEvaluator(_gameRepositoryMock.Object);
    }

    [Fact]
    public void BadgeType_ShouldBeCloseLoss()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.CLoseLoss);
    }

    #region Basic Requirements Tests

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSoloSession()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, false);
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
        session.AddPlayerSession(PlayerId, 98, false, false);
        session.AddPlayerSession(2, null, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenPlayerWon()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 98, false, false);
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
        session.AddPlayerSession(PlayerId, 98, false, false);
        session.AddPlayerSession(2, 100, false, true);
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
        session.AddPlayerSession(PlayerId, 98, false, false);
        session.AddPlayerSession(2, 100, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Close Loss to Highest Scorer Tests

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenLoseByExactly1Point_ToHighest()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 99, false, false);
        session.AddPlayerSession(2, 100, false, true);  // Winner
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenLoseByExactly2Points_ToHighest()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 98, false, false);
        session.AddPlayerSession(2, 100, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenLoseByMoreThan2Points_ToHighest()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 97, false, false);
        session.AddPlayerSession(2, 100, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Close Loss to Lowest Scorer Tests (Golf-style games)

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenLoseByExactly1Point_ToLowest()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 11, false, false);
        session.AddPlayerSession(2, 10, false, true);  // Winner with lowest
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenLoseByExactly2Points_ToLowest()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 12, false, false);
        session.AddPlayerSession(2, 10, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenLoseByMoreThan2Points_ToLowest()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 13, false, false);
        session.AddPlayerSession(2, 10, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Multiple Players Tests

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenCloseToFirstPlace()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 99, false, false);  // Close to winner
        session.AddPlayerSession(2, 100, false, true);         // Winner
        session.AddPlayerSession(3, 50, false, false);         // Far from everyone
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenCloseToLastPlace()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 52, false, false);  // Close to last
        session.AddPlayerSession(2, 100, false, true);         // Winner
        session.AddPlayerSession(3, 50, false, false);         // Last place
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNotCloseToAnyOtherPlayer()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 75, false, false);  // Middle, not close to anyone
        session.AddPlayerSession(2, 100, false, true);
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
        session.AddPlayerSession(PlayerId, 98.5, false, false);
        session.AddPlayerSession(2, 100.0, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue(); // 1.5 points difference
    }

    [Fact]
    public async Task CanAwardBadge_ShouldWorkWithAnyBadgeLevel()
    {
        SetupGameWithScoring();
        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 99, false, false);
        session.AddPlayerSession(2, 100, false, true);
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
        return Badge.CreateWithId(1, "close_loss_title", "close_loss_desc", BadgeType.CLoseLoss, "badge.png", level);
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
