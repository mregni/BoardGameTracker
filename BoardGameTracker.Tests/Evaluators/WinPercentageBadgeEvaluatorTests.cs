using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class WinPercentageBadgeEvaluatorTests
{
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly WinPercentageBadgeEvaluator _evaluator;

    public WinPercentageBadgeEvaluatorTests()
    {
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _evaluator = new WinPercentageBadgeEvaluator(_sessionRepositoryMock.Object);
    }

    [Fact]
    public void BadgeType_ShouldReturnWinPercentage()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.WinPercentage);
    }

    [Fact]
    public async Task CanAwardBadge_WhenTotalSessionsLessThan5_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessionsLost = new List<Session> { new() };
        var sessionsWon = new List<Session> { new(), new() };

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, false)).ReturnsAsync(sessionsLost);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, true)).ReturnsAsync(sessionsWon);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeFalse();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, true), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, false), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenTotalSessionsExactly5_ShouldEvaluateWinPercentage()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessionsLost = new List<Session> { new(), new() };
        var sessionsWon = new List<Session> { new(), new(), new() };

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, false)).ReturnsAsync(sessionsLost);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, true)).ReturnsAsync(sessionsWon);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeTrue();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, true), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, false), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 100, true)]
    [InlineData(BadgeLevel.Green, 30, true)]
    [InlineData(BadgeLevel.Green, 29, false)]
    [InlineData(BadgeLevel.Blue, 100, true)]
    [InlineData(BadgeLevel.Blue, 50, true)]
    [InlineData(BadgeLevel.Blue, 49, false)]
    [InlineData(BadgeLevel.Red, 100, true)]
    [InlineData(BadgeLevel.Red, 65, true)]
    [InlineData(BadgeLevel.Red, 64, false)]
    [InlineData(BadgeLevel.Gold, 100, true)]
    [InlineData(BadgeLevel.Gold, 80, true)]
    [InlineData(BadgeLevel.Gold, 79, false)]
    public async Task CanAwardBadge_WhenWinPercentageMatchesLevel_ShouldReturnExpectedResult(
        BadgeLevel level, int winPercentage, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = level };
        var session = new Session();
        
        var totalSessions = 100;
        var wonSessions = (int)(totalSessions * winPercentage / 100.0);
        var lostSessions = totalSessions - wonSessions;
        
        var sessionsLost = Enumerable.Range(0, lostSessions).Select(_ => new Session()).ToList();
        var sessionsWon = Enumerable.Range(0, wonSessions).Select(_ => new Session()).ToList();

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, false)).ReturnsAsync(sessionsLost);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, true)).ReturnsAsync(sessionsWon);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().Be(expectedResult);
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, true), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, false), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenNoSessionsWon_ShouldReturnFalse()
    {
        var playerId = 1;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessionsLost = Enumerable.Range(0, 10).Select(_ => new Session()).ToList();
        var sessionsWon = new List<Session>();

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, false)).ReturnsAsync(sessionsLost);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, true)).ReturnsAsync(sessionsWon);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().BeFalse();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, true), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, false), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenRepositoryCallsAreMade_ShouldCallWithCorrectParameters()
    {
        var playerId = 42;
        var badge = new Badge { Level = BadgeLevel.Green };
        var session = new Session();
        var sessionsLost = Enumerable.Range(0, 2).Select(_ => new Session()).ToList();
        var sessionsWon = Enumerable.Range(0, 8).Select(_ => new Session()).ToList();

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, false)).ReturnsAsync(sessionsLost);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, true)).ReturnsAsync(sessionsWon);

        await _evaluator.CanAwardBadge(playerId, badge, session);

        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, false), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, true), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }
}