using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Games.Specifications;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Badges.BadgeEvaluators;

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
        var badge = TestBadges.Create(BadgeType.CloseLoss, BadgeLevel.Green);
        var session = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId, 100)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenOpponentScoreIsNull()
    {
        var badge = TestBadges.Create(BadgeType.CloseLoss, BadgeLevel.Green);
        var session = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId, 98)
            .WithWinner(2)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenOwnScoreIsNull()
    {
        var badge = TestBadges.Create(BadgeType.CloseLoss, BadgeLevel.Green);
        var session = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId)
            .WithWinner(2, 100)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenPlayerWon()
    {
        var badge = TestBadges.Create(BadgeType.CloseLoss, BadgeLevel.Green);
        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, 100)
            .WithPlayer(2, 98)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenGameDoesNotSupportScoring()
    {
        var badge = TestBadges.Create(BadgeType.CloseLoss, BadgeLevel.Green);
        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var session = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId, 98)
            .WithWinner(2, 100)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenGameNotFound()
    {
        var badge = TestBadges.Create(BadgeType.CloseLoss, BadgeLevel.Green);
        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)null);

        var session = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId, 98)
            .WithWinner(2, 100)
            .Build();
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
        var badge = TestBadges.Create(BadgeType.CloseLoss, BadgeLevel.Green);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId, playerScore)
            .WithWinner(2, winnerScore)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().Be(expected);
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnTrue_WhenCloseToFirstPlace()
    {
        var badge = TestBadges.Create(BadgeType.CloseLoss, BadgeLevel.Green);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId, 99)
            .WithWinner(2, 100)
            .WithPlayer(3, 50)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenOnlyCloseToLastPlace()
    {
        var badge = TestBadges.Create(BadgeType.CloseLoss, BadgeLevel.Green);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId, 52)
            .WithWinner(2, 100)
            .WithPlayer(3, 50)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNotCloseToAnyOtherPlayer()
    {
        var badge = TestBadges.Create(BadgeType.CloseLoss, BadgeLevel.Green);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId, 75)
            .WithWinner(2, 100)
            .WithPlayer(3, 50)
            .Build();
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
        var badge = TestBadges.Create(BadgeType.CloseLoss, level);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId, 99)
            .WithWinner(2, 100)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    private void SetupGameWithScoring()
    {
        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void VerifyGameLookup()
    {
        _gameRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
    }
}
