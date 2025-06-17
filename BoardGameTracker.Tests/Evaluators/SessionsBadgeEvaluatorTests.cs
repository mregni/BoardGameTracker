using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class SessionsBadgeEvaluatorTests
{
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly SessionsBadgeEvaluator _evaluator;

    public SessionsBadgeEvaluatorTests()
    {
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _evaluator = new SessionsBadgeEvaluator(_sessionRepositoryMock.Object);
    }

    [Fact]
    public void BadgeType_ShouldReturnSessions()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.Sessions);
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 5, true)]
    [InlineData(BadgeLevel.Green, 4, false)]
    [InlineData(BadgeLevel.Blue, 10, true)]
    [InlineData(BadgeLevel.Blue, 9, false)]
    [InlineData(BadgeLevel.Red, 50, true)]
    [InlineData(BadgeLevel.Red, 49, false)]
    [InlineData(BadgeLevel.Gold, 100, true)]
    [InlineData(BadgeLevel.Gold, 99, false)]
    public async Task CanAwardBadge_WhenSessionCountMatchesLevel_ShouldReturnExpectedResult(
        BadgeLevel level, int sessionCount, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = level };
        var session = new Session();

        _sessionRepositoryMock.Setup(x => x.CountByPlayer(playerId)).ReturnsAsync(sessionCount);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().Be(expectedResult);
        
        _sessionRepositoryMock.Verify(x => x.CountByPlayer(playerId), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenNoSessions_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();

        _sessionRepositoryMock.Setup(x => x.CountByPlayer(playerId)).ReturnsAsync(0);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeFalse();
        
        _sessionRepositoryMock.Verify(x => x.CountByPlayer(playerId), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenHigherLevelHasEnoughSessions_ShouldReturnTrueForLowerLevels()
    {
        var playerId = 1;
        var session = new Session();

        _sessionRepositoryMock.Setup(x => x.CountByPlayer(playerId)).ReturnsAsync(100);

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
        
        _sessionRepositoryMock.Verify(x => x.CountByPlayer(playerId), Times.Exactly(4));
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }
}