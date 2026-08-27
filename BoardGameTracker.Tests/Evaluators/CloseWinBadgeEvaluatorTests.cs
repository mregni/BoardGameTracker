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

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenSoloSession()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyNoOtherCalls();
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
        VerifyNoOtherCalls();
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
        VerifyNoOtherCalls();
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
        VerifyGameLookup();
        VerifyNoOtherCalls();
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
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, playerScore, false, true);
        session.AddPlayerSession(2, loserScore, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().Be(expected);
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenWinnerHasMiddleScore()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 90, false, true);
        session.AddPlayerSession(2, 100, false, false);
        session.AddPlayerSession(3, 80, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenAllScoresAreEqual()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 100, false, false);
        session.AddPlayerSession(3, 100, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenAllOtherPlayersAlsoWon()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 98, false, true);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeFalse();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldCompareWithSecondPlace_NotAllOpponents()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 99, false, false);
        session.AddPlayerSession(3, 50, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNotCloseToSecondPlace()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        SetupGameWithScoring();

        var session = CreateSession();
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 90, false, false);
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
        session.AddPlayerSession(PlayerId, 100, false, true);
        session.AddPlayerSession(2, 99, false, false);
        var sessions = new List<Session> { session };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, session, sessions);

        result.Should().BeTrue();
        VerifyGameLookup();
        VerifyNoOtherCalls();
    }

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

    private void VerifyGameLookup()
    {
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(GameId), Times.Once);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
    }
}
