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

public class SessionWinEvaluatorTests
{
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly SessionWinEvaluator _evaluator;

    public SessionWinEvaluatorTests()
    {
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _evaluator = new SessionWinEvaluator(_sessionRepositoryMock.Object);
    }

    [Fact]
    public void BadgeType_ShouldReturnWins()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.Wins);
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 3, true)]
    [InlineData(BadgeLevel.Green, 2, false)]
    [InlineData(BadgeLevel.Blue, 10, true)]
    [InlineData(BadgeLevel.Blue, 9, false)]
    [InlineData(BadgeLevel.Red, 25, true)]
    [InlineData(BadgeLevel.Red, 24, false)]
    [InlineData(BadgeLevel.Gold, 50, true)]
    [InlineData(BadgeLevel.Gold, 49, false)]
    public async Task CanAwardBadge_WhenWinCountMatchesLevel_ShouldReturnExpectedResult(
        BadgeLevel level, int winCount, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = level };
        var session = new Session();
        var sessions = CreateSessions(winCount);

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, true)).ReturnsAsync(sessions);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().Be(expectedResult);
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, true), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenNoWins_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessions = new List<Session>();

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, true)).ReturnsAsync(sessions);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeFalse();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, true), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenHigherLevelHasEnoughWins_ShouldReturnTrueForLowerLevels()
    {
        var playerId = 1;
        var session = new Session();
        var sessions = CreateSessions(50);

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, true)).ReturnsAsync(sessions);

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
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, true), Times.Exactly(4));
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    private static List<Session> CreateSessions(int count)
    {
        var sessions = new List<Session>();

        for (var i = 0; i < count; i++)
        {
            sessions.Add(new Session());
        }

        return sessions;
    }
}