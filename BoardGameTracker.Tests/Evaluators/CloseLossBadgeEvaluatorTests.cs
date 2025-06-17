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

public class CloseLossBadgeEvaluatorTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly CloseLossBadgeEvaluator _evaluator;

    public CloseLossBadgeEvaluatorTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _evaluator = new CloseLossBadgeEvaluator(_gameRepositoryMock.Object);
    }

    [Fact]
    public void BadgeType_ShouldReturnCloseLoss()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.CLoseLoss);
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
                new() { PlayerId = playerId, Score = 10, Won = false }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

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
                new() { PlayerId = playerId, Score = null, Won = false },
                new() { PlayerId = 2, Score = 10, Won = true }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

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
                new() { PlayerId = playerId, Score = 8, Won = false },
                new() { PlayerId = 2, Score = null, Won = true }
            }
        };

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeFalse();
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenPlayerWon_ShouldReturnFalse()
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

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

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
                new() { PlayerId = playerId, Score = 8, Won = false },
                new() { PlayerId = 2, Score = 10, Won = true }
            }
        };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Game?)null);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

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
                new() { PlayerId = playerId, Score = 8, Won = false },
                new() { PlayerId = 2, Score = 10, Won = true }
            }
        };
        var game = new Game { HasScoring = false };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeFalse();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(8, 10, true)]
    [InlineData(7, 10, false)]
    [InlineData(7, 5, true)]
    [InlineData(8, 5, false)] 
    public async Task CanAwardBadge_WhenCloseLossScenarios_ShouldReturnExpectedResult(
        double playerScore, double winnerScore, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = playerScore, Won = false },
                new() { PlayerId = 2, Score = winnerScore, Won = true }
            }
        };
        var game = new Game { HasScoring = true };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().Be(expectedResult);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenPlayerTiedForLoss_ShouldReturnTrue()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = 8, Won = false },
                new() { PlayerId = 2, Score = 8, Won = false },
                new() { PlayerId = 3, Score = 10, Won = true }
            }
        };
        var game = new Game { HasScoring = true };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeTrue();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenMultiplePlayersHighScoreLoss_ShouldReturnTrue()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = 8, Won = false },
                new() { PlayerId = 2, Score = 10, Won = true },
                new() { PlayerId = 3, Score = 7, Won = false },
                new() { PlayerId = 4, Score = 6, Won = false }
            }
        };
        var game = new Game { HasScoring = true };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeTrue();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenMultiplePlayersLowScoreLoss_ShouldReturnTrue()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session
        {
            GameId = 1,
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId, Score = 7, Won = false },
                new() { PlayerId = 2, Score = 5, Won = true },
                new() { PlayerId = 3, Score = 8, Won = false },
                new() { PlayerId = 4, Score = 9, Won = false }
            }
        };
        var game = new Game { HasScoring = true };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeTrue();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
    }
}