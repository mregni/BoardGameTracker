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

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSoloSession()
    {
        var badge = TestBadges.Create(BadgeType.CloseWin, BadgeLevel.Green);
        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, 100)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenAnyScoreIsNull()
    {
        var badge = TestBadges.Create(BadgeType.CloseWin, BadgeLevel.Green);
        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, 100)
            .WithPlayer(2)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenPlayerDidNotWin()
    {
        var badge = TestBadges.Create(BadgeType.CloseWin, BadgeLevel.Green);
        var session = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId, 98)
            .WithWinner(2, 100)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenGameDoesNotSupportScoring()
    {
        var badge = TestBadges.Create(BadgeType.CloseWin, BadgeLevel.Green);
        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, 100)
            .WithPlayer(2, 98)
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
        var badge = TestBadges.Create(BadgeType.CloseWin, BadgeLevel.Green);
        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)null);

        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, 100)
            .WithPlayer(2, 98)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(100, 99, true)]
    [InlineData(100, 98, true)]
    [InlineData(100, 97, false)]
    [InlineData(100.5, 99, true)]
    [InlineData(10, 11, true)]
    [InlineData(10, 12, true)]
    [InlineData(10, 13, false)]
    public async Task CanAwardBadge_ShouldEvaluateScoreDifferenceToLoser(double playerScore, double loserScore, bool expected)
    {
        var badge = TestBadges.Create(BadgeType.CloseWin, BadgeLevel.Green);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, playerScore)
            .WithPlayer(2, loserScore)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().Be(expected);
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenWinnerHasMiddleScore()
    {
        var badge = TestBadges.Create(BadgeType.CloseWin, BadgeLevel.Green);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, 90)
            .WithPlayer(2, 100)
            .WithPlayer(3, 80)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenAllScoresAreEqual()
    {
        var badge = TestBadges.Create(BadgeType.CloseWin, BadgeLevel.Green);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, 100)
            .WithPlayer(2, 100)
            .WithPlayer(3, 100)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenAllOtherPlayersAlsoWon()
    {
        var badge = TestBadges.Create(BadgeType.CloseWin, BadgeLevel.Green);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, 100)
            .WithWinner(2, 98)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCompareWithSecondPlace_NotAllOpponents()
    {
        var badge = TestBadges.Create(BadgeType.CloseWin, BadgeLevel.Green);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, 100)
            .WithPlayer(2, 99)
            .WithPlayer(3, 50)
            .Build();
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNotCloseToSecondPlace()
    {
        var badge = TestBadges.Create(BadgeType.CloseWin, BadgeLevel.Green);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, 100)
            .WithPlayer(2, 90)
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
        var badge = TestBadges.Create(BadgeType.CloseWin, level);
        SetupGameWithScoring();

        var session = new SessionBuilder().ForGame(GameId)
            .WithWinner(PlayerId, 100)
            .WithPlayer(2, 99)
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
