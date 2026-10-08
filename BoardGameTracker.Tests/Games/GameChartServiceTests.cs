using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Games.Specifications;
using BoardGameTracker.Core.Sessions.Specifications;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class GameChartServiceTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IReadRepository<Session>> _sessionRepositoryMock;
    private readonly Mock<IGameStatisticsRepository> _gameStatisticsRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<ILogger<GameChartService>> _loggerMock;
    private readonly GameChartService _gameChartService;

    public GameChartServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _sessionRepositoryMock = new Mock<IReadRepository<Session>>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _gameStatisticsRepositoryMock = new Mock<IGameStatisticsRepository>();
        _loggerMock = new Mock<ILogger<GameChartService>>();

        _gameChartService = new GameChartService(
            _gameRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _gameStatisticsRepositoryMock.Object,
            _dateTimeProviderMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _gameStatisticsRepositoryMock.VerifyNoOtherCalls();
    }

    private static Session CreateSessionWithPlayers(int gameId, DateTime start, params (int PlayerId, double? Score, bool Won)[] players)
    {
        var builder = new SessionBuilder().ForGame(gameId).Between(start, start.AddHours(1));
        foreach (var (playerId, score, won) in players)
        {
            builder.WithPlayer(playerId, score, won);
        }

        return builder.Build();
    }

    private static PlayerSession CreatePlayerSession(int playerId, double? score, bool won)
    {
        return new PlayerSession(playerId, score, false, won);
    }

    #region GetPlayByDayChart Tests

    [Fact]
    public async Task GetPlayByDayChart_ShouldReturnAllDaysOfWeek_WithZeroCounts_WhenNoSessions()
    {
        var gameId = 1;
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetSessionStartTimes(gameId))
            .ReturnsAsync([]);

        var result = (await _gameChartService.GetPlayByDayChart(gameId)).ToList();

        result.Should().HaveCount(7);
        result.Should().OnlyContain(x => x.PlayCount == 0);

        _gameStatisticsRepositoryMock.Verify(x => x.GetSessionStartTimes(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayByDayChart_ShouldMapCountsPerDay_StartingOnMonday()
    {
        var gameId = 1;
        var monday = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var sunday = new DateTime(2024, 1, 7, 10, 0, 0, DateTimeKind.Utc);
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(It.IsAny<DateTime>())).Returns((DateTime utc) => utc);
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetSessionStartTimes(gameId))
            .ReturnsAsync([monday, monday.AddHours(3), sunday]);

        var result = (await _gameChartService.GetPlayByDayChart(gameId)).ToList();

        result.Should().HaveCount(7);
        result[0].DayOfWeek.Should().Be(DayOfWeek.Monday);
        result[0].PlayCount.Should().Be(2);
        result[6].DayOfWeek.Should().Be(DayOfWeek.Sunday);
        result[6].PlayCount.Should().Be(1);
        result.Skip(1).Take(5).Should().OnlyContain(x => x.PlayCount == 0);

        _gameStatisticsRepositoryMock.Verify(x => x.GetSessionStartTimes(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetPlayerCountChart Tests

    [Fact]
    public async Task GetPlayerCountChart_ShouldReturnEmpty_WhenNoSessions()
    {
        var gameId = 1;
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetPlayerCountChart(gameId))
            .ReturnsAsync([]);

        var result = await _gameChartService.GetPlayerCountChart(gameId);

        result.Should().BeEmpty();

        _gameStatisticsRepositoryMock.Verify(x => x.GetPlayerCountChart(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerCountChart_ShouldMapGroupingsToPlayerCounts()
    {
        var gameId = 1;
        var groupings = new List<int> { 2, 2, 2, 4 }.GroupBy(x => x).ToList();

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetPlayerCountChart(gameId))
            .ReturnsAsync(groupings);

        var result = (await _gameChartService.GetPlayerCountChart(gameId)).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(x => x.Players == 2 && x.PlayCount == 3);
        result.Should().Contain(x => x.Players == 4 && x.PlayCount == 1);

        _gameStatisticsRepositoryMock.Verify(x => x.GetPlayerCountChart(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetPlayerScoringChart Tests

    [Fact]
    public async Task GetPlayerScoringChart_ShouldReturnNull_WhenGameHasNoScoring()
    {
        var gameId = 1;

        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)false);

        var result = await _gameChartService.GetPlayerScoringChart(gameId);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerScoringChart_ShouldReturnNull_WhenGameDoesNotExist()
    {
        var gameId = 999;

        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)null);

        var result = await _gameChartService.GetPlayerScoringChart(gameId);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerScoringChart_ShouldBuildChartPerSession_WithNullScoresForAbsentPlayers()
    {
        var gameId = 1;
        var now = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

        var firstStart = now.AddDays(-10);
        var secondStart = now.AddDays(-5);
        var firstSession = CreateSessionWithPlayers(gameId, firstStart, (1, 50, true), (2, 30, false));
        var secondSession = CreateSessionWithPlayers(gameId, secondStart, (1, 70, true));

        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)true);

        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSinceSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync([firstSession, secondSession]);

        var result = await _gameChartService.GetPlayerScoringChart(gameId);

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        var first = result.Single(x => x.DateTime == firstStart).Series;
        first.Should().HaveCount(2);
        first.Should().Contain(x => x.Id == 1 && x.Value == 50);
        first.Should().Contain(x => x.Id == 2 && x.Value == 30);
        var second = result.Single(x => x.DateTime == secondStart).Series;
        second.Should().HaveCount(2);
        second.Should().Contain(x => x.Id == 1 && x.Value == 70);
        second.Should().Contain(x => x.Id == 2 && x.Value == null);

        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _sessionRepositoryMock.Verify(
            x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSinceSpec), It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerScoringChart_ShouldKeepBothSessions_WhenSessionsShareStartTime()
    {
        var gameId = 1;
        var now = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

        var start = now.AddDays(-3);
        var firstSession = CreateSessionWithPlayers(gameId, start, (1, 10, true));
        var duplicateStartSession = CreateSessionWithPlayers(gameId, start, (1, 99, true));

        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)true);

        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSinceSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync([firstSession, duplicateStartSession]);

        var result = await _gameChartService.GetPlayerScoringChart(gameId);

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().OnlyContain(x => x.DateTime == start);
        result.Select(x => x.Series.Single().Value).Should().BeEquivalentTo(new double?[] { 10, 99 });

        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _sessionRepositoryMock.Verify(
            x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSinceSpec), It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetTopPlayers Tests

    [Fact]
    public async Task GetTopPlayers_ShouldReturnEmptyList_WhenNoSessions()
    {
        var gameId = 1;
        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _gameChartService.GetTopPlayers(gameId);

        result.Should().BeEmpty();

        _sessionRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTopPlayers_ShouldOrderByWins_AndLimitToFive()
    {
        var gameId = 1;
        var baseDate = new DateTime(2024, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        var sessions = new List<Session>();
        for (var sessionIndex = 1; sessionIndex <= 6; sessionIndex++)
        {
            var players = Enumerable.Range(sessionIndex, 6 - sessionIndex + 1)
                .Select(playerId => (playerId, (double?)null, true))
                .ToArray();
            sessions.Add(CreateSessionWithPlayers(gameId, baseDate.AddDays(sessionIndex), players));
        }

        sessions.Add(CreateSessionWithPlayers(gameId, baseDate.AddDays(10), (7, null, false)));

        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var result = await _gameChartService.GetTopPlayers(gameId);

        result.Should().HaveCount(5);
        result.Select(x => x.PlayerId).Should().ContainInOrder(6, 5, 4, 3, 2);
        result.Select(x => x.Wins).Should().ContainInOrder(6, 5, 4, 3, 2);
        result.Should().NotContain(x => x.PlayerId == 1);
        result.Should().NotContain(x => x.PlayerId == 7);

        _sessionRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTopPlayers_ShouldIncludePlayersWithoutWins_RankedByWinPercentageThenPlays()
    {
        var gameId = 1;
        var baseDate = new DateTime(2024, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        var sessions = new List<Session>
        {
            CreateSessionWithPlayers(gameId, baseDate.AddDays(1), (1, null, true), (2, null, false), (3, null, false)),
            CreateSessionWithPlayers(gameId, baseDate.AddDays(2), (1, null, false), (2, null, true), (3, null, false)),
            CreateSessionWithPlayers(gameId, baseDate.AddDays(3), (1, null, true), (3, null, false)),
            CreateSessionWithPlayers(gameId, baseDate.AddDays(4), (4, null, false)),
        };

        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var result = await _gameChartService.GetTopPlayers(gameId);

        result.Select(x => x.PlayerId).Should().ContainInOrder(1, 2, 3, 4);
        result.Select(x => x.Wins).Should().ContainInOrder(2, 1, 0, 0);
        result.Select(x => x.PlayCount).Should().ContainInOrder(3, 2, 3, 1);

        _sessionRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetScoringRankedChart Tests

    [Fact]
    public async Task GetScoringRankedChart_ShouldReturnAllRanksInOrder_WhenAllStatisticsExist()
    {
        var gameId = 1;
        SetupHasScoring(true);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetHighestScoringPlayer(gameId))
            .ReturnsAsync(CreatePlayerSession(1, 100, true));

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetHighestLosingPlayer(gameId))
            .ReturnsAsync(CreatePlayerSession(2, 80, false));

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetLowestWinning(gameId))
            .ReturnsAsync(CreatePlayerSession(3, 10, true));

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetLowestScoringPlayer(gameId))
            .ReturnsAsync(CreatePlayerSession(4, 5, false));

        var result = await _gameChartService.GetScoringRankedChart(gameId, 42.5);

        result.Should().HaveCount(5);
        result.Select(x => x.Key).Should().ContainInOrder("top-score", "highest-losing", "average", "lowest-winning", "lowest");
        result[0].Score.Should().Be(100);
        result[0].PlayerId.Should().Be(1);
        result[1].Score.Should().Be(80);
        result[1].PlayerId.Should().Be(2);
        result[2].Score.Should().Be(42.5);
        result[3].Score.Should().Be(10);
        result[3].PlayerId.Should().Be(3);
        result[4].Score.Should().Be(5);
        result[4].PlayerId.Should().Be(4);

        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestScoringPlayer(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestLosingPlayer(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetLowestWinning(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetLowestScoringPlayer(gameId), Times.Once);
        VerifyHasScoringChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetScoringRankedChart_ShouldReturnEmptyList_WhenNoStatisticsExist()
    {
        var gameId = 1;
        SetupHasScoring(true);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetHighestScoringPlayer(gameId))
            .ReturnsAsync((PlayerSession?)null);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetHighestLosingPlayer(gameId))
            .ReturnsAsync((PlayerSession?)null);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetLowestWinning(gameId))
            .ReturnsAsync((PlayerSession?)null);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetLowestScoringPlayer(gameId))
            .ReturnsAsync((PlayerSession?)null);

        var result = await _gameChartService.GetScoringRankedChart(gameId, null);

        result.Should().BeEmpty();

        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestScoringPlayer(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestLosingPlayer(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetLowestWinning(gameId), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetLowestScoringPlayer(gameId), Times.Once);
        VerifyHasScoringChecked();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task GetScoringRankedChart_ShouldReturnEmptyList_WithoutQueryingScores_WhenGameHasNoScoring(bool? hasScoring)
    {
        SetupHasScoring(hasScoring);

        var result = await _gameChartService.GetScoringRankedChart(1, 42.5);

        result.Should().BeEmpty();
        VerifyHasScoringChecked();
        VerifyNoOtherCalls();
    }

    private void SetupHasScoring(bool? hasScoring)
    {
        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasScoring);
    }

    private void VerifyHasScoringChecked()
    {
        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
