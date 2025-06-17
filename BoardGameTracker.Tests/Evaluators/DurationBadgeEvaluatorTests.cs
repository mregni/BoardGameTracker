using System;
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

public class DurationBadgeEvaluatorTests
{
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly DurationBadgeEvaluator _evaluator;

    public DurationBadgeEvaluatorTests()
    {
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _evaluator = new DurationBadgeEvaluator(_sessionRepositoryMock.Object);
    }

    [Fact]
    public void BadgeType_ShouldReturnDuration()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.Duration);
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 300, true)]
    [InlineData(BadgeLevel.Green, 299, false)]
    [InlineData(BadgeLevel.Blue, 600, true)]
    [InlineData(BadgeLevel.Blue, 599, false)]
    [InlineData(BadgeLevel.Red, 3000, true)]
    [InlineData(BadgeLevel.Red, 2999, false)]
    [InlineData(BadgeLevel.Gold, 6000, true)]
    [InlineData(BadgeLevel.Gold, 5999, false)]
    public async Task CanAwardBadge_WhenTotalDurationMatchesLevel_ShouldReturnExpectedResult(
        BadgeLevel level, int totalMinutes, bool expectedResult)
    {
        var playerId = 1;
        var badge = new Badge { Level = level };
        var session = new Session();
        var sessions = CreateSessionsWithTotalDuration(totalMinutes);

        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, true)).ReturnsAsync(sessions);

        var result = await _evaluator.CanAwardBadge(playerId, badge, session);

        result.Should().Be(expectedResult);
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, true), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanAwardBadge_WhenNoWinningSessions_ShouldReturnFalse()
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
    public async Task CanAwardBadge_WhenHigherLevelHasEnoughDuration_ShouldReturnTrueForLowerLevels()
    {
        var playerId = 1;
        var session = new Session();
        var sessions = CreateSessionsWithTotalDuration(6000);

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

    private static List<Session> CreateSessionsWithTotalDuration(int totalMinutes)
    {
        var sessions = new List<Session>();
        var baseDate = new DateTime(2025, 1, 1, 10, 0, 0);
        
        if (totalMinutes > 0)
        {
            sessions.Add(new Session
            {
                Start = baseDate,
                End = baseDate.AddMinutes(totalMinutes)
            });
        }

        return sessions;
    }
}