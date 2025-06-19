using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
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

    public CloseWinBadgeEvaluatorTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _evaluator = new CloseWinBadgeEvaluator(_gameRepositoryMock.Object);
    }

    [Fact]
    public void BadgeType_ShouldReturnCloseWin()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.CloseWin);
    }

    [Fact]
    public async Task CanAwardBadge_WhenSoloSession_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = 10 }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, []);

        result.Should().BeFalse();
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenPlayerScoreIsNull_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = null, Won = true },
                new() { PlayerId = 2, Score = 10, Won = false }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, []);

        result.Should().BeFalse();
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenAnyPlayerScoreIsNull_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = 10, Won = true },
                new() { PlayerId = 2, Score = null, Won = false }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, []);

        result.Should().BeFalse();
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenPlayerDidNotWin_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = 8, Won = false },
                new() { PlayerId = 2, Score = 10, Won = true }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, []);

        result.Should().BeFalse();
        _gameRepositoryMock.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task CanAwardBadge_WhenGameNotFound_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = 10, Won = true },
                new() { PlayerId = 2, Score = 8, Won = false }
            }
        };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Game?)null);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, []);

        result.Should().BeFalse();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenGameHasNoScoring_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = 10, Won = true },
                new() { PlayerId = 2, Score = 8, Won = false }
            }
        };
        var game = new Game { HasScoring = false };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, []);

        result.Should().BeFalse();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(10, 8, true)]
    [InlineData(10, 7, false)] 
    [InlineData(5, 7, true)]
    [InlineData(5, 8,  false)] 
    public async Task CanAwardBadge_WhenCloseWinScenarios_ShouldReturnExpectedResult(
        double playerScore, double opponentScore, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = playerScore, Won = true },
                new() { PlayerId = 2, Score = opponentScore, Won = false }
            }
        };
        var game = new Game { HasScoring = true };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, []);

        result.Should().Be(expectedResult);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenAllOtherPlayersAlsoWon_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = 10, Won = true },
                new() { PlayerId = 2, Score = 10, Won = true },
                new() { PlayerId = 3, Score = 10, Won = true }
            }
        };
        var game = new Game { HasScoring = true };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, []);

        result.Should().BeFalse();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenMultiplePlayersHighScoreWin_ShouldReturnTrue()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = 10, Won = true },
                new() { PlayerId = 2, Score = 8, Won = false },
                new() { PlayerId = 3, Score = 7, Won = false },
                new() { PlayerId = 4, Score = 6, Won = false }
            }
        };
        var game = new Game { HasScoring = true };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, []);

        result.Should().BeTrue();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenMultiplePlayersLowScoreWin_ShouldReturnTrue()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = 5, Won = true },
                new() { PlayerId = 2, Score = 7, Won = false },
                new() { PlayerId = 3, Score = 8, Won = false },
                new() { PlayerId = 4, Score = 9, Won = false }
            }
        };
        var game = new Game { HasScoring = true };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session, []);

        result.Should().BeTrue();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
    }
}