using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges;
using BoardGameTracker.Core.Badges.Interfaces;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class BadgeServiceTests
{
    private readonly Mock<IBadgeRepository> _badgeRepositoryMock;
    private readonly Mock<IBadgeEvaluator> _evaluatorMock1;
    private readonly Mock<IBadgeEvaluator> _evaluatorMock2;
    private readonly BadgeService _service;

    public BadgeServiceTests()
    {
        _badgeRepositoryMock = new Mock<IBadgeRepository>();
        _evaluatorMock1 = new Mock<IBadgeEvaluator>();
        _evaluatorMock2 = new Mock<IBadgeEvaluator>();
        
        _evaluatorMock1.Setup(x => x.BadgeType).Returns(BadgeType.Wins);
        _evaluatorMock2.Setup(x => x.BadgeType).Returns(BadgeType.Sessions);
        
        var evaluators = new[] { _evaluatorMock1.Object, _evaluatorMock2.Object };
        _service = new BadgeService(_badgeRepositoryMock.Object, evaluators);
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
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenPlayerHasAllBadges_ShouldNotAwardAny()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId }
            }
        };
        var badges = new List<Badge>
        {
            new() { Id = 1, Type = BadgeType.Wins },
            new() { Id = 2, Type = BadgeType.Sessions }
        };
        var playerBadges = new List<Badge>
        {
            new() { Id = 1, Type = BadgeType.Wins },
            new() { Id = 2, Type = BadgeType.Sessions }
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
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenNoEvaluatorForBadgeType_ShouldSkipBadge()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId }
            }
        };
        var badges = new List<Badge>
        {
            new() { Id = 1, Type = BadgeType.WinPercentage }
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
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenEvaluatorReturnsFalse_ShouldNotAwardBadge()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId }
            }
        };
        var badges = new List<Badge>
        {
            new() { Id = 1, Type = BadgeType.Wins }
        };
        var playerBadges = new List<Badge>();

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(playerId, badges[0], session)).ReturnsAsync(false);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _badgeRepositoryMock.VerifyNoOtherCalls();
        
        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[0], session), Times.Once);
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();
        
        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenEvaluatorReturnsTrue_ShouldAwardBadge()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId }
            }
        };
        var badges = new List<Badge>
        {
            new() { Id = 1, Type = BadgeType.Wins }
        };
        var playerBadges = new List<Badge>();

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(playerId, badges[0], session)).ReturnsAsync(true);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 1), Times.Once);
        _badgeRepositoryMock.VerifyNoOtherCalls();
        
        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[0], session), Times.Once); 
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();
        
        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();       
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenMultiplePlayers_ShouldProcessEachPlayer()
    {
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = 1 },
                new() { PlayerId = 2 }
            }
        };
        var badges = new List<Badge>
        {
            new() { Id = 1, Type = BadgeType.Wins }
        };
        var playerBadges = new List<Badge>();

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(1)).ReturnsAsync(playerBadges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(2)).ReturnsAsync(playerBadges);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(1, badges[0], session)).ReturnsAsync(true);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(2, badges[0], session)).ReturnsAsync(false);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Exactly(2));
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(1), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(2), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(1, 1), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(2, It.IsAny<int>()), Times.Never);
        _badgeRepositoryMock.VerifyNoOtherCalls();
        
        _evaluatorMock1.Verify(x => x.CanAwardBadge(1, badges[0], session), Times.Once);
        _evaluatorMock1.Verify(x => x.CanAwardBadge(2, badges[0], session), Times.Once);
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();
        
        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls(); 
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenMultipleBadges_ShouldProcessEachBadge()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId }
            }
        };
        var badges = new List<Badge>
        {
            new() { Id = 1, Type = BadgeType.Wins },
            new() { Id = 2, Type = BadgeType.Sessions }
        };
        var playerBadges = new List<Badge>();

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);
        _evaluatorMock1.Setup(x => x.CanAwardBadge(playerId, badges[0], session)).ReturnsAsync(true);
        _evaluatorMock2.Setup(x => x.CanAwardBadge(playerId, badges[1], session)).ReturnsAsync(true);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 1), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 2), Times.Once);
        _badgeRepositoryMock.VerifyNoOtherCalls();
        
        _evaluatorMock1.Verify(x => x.CanAwardBadge(playerId, badges[0], session), Times.Once);
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();
        
        _evaluatorMock2.Verify(x => x.CanAwardBadge(playerId, badges[1], session), Times.Once);
        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_WhenPlayerHasSomeBadges_ShouldOnlyProcessNewBadges()
    {
        var playerId = 1;
        var session = new Session
        {
            PlayerSessions = new List<PlayerSession>
            {
                new() { PlayerId = playerId }
            }
        };
        var badges = new List<Badge>
        {
            new() { Id = 1, Type = BadgeType.Wins },
            new() { Id = 2, Type = BadgeType.Sessions }
        };
        var playerBadges = new List<Badge>
        {
            new() { Id = 1, Type = BadgeType.Wins }
        };

        _badgeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(badges);
        _badgeRepositoryMock.Setup(x => x.GetPlayerBadgesAsync(playerId)).ReturnsAsync(playerBadges);
        _evaluatorMock2.Setup(x => x.CanAwardBadge(playerId, badges[1], session)).ReturnsAsync(true);

        await _service.AwardBadgesAsync(session);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesAsync(playerId), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, 2), Times.Once);
        _badgeRepositoryMock.VerifyNoOtherCalls();
        
        _evaluatorMock1.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock1.VerifyNoOtherCalls();
        
        _evaluatorMock2.Verify(x => x.CanAwardBadge(playerId, badges[1], session), Times.Once);
        _evaluatorMock2.VerifyGet(x => x.BadgeType, Times.Once);
        _evaluatorMock2.VerifyNoOtherCalls();
    }
}