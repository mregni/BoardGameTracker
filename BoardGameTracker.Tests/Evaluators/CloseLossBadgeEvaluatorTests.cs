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
        _evaluator.BadgeType.Should().Be(BadgeType.CloseLoss);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSoloSession()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenOpponentScoreIsNull()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 98, false, false);
        session.AddPlayerSession(2, null, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenOwnScoreIsNull()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession();
        session.AddPlayerSession(PlayerId, null, false, false);
        session.AddPlayerSession(2, 100, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyNoOtherCalls();
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
        VerifyNoOtherCalls();
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
        VerifyGameLookup();
        VerifyNoOtherCalls();
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
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(99, 100, true)]
    [InlineData(98, 100, true)]
    [InlineData(97, 100, false)]
    [InlineData(98.5, 100, true)]
    [InlineData(11, 10, true)]
    [InlineData(12, 10, true)]
    [InlineData(13, 10, false)]
    [InlineData(100, 100, false)]
    public async Task CanAwardBadge_ShouldEvaluateScoreDifferenceToWinner(double playerScore, double winnerScore, bool expected)
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, playerScore, false, false);
        session.AddPlayerSession(2, winnerScore, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().Be(expected);
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenCloseToFirstPlace()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 99, false, false);
        session.AddPlayerSession(2, 100, false, true);
        session.AddPlayerSession(3, 50, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenCloseToLastPlace()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 52, false, false);
        session.AddPlayerSession(2, 100, false, true);
        session.AddPlayerSession(3, 50, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNotCloseToAnyOtherPlayer()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 75, false, false);
        session.AddPlayerSession(2, 100, false, true);
        session.AddPlayerSession(3, 50, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(BadgeLevel.Green)]
    [InlineData(BadgeLevel.Blue)]
    [InlineData(BadgeLevel.Red)]
    [InlineData(BadgeLevel.Gold)]
    public async Task CanAwardBadge_ShouldIgnoreBadgeLevel(BadgeLevel level)
    {
        var badge = CreateBadge(level);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 99, false, false);
        session.AddPlayerSession(2, 100, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "close_loss_title", "close_loss_desc", BadgeType.CloseLoss, "badge.png", level);
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

    private void VerifyGameLookup()
    {
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(GameId), Times.Once);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
    }
}
