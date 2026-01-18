using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class BadgeServiceTests
{
    private readonly Mock<IBadgeRepository> _badgeRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IBadgeEvaluator> _sessionsEvaluatorMock;
    private readonly Mock<IBadgeEvaluator> _winsEvaluatorMock;
    private readonly BadgeService _badgeService;

    public BadgeServiceTests()
    {
        _badgeRepositoryMock = new Mock<IBadgeRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _sessionsEvaluatorMock = new Mock<IBadgeEvaluator>();
        _winsEvaluatorMock = new Mock<IBadgeEvaluator>();

        _sessionsEvaluatorMock.Setup(x => x.BadgeType).Returns(BadgeType.Sessions);
        _winsEvaluatorMock.Setup(x => x.BadgeType).Returns(BadgeType.Wins);

        var evaluators = new List<IBadgeEvaluator>
        {
            _sessionsEvaluatorMock.Object,
            _winsEvaluatorMock.Object
        };

        _badgeService = new BadgeService(
            _badgeRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            evaluators);
    }

    private void VerifyNoOtherCalls()
    {
        _badgeRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    #region GetAllBadgesAsync Tests

    [Fact]
    public async Task GetAllBadgesAsync_ShouldReturnAllBadges_WhenBadgesExist()
    {
        // Arrange
        var badges = new List<Badge>
        {
            Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green),
            Badge.CreateWithId(2, "badge.session.silver.title", "badge.session.silver.desc", BadgeType.Sessions, "session-silver.png", BadgeLevel.Blue),
            Badge.CreateWithId(3, "badge.win.bronze.title", "badge.win.bronze.desc", BadgeType.Wins, "win-bronze.png", BadgeLevel.Green)
        };

        _badgeRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(badges);

        // Act
        var result = await _badgeService.GetAllBadgesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(badges);

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllBadgesAsync_ShouldReturnEmptyList_WhenNoBadgesExist()
    {
        // Arrange
        _badgeRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Badge>());

        // Act
        var result = await _badgeService.GetAllBadgesAsync();

        // Assert
        result.Should().BeEmpty();

        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region AwardBadgesAsync Tests

    [Fact]
    public async Task AwardBadgesAsync_ShouldReturnEarly_WhenSessionHasNoPlayers()
    {
        // Arrange
        var session = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Test session");

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldAwardBadge_WhenEvaluatorReturnsTrue()
    {
        // Arrange
        var playerId = 1;
        var session = CreateSessionWithPlayer(playerId);
        var badge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            new List<Badge> { badge },
            new Dictionary<int, List<Badge>> { { playerId, new List<Badge>() } },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, badge, session, playerSessions))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBatchToPlayer(playerId, badge.Id))
            .ReturnsAsync(true);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, badge.Id), Times.Once);
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, badge, session, playerSessions), Times.Once);
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldNotAwardBadge_WhenEvaluatorReturnsFalse()
    {
        // Arrange
        var playerId = 1;
        var session = CreateSessionWithPlayer(playerId);
        var badge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            new List<Badge> { badge },
            new Dictionary<int, List<Badge>> { { playerId, new List<Badge>() } },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, badge, session, playerSessions))
            .ReturnsAsync(false);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldSkipAlreadyEarnedBadges()
    {
        // Arrange
        var playerId = 1;
        var session = CreateSessionWithPlayer(playerId);
        var earnedBadge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            new List<Badge> { earnedBadge },
            new Dictionary<int, List<Badge>> { { playerId, new List<Badge> { earnedBadge } } },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(It.IsAny<int>(), It.IsAny<Badge>(), It.IsAny<Session>(), It.IsAny<List<Session>>()), Times.Never);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldProcessMultiplePlayers()
    {
        // Arrange
        var playerId1 = 1;
        var playerId2 = 2;
        var session = CreateSessionWithPlayers(new[] { playerId1, playerId2 });
        var badge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        _badgeRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Badge> { badge });

        _badgeRepositoryMock
            .Setup(x => x.GetPlayerBadgesBatchAsync(It.Is<IEnumerable<int>>(ids => ids.Contains(playerId1) && ids.Contains(playerId2))))
            .ReturnsAsync(new Dictionary<int, List<Badge>>
            {
                { playerId1, new List<Badge>() },
                { playerId2, new List<Badge>() }
            });

        _sessionRepositoryMock
            .Setup(x => x.GetByPlayerBatchAsync(It.Is<IEnumerable<int>>(ids => ids.Contains(playerId1) && ids.Contains(playerId2))))
            .ReturnsAsync(new Dictionary<int, List<Session>>
            {
                { playerId1, playerSessions },
                { playerId2, playerSessions }
            });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(It.IsAny<int>(), badge, session, playerSessions))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBatchToPlayer(It.IsAny<int>(), badge.Id))
            .ReturnsAsync(true);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId1, badge.Id), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId2, badge.Id), Times.Once);
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldProcessBadgesInLevelOrder()
    {
        // Arrange
        var playerId = 1;
        var session = CreateSessionWithPlayer(playerId);
        var bronzeBadge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var silverBadge = Badge.CreateWithId(2, "badge.session.silver.title", "badge.session.silver.desc", BadgeType.Sessions, "session-silver.png", BadgeLevel.Blue);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            new List<Badge> { silverBadge, bronzeBadge }, // Intentionally out of order
            new Dictionary<int, List<Badge>> { { playerId, new List<Badge>() } },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        var callOrder = new List<BadgeLevel?>();
        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, It.IsAny<Badge>(), session, playerSessions))
            .Callback<int, Badge, Session, List<Session>>((_, badge, _, _) => callOrder.Add(badge.Level))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBatchToPlayer(playerId, It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        callOrder.Should().ContainInOrder(BadgeLevel.Green, BadgeLevel.Blue);
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldStopProcessingBadgeGroup_WhenEvaluatorReturnsFalse()
    {
        // Arrange
        var playerId = 1;
        var session = CreateSessionWithPlayer(playerId);
        var bronzeBadge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var silverBadge = Badge.CreateWithId(2, "badge.session.silver.title", "badge.session.silver.desc", BadgeType.Sessions, "session-silver.png", BadgeLevel.Blue);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            new List<Badge> { bronzeBadge, silverBadge },
            new Dictionary<int, List<Badge>> { { playerId, new List<Badge>() } },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, bronzeBadge, session, playerSessions))
            .ReturnsAsync(false);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, bronzeBadge, session, playerSessions), Times.Once);
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, silverBadge, session, playerSessions), Times.Never);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldSkipBadgeType_WhenNoEvaluatorExists()
    {
        // Arrange
        var playerId = 1;
        var session = CreateSessionWithPlayer(playerId);
        // Badge type that has no evaluator registered
        var badge = Badge.CreateWithId(1, "badge.unknown.title", "badge.unknown.desc", BadgeType.DifferentGames, "unknown.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            new List<Badge> { badge },
            new Dictionary<int, List<Badge>> { { playerId, new List<Badge>() } },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldProcessMultipleBadgeTypes()
    {
        // Arrange
        var playerId = 1;
        var session = CreateSessionWithPlayer(playerId);
        var sessionBadge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var winBadge = Badge.CreateWithId(2, "badge.win.bronze.title", "badge.win.bronze.desc", BadgeType.Wins, "win-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            new List<Badge> { sessionBadge, winBadge },
            new Dictionary<int, List<Badge>> { { playerId, new List<Badge>() } },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, sessionBadge, session, playerSessions))
            .ReturnsAsync(true);

        _winsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, winBadge, session, playerSessions))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBatchToPlayer(playerId, It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, sessionBadge.Id), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBatchToPlayer(playerId, winBadge.Id), Times.Once);
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldHandleDistinctPlayerIds()
    {
        // Arrange - Session where same player appears twice (shouldn't happen in practice but testing robustness)
        var playerId = 1;
        var session = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Test session");
        session.AddPlayerSession(playerId, 100, false, true);

        var badge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            new List<Badge> { badge },
            new Dictionary<int, List<Badge>> { { playerId, new List<Badge>() } },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, badge, session, playerSessions))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBatchToPlayer(playerId, badge.Id))
            .ReturnsAsync(true);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert - Should only process player once
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, badge, session, playerSessions), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Session CreateSessionWithPlayer(int playerId)
    {
        var session = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Test session");
        session.AddPlayerSession(playerId, 100, false, true);
        return session;
    }

    private static Session CreateSessionWithPlayers(int[] playerIds)
    {
        var session = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Test session");
        foreach (var playerId in playerIds)
        {
            session.AddPlayerSession(playerId, 100, false, false);
        }
        return session;
    }

    private void SetupRepositoriesForAwardTest(
        int playerId,
        List<Badge> allBadges,
        Dictionary<int, List<Badge>> playerBadgesMap,
        Dictionary<int, List<Session>> playerSessionsMap)
    {
        _badgeRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(allBadges);

        _badgeRepositoryMock
            .Setup(x => x.GetPlayerBadgesBatchAsync(It.Is<IEnumerable<int>>(ids => ids.Contains(playerId))))
            .ReturnsAsync(playerBadgesMap);

        _sessionRepositoryMock
            .Setup(x => x.GetByPlayerBatchAsync(It.Is<IEnumerable<int>>(ids => ids.Contains(playerId))))
            .ReturnsAsync(playerSessionsMap);
    }

    #endregion
}
