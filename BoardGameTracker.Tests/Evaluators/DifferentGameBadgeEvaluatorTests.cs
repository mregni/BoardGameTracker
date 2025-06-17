using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class DifferentGameBadgeEvaluatorTests
{
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly DifferentGameBadgeEvaluator _evaluator;

    public DifferentGameBadgeEvaluatorTests()
    {
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _evaluator = new DifferentGameBadgeEvaluator(_sessionRepositoryMock.Object);
    }

    [Fact]
    public void BadgeType_ShouldReturnDifferentGames()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.DifferentGames);
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 3, true)]
    [InlineData(BadgeLevel.Green, 2, false)]
    [InlineData(BadgeLevel.Blue, 10, true)]
    [InlineData(BadgeLevel.Blue, 9, false)]
    [InlineData(BadgeLevel.Red, 20, true)]
    [InlineData(BadgeLevel.Red, 19, false)]
    [InlineData(BadgeLevel.Gold, 50, true)]
    [InlineData(BadgeLevel.Gold, 49, false)]
    public async Task CanAwardBadge_WhenDistinctGameCountMatchesLevel_ShouldReturnExpectedResult(
        BadgeLevel level, int distinctGameCount, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = level };
        var session = new Session();
        var sessions = CreateSessionsWithDistinctGames(distinctGameCount);

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, null)).ReturnsAsync(sessions);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().Be(expectedResult);
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenNoSessions_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>();

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, null)).ReturnsAsync(sessions);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeFalse();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenHigherLevelHasEnoughGames_ShouldReturnTrueForLowerLevels()
    {
        var playerId = 1;
        var session = new Session();
        var sessions = CreateSessionsWithDistinctGames(50);

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, null)).ReturnsAsync(sessions);

        var greenBadge = new Badge { Level = BadgeLevel.Green };
        var blueBadge = new Badge { Level = BadgeLevel.Blue };
        var redBadge = new Badge { Level = BadgeLevel.Red };
        var goldBadge = new Badge { Level = BadgeLevel.Gold };

        var greenResult = await _evaluator.CanAwardBadge(playerId, greenBadge, session);
        var blueResult = await _evaluator.CanAwardBadge(playerId, blueBadge, session);
        var redResult = await _evaluator.CanAwardBadge(playerId, redBadge, session);
        var goldResult = await _evaluator.CanAwardBadge(playerId, goldBadge, session);

        greenResult.Should().BeTrue();
        blueResult.Should().BeTrue();
        redResult.Should().BeTrue();
        goldResult.Should().BeTrue();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Exactly(4));
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenMultipleSessionsWithSameGame_ShouldCountDistinctGamesOnly()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>
        {
            new() { GameId = 1 },
            new() { GameId = 1 },
            new() { GameId = 2 },
            new() { GameId = 2 },
            new() { GameId = 3 },
            new() { GameId = 3 }
        };

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, null)).ReturnsAsync(sessions);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeTrue();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    private static List<Session> CreateSessionsWithDistinctGames(int distinctGameCount)
    {
        var sessions = new List<Session>();

        for (var i = 1; i <= distinctGameCount; i++)
        {
            sessions.Add(new Session { GameId = i });
        }

        return sessions;
    }
}