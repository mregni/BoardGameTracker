using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class BadgeServiceTests
{
    private readonly Mock<IBadgeRepository> _badgeRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IBadgeEvaluator> _evaluatorMock1;
    private readonly Mock<IBadgeEvaluator> _evaluatorMock2;
    private readonly BadgeService _service;

    public BadgeServiceTests()
    {
        _badgeRepositoryMock = new Mock<IBadgeRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _evaluatorMock1 = new Mock<IBadgeEvaluator>();
        _evaluatorMock2 = new Mock<IBadgeEvaluator>();

        _evaluatorMock1.Setup(x => x.BadgeType).Returns(BadgeType.Wins);
        _evaluatorMock2.Setup(x => x.BadgeType).Returns(BadgeType.Sessions);

        var evaluators = new[] {_evaluatorMock1.Object, _evaluatorMock2.Object};
        _service = new BadgeService(_badgeRepositoryMock.Object, _sessionRepositoryMock.Object, evaluators);
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenNoPlayers_ShouldNotCallAnyMethods()
    {
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>()
        };

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.VerifyNoOtherCalls();

        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();

        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
        
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenPlayerHasAllBadges_ShouldNotAwardAny()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() {PlayerId = playerId}
            }
        };
        var badges = new List<Badge>
        {
            new() {Id = 1, Type = BadgeType.Wins, Level = BadgeLevel.Green},
            new() {Id = 2, Type = BadgeType.Sessions, Level = BadgeLevel.Green}
        };
        var playerBadges = new List<Badge>
        {
            new() {Id = 1, Type = BadgeType.Wins, Level = BadgeLevel.Green},
            new() {Id = 2, Type = BadgeType.Sessions, Level = BadgeLevel.Green}
        };

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _badgeRepositoryMock.VerifyNoOtherCalls();

        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();

        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenNoEvaluatorForBadgeType_ShouldSkipEntireGroup()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() {PlayerId = playerId}
            }
        };
        var badges = new List<Badge>
        {
            new() {Id = 1, Type = BadgeType.WinPercentage, Level = BadgeLevel.Green},
            new() {Id = 2, Type = BadgeType.WinPercentage, Level = BadgeLevel.Blue}
        };
        var playerBadges = new List<Badge>();

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _badgeRepositoryMock.VerifyNoOtherCalls();

        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();

        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenEvaluatorReturnsFalseForFirstBadge_ShouldStopProcessingGroup()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() {PlayerId = playerId}
            }
        };
        var badges = new List<Badge>
        {
            new() {Id = 1, Type = BadgeType.Wins, Level = BadgeLevel.Green},
            new() {Id = 2, Type = BadgeType.Wins, Level = BadgeLevel.Blue}
        };
        var playerBadges = new List<Badge>();
        var sessions = new List<Session>();

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(playerId, badges[0], session, sessions)).ReturnsAsync(false);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, null)).ReturnsAsync(sessions);
        
        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _badgeRepositoryMock.VerifyNoOtherCalls();

        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[0], session, sessions), Times.Once);
        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[1], session, sessions), Times.Never);
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();

        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenEvaluatorReturnsTrueForFirstBadge_ShouldAwardAndContinue()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() {PlayerId = playerId}
            }
        };
        var badges = new List<Badge>
        {
            new() {Id = 1, Type = BadgeType.Wins, Level = BadgeLevel.Green},
            new() {Id = 2, Type = BadgeType.Wins, Level = BadgeLevel.Blue}
        };
        var playerBadges = new List<Badge>();
        var sessions = new List<Session>();
        
        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(playerId, badges[0], session, sessions)).ReturnsAsync(true);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(playerId, badges[1], session, sessions)).ReturnsAsync(false);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, null)).ReturnsAsync(sessions);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 1), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 2), Times.Never);
        _badgeRepositoryMock.VerifyNoOtherCalls();

        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[0], session, sessions), Times.Once);
        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[1], session, sessions), Times.Once);
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();

        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenBothBadgesCanBeAwarded_ShouldAwardBoth()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() {PlayerId = playerId}
            }
        };
        var badges = new List<Badge>
        {
            new() {Id = 1, Type = BadgeType.Wins, Level = BadgeLevel.Green},
            new() {Id = 2, Type = BadgeType.Wins, Level = BadgeLevel.Blue}
        };
        var playerBadges = new List<Badge>();
        var sessions = new List<Session>();
        
        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(playerId, badges[0], session, sessions)).ReturnsAsync(true);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(playerId, badges[1], session, sessions)).ReturnsAsync(true);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, null)).ReturnsAsync(sessions);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 1), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 2), Times.Once);
        _badgeRepositoryMock.VerifyNoOtherCalls();

        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[0], session, sessions), Times.Once);
        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[1], session, sessions), Times.Once);
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();

        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenMultiplePlayers_ShouldProcessEachPlayer()
    {
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() {PlayerId = 1},
                new() {PlayerId = 2}
            }
        };
        var badges = new List<Badge>
        {
            new() {Id = 1, Type = BadgeType.Wins, Level = BadgeLevel.Green}
        };
        var playerBadges = new List<Badge>();
        var sessions = new List<Session>();

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(1)).ReturnsAsync(playerBadges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(2)).ReturnsAsync(playerBadges);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(1, badges[0], session, sessions)).ReturnsAsync(true);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(2, badges[0], session, sessions)).ReturnsAsync(false);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(1, null)).ReturnsAsync(sessions);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(2, null)).ReturnsAsync(sessions);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Exactly(2));
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(1), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(2), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(1, 1), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(2, It.IsAny<int>()), Times.Never);
        _badgeRepositoryMock.VerifyNoOtherCalls();

        _evaluatorMock1.Verify(x => x.CanAwardBadge(1, badges[0], session, sessions), Times.Once);
        _evaluatorMock1.Verify(x => x.CanAwardBadge(2, badges[0], session, sessions), Times.Once);
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();

        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(1, null), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(2, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenMultipleBadgeTypes_ShouldProcessEachTypeIndependently()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() {PlayerId = playerId}
            }
        };
        var badges = new List<Badge>
        {
            new() {Id = 1, Type = BadgeType.Wins, Level = BadgeLevel.Green},
            new() {Id = 2, Type = BadgeType.Sessions, Level = BadgeLevel.Green}
        };
        var playerBadges = new List<Badge>();
        var sessions = new List<Session>();

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(playerId, badges[0], session, sessions)).ReturnsAsync(true);
        _evaluatorMock2.Setup(x => x.CanAwardBadge(playerId, badges[1], session, sessions)).ReturnsAsync(true);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, null)).ReturnsAsync(sessions);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 1), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 2), Times.Once);
        _badgeRepositoryMock.VerifyNoOtherCalls();

        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[0], session, sessions), Times.Once);
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();

        _evaluatorMock2.Verify(x => x.CanAwardBadge(playerId, badges[1], session, sessions), Times.Once);
        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenPlayerHasLowerLevelBadge_ShouldOnlyProcessHigherLevels()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() {PlayerId = playerId}
            }
        };
        var badges = new List<Badge>
        {
            new() {Id = 1, Type = BadgeType.Wins, Level = BadgeLevel.Green},
            new() {Id = 2, Type = BadgeType.Wins, Level = BadgeLevel.Blue},
            new() {Id = 3, Type = BadgeType.Sessions, Level = BadgeLevel.Green}
        };
        var playerBadges = new List<Badge>
        {
            new() {Id = 1, Type = BadgeType.Wins, Level = BadgeLevel.Green}
        };
        var sessions = new List<Session>();

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(playerId, badges[1], session, sessions)).ReturnsAsync(true);
        _evaluatorMock2.Setup(x => x.CanAwardBadge(playerId, badges[2], session, sessions)).ReturnsAsync(true);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, null)).ReturnsAsync(sessions);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 2), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 3), Times.Once);
        _badgeRepositoryMock.VerifyNoOtherCalls();

        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[0], session, sessions), Times.Never);
        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[1], session, sessions), Times.Once);
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();

        _evaluatorMock2.Verify(x => x.CanAwardBadge(playerId, badges[2], session, sessions), Times.Once);
        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenBadgesOrderedByLevel_ShouldProcessInCorrectOrder()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() {PlayerId = playerId}
            }
        };
        var badges = new List<Badge>
        {
            new() {Id = 2, Type = BadgeType.Wins, Level = BadgeLevel.Blue},
            new() {Id = 1, Type = BadgeType.Wins, Level = BadgeLevel.Green},
            new() {Id = 3, Type = BadgeType.Wins, Level = BadgeLevel.Red}
        };
        var playerBadges = new List<Badge>();
        var sessions = new List<Session>();

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(playerId, It.IsAny<Badge>(), session, new List<Session>())).ReturnsAsync(true);
        _sessionRepositoryMock.Setup(x => x.GetByPlayer(playerId, null)).ReturnsAsync(sessions);

        _evaluatorMock1
            .Setup(x => x.CanAwardBadge(playerId, badges[0], session, sessions))
            .ReturnsAsync(true);
        _evaluatorMock1
            .Setup(x => x.CanAwardBadge(playerId, badges[1], session, sessions))
            .ReturnsAsync(true);
        _evaluatorMock1
            .Setup(x => x.CanAwardBadge(playerId, badges[2], session, sessions))
            .ReturnsAsync(true);
        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 1), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 2), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 3), Times.Once);
        _badgeRepositoryMock.VerifyNoOtherCalls();
        
        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[0], session, sessions), Times.Once);
        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[1], session, sessions), Times.Once);
        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[2], session, sessions), Times.Once);
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();

        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
        
        _sessionRepositoryMock.Verify(x => x.GetByPlayer(playerId, null), Times.Once);
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }
}