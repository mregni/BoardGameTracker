using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Badges;

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
            evaluators,
            NullLogger<BadgeService>.Instance);
    }

    private void VerifyNoOtherCalls()
    {
        _badgeRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _sessionsEvaluatorMock.Verify(x => x.BadgeType, Times.Once);
        _sessionsEvaluatorMock.VerifyNoOtherCalls();
        _winsEvaluatorMock.Verify(x => x.BadgeType, Times.Once);
        _winsEvaluatorMock.VerifyNoOtherCalls();
    }

    private void VerifyAwardRepositoryReads(params int[] playerIds)
    {
        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _badgeRepositoryMock.Verify(x => x.GetPlayerBadgesBatchAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(playerIds))), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetByPlayerBatchAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(playerIds))), Times.Once);
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
        result.Should().BeEquivalentTo(badges);

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
        _badgeRepositoryMock.Verify(x => x.GetAllAsync(), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldAwardBadge_WhenEvaluatorReturnsTrue()
    {
        // Arrange
        var playerId = 1;
        var session = new SessionBuilder().WithWinner(playerId, 100).Build();
        var badge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            [badge],
            new Dictionary<int, List<Badge>> { { playerId, []} },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, badge, session, playerSessions))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBadgeToPlayer(playerId, badge.Id))
            .ReturnsAsync(true);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, badge.Id), Times.Once);
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, badge, session, playerSessions), Times.Once);
        VerifyAwardRepositoryReads(playerId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldNotAwardBadge_WhenEvaluatorReturnsFalse()
    {
        // Arrange
        var playerId = 1;
        var session = new SessionBuilder().WithWinner(playerId, 100).Build();
        var badge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            [badge],
            new Dictionary<int, List<Badge>> { { playerId, []} },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, badge, session, playerSessions))
            .ReturnsAsync(false);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, badge, session, playerSessions), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, badge.Id), Times.Never);
        VerifyAwardRepositoryReads(playerId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldSkipAlreadyEarnedBadges()
    {
        // Arrange
        var playerId = 1;
        var session = new SessionBuilder().WithWinner(playerId, 100).Build();
        var earnedBadge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            [earnedBadge],
            new Dictionary<int, List<Badge>> { { playerId, [earnedBadge]} },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, earnedBadge, session, playerSessions), Times.Never);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, earnedBadge.Id), Times.Never);
        VerifyAwardRepositoryReads(playerId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldProcessMultiplePlayers()
    {
        // Arrange
        var playerId1 = 1;
        var playerId2 = 2;
        var session = new SessionBuilder().WithPlayers(playerId1, playerId2).Build();
        var badge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var firstPlayerSessions = new List<Session> { session };
        var secondPlayerSessions = new List<Session> { session, new SessionBuilder().WithPlayer(playerId2).Build() };

        SetupRepositories(
            [playerId1, playerId2],
            [badge],
            new Dictionary<int, List<Badge>> { { playerId1, [] }, { playerId2, [] } },
            new Dictionary<int, List<Session>> { { playerId1, firstPlayerSessions }, { playerId2, secondPlayerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId1, badge, session, firstPlayerSessions))
            .ReturnsAsync(true);

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId2, badge, session, secondPlayerSessions))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBadgeToPlayer(playerId1, badge.Id))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBadgeToPlayer(playerId2, badge.Id))
            .ReturnsAsync(true);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId1, badge, session, firstPlayerSessions), Times.Once);
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId2, badge, session, secondPlayerSessions), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId1, badge.Id), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId2, badge.Id), Times.Once);
        VerifyAwardRepositoryReads(playerId1, playerId2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldProcessBadgesInLevelOrder()
    {
        // Arrange
        var playerId = 1;
        var session = new SessionBuilder().WithWinner(playerId, 100).Build();
        var bronzeBadge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var silverBadge = Badge.CreateWithId(2, "badge.session.silver.title", "badge.session.silver.desc", BadgeType.Sessions, "session-silver.png", BadgeLevel.Blue);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            [silverBadge, bronzeBadge],
            new Dictionary<int, List<Badge>> { { playerId, []} },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        var callOrder = new List<BadgeLevel?>();
        foreach (var badge in new[] { bronzeBadge, silverBadge })
        {
            _sessionsEvaluatorMock
                .Setup(x => x.CanAwardBadge(playerId, badge, session, playerSessions))
                .Callback(() => callOrder.Add(badge.Level))
                .ReturnsAsync(true);

            _badgeRepositoryMock
                .Setup(x => x.AwardBadgeToPlayer(playerId, badge.Id))
                .ReturnsAsync(true);
        }

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        callOrder.Should().Equal(BadgeLevel.Green, BadgeLevel.Blue);
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, bronzeBadge, session, playerSessions), Times.Once);
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, silverBadge, session, playerSessions), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, bronzeBadge.Id), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, silverBadge.Id), Times.Once);
        VerifyAwardRepositoryReads(playerId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldStopProcessingBadgeGroup_WhenEvaluatorReturnsFalse()
    {
        // Arrange
        var playerId = 1;
        var session = new SessionBuilder().WithWinner(playerId, 100).Build();
        var bronzeBadge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var silverBadge = Badge.CreateWithId(2, "badge.session.silver.title", "badge.session.silver.desc", BadgeType.Sessions, "session-silver.png", BadgeLevel.Blue);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            [bronzeBadge, silverBadge],
            new Dictionary<int, List<Badge>> { { playerId, []} },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, bronzeBadge, session, playerSessions))
            .ReturnsAsync(false);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, bronzeBadge, session, playerSessions), Times.Once);
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, silverBadge, session, playerSessions), Times.Never);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, bronzeBadge.Id), Times.Never);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, silverBadge.Id), Times.Never);
        VerifyAwardRepositoryReads(playerId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldSkipBadgeType_WhenNoEvaluatorExists()
    {
        // Arrange
        var playerId = 1;
        var session = new SessionBuilder().WithWinner(playerId, 100).Build();
        var badge = Badge.CreateWithId(1, "badge.unknown.title", "badge.unknown.desc", BadgeType.DifferentGames, "unknown.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            [badge],
            new Dictionary<int, List<Badge>> { { playerId, []} },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, badge.Id), Times.Never);
        VerifyAwardRepositoryReads(playerId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldProcessMultipleBadgeTypes()
    {
        // Arrange
        var playerId = 1;
        var session = new SessionBuilder().WithWinner(playerId, 100).Build();
        var sessionBadge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var winBadge = Badge.CreateWithId(2, "badge.win.bronze.title", "badge.win.bronze.desc", BadgeType.Wins, "win-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            [sessionBadge, winBadge],
            new Dictionary<int, List<Badge>> { { playerId, []} },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, sessionBadge, session, playerSessions))
            .ReturnsAsync(true);

        _winsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, winBadge, session, playerSessions))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBadgeToPlayer(playerId, sessionBadge.Id))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBadgeToPlayer(playerId, winBadge.Id))
            .ReturnsAsync(true);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, sessionBadge, session, playerSessions), Times.Once);
        _winsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, winBadge, session, playerSessions), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, sessionBadge.Id), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, winBadge.Id), Times.Once);
        VerifyAwardRepositoryReads(playerId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldProcessPlayerOnce_WhenSamePlayerIsAddedTwice()
    {
        var playerId = 1;
        var session = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Test session");
        session.AddPlayerSession(playerId, 100, false, true);
        session.AddPlayerSession(playerId, 100, false, true);

        var badge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            [badge],
            new Dictionary<int, List<Badge>> { { playerId, []} },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, badge, session, playerSessions))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBadgeToPlayer(playerId, badge.Id))
            .ReturnsAsync(true);

        // Act
        await _badgeService.AwardBadgesAsync(session);

        // Assert
        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, badge, session, playerSessions), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, badge.Id), Times.Once);
        VerifyAwardRepositoryReads(playerId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldStartFromTheFirstUnearnedLevel_WhenALowerLevelIsAlreadyEarned()
    {
        var playerId = 1;
        var session = new SessionBuilder().WithWinner(playerId, 100).Build();
        var bronzeBadge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var silverBadge = Badge.CreateWithId(2, "badge.session.silver.title", "badge.session.silver.desc", BadgeType.Sessions, "session-silver.png", BadgeLevel.Blue);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            [bronzeBadge, silverBadge],
            new Dictionary<int, List<Badge>> { { playerId, [bronzeBadge] } },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, silverBadge, session, playerSessions))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBadgeToPlayer(playerId, silverBadge.Id))
            .ReturnsAsync(true);

        await _badgeService.AwardBadgesAsync(session);

        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, silverBadge, session, playerSessions), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, silverBadge.Id), Times.Once);
        VerifyAwardRepositoryReads(playerId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldKeepEarnedBadgesPerPlayer()
    {
        var playerId1 = 1;
        var playerId2 = 2;
        var session = new SessionBuilder().WithPlayers(playerId1, playerId2).Build();
        var badge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositories(
            [playerId1, playerId2],
            [badge],
            new Dictionary<int, List<Badge>> { { playerId1, [badge] }, { playerId2, [] } },
            new Dictionary<int, List<Session>> { { playerId1, playerSessions }, { playerId2, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId2, badge, session, playerSessions))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBadgeToPlayer(playerId2, badge.Id))
            .ReturnsAsync(true);

        await _badgeService.AwardBadgesAsync(session);

        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId2, badge, session, playerSessions), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId2, badge.Id), Times.Once);
        VerifyAwardRepositoryReads(playerId1, playerId2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldStillEvaluateTheNextBadgeType_WhenAnEarlierTypeIsNotAwarded()
    {
        var playerId = 1;
        var session = new SessionBuilder().WithWinner(playerId, 100).Build();
        var sessionBadge = Badge.CreateWithId(1, "badge.session.bronze.title", "badge.session.bronze.desc", BadgeType.Sessions, "session-bronze.png", BadgeLevel.Green);
        var winBadge = Badge.CreateWithId(2, "badge.win.bronze.title", "badge.win.bronze.desc", BadgeType.Wins, "win-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            [sessionBadge, winBadge],
            new Dictionary<int, List<Badge>> { { playerId, [] } },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _sessionsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, sessionBadge, session, playerSessions))
            .ReturnsAsync(false);

        _winsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, winBadge, session, playerSessions))
            .ReturnsAsync(true);

        _badgeRepositoryMock
            .Setup(x => x.AwardBadgeToPlayer(playerId, winBadge.Id))
            .ReturnsAsync(true);

        await _badgeService.AwardBadgesAsync(session);

        _sessionsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, sessionBadge, session, playerSessions), Times.Once);
        _winsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, winBadge, session, playerSessions), Times.Once);
        _badgeRepositoryMock.Verify(x => x.AwardBadgeToPlayer(playerId, winBadge.Id), Times.Once);
        VerifyAwardRepositoryReads(playerId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AwardBadgesAsync_ShouldStillEvaluateTheNextBadgeType_WhenAnEarlierTypeHasNoEvaluator()
    {
        var playerId = 1;
        var session = new SessionBuilder().WithWinner(playerId, 100).Build();
        var unknownBadge = Badge.CreateWithId(1, "badge.unknown.title", "badge.unknown.desc", BadgeType.DifferentGames, "unknown.png", BadgeLevel.Green);
        var winBadge = Badge.CreateWithId(2, "badge.win.bronze.title", "badge.win.bronze.desc", BadgeType.Wins, "win-bronze.png", BadgeLevel.Green);
        var playerSessions = new List<Session> { session };

        SetupRepositoriesForAwardTest(
            playerId,
            [unknownBadge, winBadge],
            new Dictionary<int, List<Badge>> { { playerId, [] } },
            new Dictionary<int, List<Session>> { { playerId, playerSessions } });

        _winsEvaluatorMock
            .Setup(x => x.CanAwardBadge(playerId, winBadge, session, playerSessions))
            .ReturnsAsync(false);

        await _badgeService.AwardBadgesAsync(session);

        _winsEvaluatorMock.Verify(x => x.CanAwardBadge(playerId, winBadge, session, playerSessions), Times.Once);
        VerifyAwardRepositoryReads(playerId);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Helper Methods

    private void SetupRepositoriesForAwardTest(
        int playerId,
        List<Badge> allBadges,
        Dictionary<int, List<Badge>> playerBadgesMap,
        Dictionary<int, List<Session>> playerSessionsMap)
    {
        SetupRepositories([playerId], allBadges, playerBadgesMap, playerSessionsMap);
    }

    private void SetupRepositories(
        int[] playerIds,
        List<Badge> allBadges,
        Dictionary<int, List<Badge>> playerBadgesMap,
        Dictionary<int, List<Session>> playerSessionsMap)
    {
        _badgeRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(allBadges);

        _badgeRepositoryMock
            .Setup(x => x.GetPlayerBadgesBatchAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(playerIds))))
            .ReturnsAsync(playerBadgesMap);

        _sessionRepositoryMock
            .Setup(x => x.GetByPlayerBatchAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(playerIds))))
            .ReturnsAsync(playerSessionsMap);
    }

    #endregion
}
