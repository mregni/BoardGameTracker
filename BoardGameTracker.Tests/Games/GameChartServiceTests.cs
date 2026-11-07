using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Games.Specifications;
using BoardGameTracker.Core.Sessions.Specifications;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class GameChartServiceTests
{
    private const int GameId = 7;
    private static readonly Game ChartGame = new("Scythe") { Id = GameId };
    private static readonly Session ChartSession = new(GameId, new DateTime(2024, 1, 1, 18, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 19, 0, 0, DateTimeKind.Utc), string.Empty);

    private readonly CancellationToken _token = new CancellationTokenSource().Token;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IReadRepository<Session>> _sessionRepositoryMock;
    private readonly Mock<IGameStatisticsRepository> _gameStatisticsRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly GameChartService _gameChartService;

    public GameChartServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _sessionRepositoryMock = new Mock<IReadRepository<Session>>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _gameStatisticsRepositoryMock = new Mock<IGameStatisticsRepository>();

        _gameChartService = new GameChartService(
            _gameRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _gameStatisticsRepositoryMock.Object,
            _dateTimeProviderMock.Object,
            NullLogger<GameChartService>.Instance);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _gameStatisticsRepositoryMock.VerifyNoOtherCalls();
        _dateTimeProviderMock.VerifyNoOtherCalls();
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

    private static Session ScoredSession => CreateSessionWithPlayers(GameId, ChartSession.Start, (1, 10, true));

    private static Session SessionOn(DateTime start) => CreateSessionWithPlayers(GameId, start);

    private static PlayerSession CreatePlayerSession(int playerId, double? score, bool won)
    {
        return new PlayerSession(playerId, score, false, won);
    }

    #region GetPlayByDayChart Tests

    [Fact]
    public async Task GetPlayByDayChart_ShouldReturnAllDaysOfWeek_WithZeroCounts_WhenNoSessions()
    {
        var gameId = GameId;
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetSessionStartTimes(gameId, _token))
            .ReturnsAsync([]);

        var result = (await _gameChartService.GetPlayByDayChart(gameId, _token)).ToList();

        result.Should().HaveCount(7);
        result.Should().OnlyContain(x => x.PlayCount == 0);

        _gameStatisticsRepositoryMock.Verify(x => x.GetSessionStartTimes(gameId, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayByDayChart_ShouldMapCountsPerDay_StartingOnMonday()
    {
        var gameId = GameId;
        var monday = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var sunday = new DateTime(2024, 1, 7, 10, 0, 0, DateTimeKind.Utc);
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(monday)).Returns(monday);
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(monday.AddHours(3))).Returns(monday.AddHours(3));
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(sunday)).Returns(sunday);
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetSessionStartTimes(gameId, _token))
            .ReturnsAsync([monday, monday.AddHours(3), sunday]);

        var result = (await _gameChartService.GetPlayByDayChart(gameId, _token)).ToList();

        result.Should().HaveCount(7);
        result[0].DayOfWeek.Should().Be(DayOfWeek.Monday);
        result[0].PlayCount.Should().Be(2);
        result[6].DayOfWeek.Should().Be(DayOfWeek.Sunday);
        result[6].PlayCount.Should().Be(1);
        result.Skip(1).Take(5).Should().OnlyContain(x => x.PlayCount == 0);

        _gameStatisticsRepositoryMock.Verify(x => x.GetSessionStartTimes(gameId, _token), Times.Once);
        _dateTimeProviderMock.Verify(x => x.ConvertToLocalTime(monday), Times.Once);
        _dateTimeProviderMock.Verify(x => x.ConvertToLocalTime(monday.AddHours(3)), Times.Once);
        _dateTimeProviderMock.Verify(x => x.ConvertToLocalTime(sunday), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayByDayChart_ShouldCountOnTheLocalDay_WhenTheLocalDayDiffersFromUtc()
    {
        var gameId = GameId;
        var sundayNightUtc = new DateTime(2024, 1, 7, 23, 30, 0, DateTimeKind.Utc);
        var mondayLocal = new DateTime(2024, 1, 8, 0, 30, 0, DateTimeKind.Unspecified);
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(sundayNightUtc)).Returns(mondayLocal);
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetSessionStartTimes(gameId, _token))
            .ReturnsAsync([sundayNightUtc]);

        var result = (await _gameChartService.GetPlayByDayChart(gameId, _token)).ToList();

        result.Single(x => x.DayOfWeek == DayOfWeek.Monday).PlayCount.Should().Be(1);
        result.Single(x => x.DayOfWeek == DayOfWeek.Sunday).PlayCount.Should().Be(0);

        _gameStatisticsRepositoryMock.Verify(x => x.GetSessionStartTimes(gameId, _token), Times.Once);
        _dateTimeProviderMock.Verify(x => x.ConvertToLocalTime(sundayNightUtc), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetPlayerCountChart Tests

    [Fact]
    public async Task GetPlayerCountChart_ShouldReturnEmpty_WhenNoSessions()
    {
        var gameId = GameId;
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetPlayerCountChart(gameId, _token))
            .ReturnsAsync([]);

        var result = await _gameChartService.GetPlayerCountChart(gameId, _token);

        result.Should().BeEmpty();

        _gameStatisticsRepositoryMock.Verify(x => x.GetPlayerCountChart(gameId, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerCountChart_ShouldMapGroupingsToPlayerCounts()
    {
        var gameId = GameId;
        var groupings = new List<int> { 2, 2, 2, 4 }.GroupBy(x => x).ToList();

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetPlayerCountChart(gameId, _token))
            .ReturnsAsync(groupings);

        var result = (await _gameChartService.GetPlayerCountChart(gameId, _token)).ToList();

        result.Should().BeEquivalentTo([new PlayerCount { Players = 2, PlayCount = 3 }, new PlayerCount { Players = 4, PlayCount = 1 }]);

        _gameStatisticsRepositoryMock.Verify(x => x.GetPlayerCountChart(gameId, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetPlayerScoringChart Tests

    [Fact]
    public async Task GetPlayerScoringChart_ShouldReturnNull_WhenGameHasNoScoring()
    {
        var gameId = GameId;

        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.Is<GameHasScoringSpec>(s => s.IsSatisfiedBy(ChartGame)), _token))
            .ReturnsAsync((bool?)false);

        var result = await _gameChartService.GetPlayerScoringChart(gameId, _token);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.Is<GameHasScoringSpec>(s => s.IsSatisfiedBy(ChartGame)), _token),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerScoringChart_ShouldReturnNull_WhenGameDoesNotExist()
    {
        var gameId = GameId;

        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.Is<GameHasScoringSpec>(s => s.IsSatisfiedBy(ChartGame)), _token))
            .ReturnsAsync((bool?)null);

        var result = await _gameChartService.GetPlayerScoringChart(gameId, _token);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.Is<GameHasScoringSpec>(s => s.IsSatisfiedBy(ChartGame)), _token),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerScoringChart_ShouldBuildChartPerSession_WithNullScoresForAbsentPlayers()
    {
        var gameId = GameId;
        var now = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var cutoff = now.AddDays(-200);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

        var firstStart = now.AddDays(-10);
        var secondStart = now.AddDays(-5);
        var firstSession = CreateSessionWithPlayers(gameId, firstStart, (1, 50, true), (2, 30, false));
        var secondSession = CreateSessionWithPlayers(gameId, secondStart, (1, 70, true));

        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.Is<GameHasScoringSpec>(s => s.IsSatisfiedBy(ChartGame)), _token))
            .ReturnsAsync((bool?)true);

        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<SessionsByGameSinceSpec>(s => s.IsSatisfiedBy(SessionOn(cutoff.AddMinutes(1))) && !s.IsSatisfiedBy(SessionOn(cutoff))), _token))
            .ReturnsAsync([firstSession, secondSession]);

        var result = await _gameChartService.GetPlayerScoringChart(gameId, _token);

        result.Should().HaveCount(2);
        result![0].DateTime.Should().Be(firstStart);
        result[0].Series.Should().BeEquivalentTo([new XValue { Id = 1, Value = 50 }, new XValue { Id = 2, Value = 30 }]);
        result[1].DateTime.Should().Be(secondStart);
        result[1].Series.Should().BeEquivalentTo([new XValue { Id = 1, Value = 70 }, new XValue { Id = 2, Value = null }]);

        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.Is<GameHasScoringSpec>(s => s.IsSatisfiedBy(ChartGame)), _token),
            Times.Once);
        _sessionRepositoryMock.Verify(
            x => x.ListAsync(It.Is<SessionsByGameSinceSpec>(s => s.IsSatisfiedBy(SessionOn(cutoff.AddMinutes(1))) && !s.IsSatisfiedBy(SessionOn(cutoff))), _token),
            Times.Once);
        _dateTimeProviderMock.Verify(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerScoringChart_ShouldKeepBothSessions_WhenSessionsShareStartTime()
    {
        var gameId = GameId;
        var now = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var cutoff = now.AddDays(-200);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

        var start = now.AddDays(-3);
        var firstSession = CreateSessionWithPlayers(gameId, start, (1, 10, true));
        var duplicateStartSession = CreateSessionWithPlayers(gameId, start, (1, 99, true));

        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.Is<GameHasScoringSpec>(s => s.IsSatisfiedBy(ChartGame)), _token))
            .ReturnsAsync((bool?)true);

        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<SessionsByGameSinceSpec>(s => s.IsSatisfiedBy(SessionOn(cutoff.AddMinutes(1))) && !s.IsSatisfiedBy(SessionOn(cutoff))), _token))
            .ReturnsAsync([firstSession, duplicateStartSession]);

        var result = await _gameChartService.GetPlayerScoringChart(gameId, _token);

        result.Should().HaveCount(2).And.OnlyContain(x => x.DateTime == start);
        result!.Select(x => x.Series.Single().Value).Should().Equal(10, 99);

        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.Is<GameHasScoringSpec>(s => s.IsSatisfiedBy(ChartGame)), _token),
            Times.Once);
        _sessionRepositoryMock.Verify(
            x => x.ListAsync(It.Is<SessionsByGameSinceSpec>(s => s.IsSatisfiedBy(SessionOn(cutoff.AddMinutes(1))) && !s.IsSatisfiedBy(SessionOn(cutoff))), _token),
            Times.Once);
        _dateTimeProviderMock.Verify(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetTopPlayers Tests

    [Fact]
    public async Task GetTopPlayers_ShouldReturnEmptyList_WhenNoSessions()
    {
        var gameId = GameId;
        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<SessionsByGameSpec>(s => s.IsSatisfiedBy(ChartSession)), _token))
            .ReturnsAsync([]);

        var result = await _gameChartService.GetTopPlayers(gameId, _token);

        result.Should().BeEmpty();

        _sessionRepositoryMock.Verify(x => x.ListAsync(It.Is<SessionsByGameSpec>(s => s.IsSatisfiedBy(ChartSession)), _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTopPlayers_ShouldOrderByWins_AndLimitToFive()
    {
        var gameId = GameId;
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
            .Setup(x => x.ListAsync(It.Is<SessionsByGameSpec>(s => s.IsSatisfiedBy(ChartSession)), _token))
            .ReturnsAsync(sessions);

        var result = await _gameChartService.GetTopPlayers(gameId, _token);

        result.Select(x => x.PlayerId).Should().Equal(6, 5, 4, 3, 2);
        result.Select(x => x.Wins).Should().Equal(6, 5, 4, 3, 2);

        _sessionRepositoryMock.Verify(x => x.ListAsync(It.Is<SessionsByGameSpec>(s => s.IsSatisfiedBy(ChartSession)), _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTopPlayers_ShouldIncludePlayersWithoutWins_RankedByWinPercentageThenPlays()
    {
        var gameId = GameId;
        var baseDate = new DateTime(2024, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        var sessions = new List<Session>
        {
            CreateSessionWithPlayers(gameId, baseDate.AddDays(1), (1, null, true), (2, null, false), (3, null, false)),
            CreateSessionWithPlayers(gameId, baseDate.AddDays(2), (1, null, false), (2, null, true), (3, null, false)),
            CreateSessionWithPlayers(gameId, baseDate.AddDays(3), (1, null, true), (3, null, false)),
            CreateSessionWithPlayers(gameId, baseDate.AddDays(4), (4, null, false)),
        };

        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<SessionsByGameSpec>(s => s.IsSatisfiedBy(ChartSession)), _token))
            .ReturnsAsync(sessions);

        var result = await _gameChartService.GetTopPlayers(gameId, _token);

        result.Select(x => x.PlayerId).Should().Equal(1, 2, 3, 4);
        result.Select(x => x.Wins).Should().Equal(2, 1, 0, 0);
        result.Select(x => x.PlayCount).Should().Equal(3, 2, 3, 1);

        _sessionRepositoryMock.Verify(x => x.ListAsync(It.Is<SessionsByGameSpec>(s => s.IsSatisfiedBy(ChartSession)), _token), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetScoringRankedChart Tests

    [Fact]
    public async Task GetScoringRankedChart_ShouldReturnAllRanksInOrder_WhenAllStatisticsExist()
    {
        var gameId = GameId;
        SetupHasScoring(true);
        SetupScoredSessions(3);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetHighestScoringPlayer(gameId, _token))
            .ReturnsAsync(CreatePlayerSession(1, 100, true));

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetHighestLosingPlayer(gameId, _token))
            .ReturnsAsync(CreatePlayerSession(2, 80, false));

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetLowestWinning(gameId, _token))
            .ReturnsAsync(CreatePlayerSession(3, 10, true));

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetLowestScoringPlayer(gameId, _token))
            .ReturnsAsync(CreatePlayerSession(4, 5, false));

        var result = await _gameChartService.GetScoringRankedChart(gameId, 42.5, _token);

        result.Select(x => x.Key).Should().Equal("top-score", "highest-losing", "average", "lowest-winning", "lowest");
        result[0].Score.Should().Be(100);
        result[0].PlayerId.Should().Be(1);
        result[1].Score.Should().Be(80);
        result[1].PlayerId.Should().Be(2);
        result[2].Score.Should().Be(42.5);
        result[3].Score.Should().Be(10);
        result[3].PlayerId.Should().Be(3);
        result[4].Score.Should().Be(5);
        result[4].PlayerId.Should().Be(4);

        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestScoringPlayer(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestLosingPlayer(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetLowestWinning(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetLowestScoringPlayer(gameId, _token), Times.Once);
        VerifyHasScoringChecked();
        VerifyScoredSessionsCounted();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetScoringRankedChart_ShouldReturnEmptyList_WhenNoStatisticsExist()
    {
        var gameId = GameId;
        SetupHasScoring(true);
        SetupScoredSessions(3);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetHighestScoringPlayer(gameId, _token))
            .ReturnsAsync((PlayerSession?)null);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetHighestLosingPlayer(gameId, _token))
            .ReturnsAsync((PlayerSession?)null);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetLowestWinning(gameId, _token))
            .ReturnsAsync((PlayerSession?)null);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetLowestScoringPlayer(gameId, _token))
            .ReturnsAsync((PlayerSession?)null);

        var result = await _gameChartService.GetScoringRankedChart(gameId, null, _token);

        result.Should().BeEmpty();

        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestScoringPlayer(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestLosingPlayer(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetLowestWinning(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetLowestScoringPlayer(gameId, _token), Times.Once);
        VerifyHasScoringChecked();
        VerifyScoredSessionsCounted();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetScoringRankedChart_ShouldReturnEmptyList_WithoutQueryingScores_UntilThreeSessionsAreScored(int scored)
    {
        SetupHasScoring(true);
        SetupScoredSessions(scored);

        var result = await _gameChartService.GetScoringRankedChart(GameId, 69, _token);

        result.Should().BeEmpty();
        VerifyHasScoringChecked();
        VerifyScoredSessionsCounted();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task GetScoringRankedChart_ShouldReturnEmptyList_WithoutQueryingScores_WhenGameHasNoScoring(bool? hasScoring)
    {
        SetupHasScoring(hasScoring);

        var result = await _gameChartService.GetScoringRankedChart(GameId, 42.5, _token);

        result.Should().BeEmpty();
        VerifyHasScoringChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetScoringRankedChart_ShouldReturnOnlyTheRanksThatExist()
    {
        SetupHasScoring(true);
        SetupScoredSessions(3);
        _gameStatisticsRepositoryMock.Setup(x => x.GetHighestScoringPlayer(GameId, _token)).ReturnsAsync((PlayerSession?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetHighestLosingPlayer(GameId, _token)).ReturnsAsync((PlayerSession?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetLowestWinning(GameId, _token)).ReturnsAsync(CreatePlayerSession(3, 10, true));
        _gameStatisticsRepositoryMock.Setup(x => x.GetLowestScoringPlayer(GameId, _token)).ReturnsAsync((PlayerSession?)null);

        var result = await _gameChartService.GetScoringRankedChart(GameId, 42.5, _token);

        result.Select(x => x.Key).Should().Equal("average", "lowest-winning");

        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestScoringPlayer(GameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestLosingPlayer(GameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetLowestWinning(GameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetLowestScoringPlayer(GameId, _token), Times.Once);
        VerifyHasScoringChecked();
        VerifyScoredSessionsCounted();
        VerifyNoOtherCalls();
    }

    private void SetupHasScoring(bool? hasScoring)
    {
        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.Is<GameHasScoringSpec>(s => s.IsSatisfiedBy(ChartGame)), _token))
            .ReturnsAsync(hasScoring);
    }

    private void SetupScoredSessions(int count)
    {
        _sessionRepositoryMock
            .Setup(x => x.CountAsync(It.Is<ScoredSessionsByGameSpec>(s => s.IsSatisfiedBy(ScoredSession)), _token))
            .ReturnsAsync(count);
    }

    private void VerifyScoredSessionsCounted()
    {
        _sessionRepositoryMock.Verify(
            x => x.CountAsync(It.Is<ScoredSessionsByGameSpec>(s => s.IsSatisfiedBy(ScoredSession)), _token),
            Times.Once);
    }

    private void VerifyHasScoringChecked()
    {
        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.Is<GameHasScoringSpec>(s => s.IsSatisfiedBy(ChartGame)), _token),
            Times.Once);
    }

    #endregion
}
