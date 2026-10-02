using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Dashboard;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Dashboard;

public class DashboardServiceTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IGameStatisticsRepository> _gameStatisticsRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<ILogger<DashboardService>> _loggerMock;
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameStatisticsRepositoryMock = new Mock<IGameStatisticsRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(It.IsAny<DateTime>())).Returns((DateTime utc) => utc);
        _loggerMock = new Mock<ILogger<DashboardService>>();

        _sut = new DashboardService(
            _gameRepositoryMock.Object,
            _gameStatisticsRepositoryMock.Object,
            _playerRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _dateTimeProviderMock.Object,
            _loggerMock.Object);

        SetupDefaultMocks();
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _gameStatisticsRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
    }

    private void VerifyGetStatisticsCalls()
    {
        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetTotalPlayTime(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetMeanPlayTime(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetTotalPayedAsync(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMeanPayedAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalExpansionCount(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetRecentSessions(4), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetGameStateCounts(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMostPlayedGames(4), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTopPlayers(4), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetRecentlyAddedGames(4), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetSessionStartTimes(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnCorrectScalarValues()
    {
        _gameRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(25);
        _playerRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(10);
        _sessionRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(100);
        _sessionRepositoryMock.Setup(x => x.GetTotalPlayTime()).ReturnsAsync(5000.0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetTotalPayedAsync()).ReturnsAsync(887.5m);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMeanPayedAsync()).ReturnsAsync(35.5m);
        _gameRepositoryMock.Setup(x => x.GetTotalExpansionCount()).ReturnsAsync(15);
        _sessionRepositoryMock.Setup(x => x.GetMeanPlayTime()).ReturnsAsync(50.0);

        var result = await _sut.GetStatistics();

        result.TotalGames.Should().Be(25);
        result.ActivePlayers.Should().Be(10);
        result.SessionsPlayed.Should().Be(100);
        result.TotalPlayedTime.Should().Be(5000.0);
        result.TotalCollectionValue.Should().Be(887.5m);
        result.AvgGamePrice.Should().Be(35.5m);
        result.ExpansionsOwned.Should().Be(15);
        result.AvgSessionTime.Should().Be(50.0);

        VerifyGetStatisticsCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnNullForPrices_WhenNoGamesHavePrices()
    {
        var result = await _sut.GetStatistics();

        result.TotalCollectionValue.Should().BeNull();
        result.AvgGamePrice.Should().BeNull();

        VerifyGetStatisticsCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnRecentActivities()
    {
        var session = CreateSessionWithWinner(
            gameId: 1,
            gameTitle: "Catan",
            gameImage: "catan.jpg",
            winnerId: 1,
            winnerName: "John Doe",
            durationMinutes: 90);

        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(4))
            .ReturnsAsync([session]);

        var result = await _sut.GetStatistics();

        result.RecentActivities.Should().HaveCount(1);
        var activity = result.RecentActivities[0];
        activity.GameId.Should().Be(1);
        activity.GameTitle.Should().Be("Catan");
        activity.GameImage.Should().Be("catan.jpg");
        activity.PlayerCount.Should().Be(1);
        activity.DurationInMinutes.Should().BeApproximately(90, 1);
        activity.WinnerId.Should().Be(1);
        activity.WinnerName.Should().Be("John Doe");

        VerifyGetStatisticsCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnRecentActivityWithNullWinner_WhenNoWinnerExists()
    {
        var session = CreateSessionWithoutWinner(gameId: 1, gameTitle: "Catan");

        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(4))
            .ReturnsAsync([session]);

        var result = await _sut.GetStatistics();

        result.RecentActivities.Should().HaveCount(1);
        result.RecentActivities[0].WinnerId.Should().BeNull();
        result.RecentActivities[0].WinnerName.Should().BeNull();

        VerifyGetStatisticsCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnCollection()
    {
        var groupedGames = new List<GameStateChart>
        {
            new() { Type = GameState.Owned, GameCount = 3 },
            new() { Type = GameState.Wanted, GameCount = 2 }
        };

        _gameStatisticsRepositoryMock.Setup(x => x.GetGameStateCounts())
            .ReturnsAsync(groupedGames);

        var result = await _sut.GetStatistics();

        result.Collection.Should().HaveCount(2);
        result.Collection.Should().Contain(x => x.Type == GameState.Owned && x.GameCount == 3);
        result.Collection.Should().Contain(x => x.Type == GameState.Wanted && x.GameCount == 2);

        VerifyGetStatisticsCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnMostPlayedGames()
    {
        var mostPlayed = new List<(int GameId, string Title, string? Image, int PlayCount)>
        {
            (1, "Catan", "catan.jpg", 20),
            (2, "Ticket to Ride", "ttr.jpg", 15)
        };

        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGames(4))
            .ReturnsAsync(mostPlayed);

        var result = await _sut.GetStatistics();

        result.MostPlayedGames.Should().HaveCount(2);
        result.MostPlayedGames[0].Id.Should().Be(1);
        result.MostPlayedGames[0].Title.Should().Be("Catan");
        result.MostPlayedGames[0].Image.Should().Be("catan.jpg");
        result.MostPlayedGames[0].TotalSessions.Should().Be(20);

        VerifyGetStatisticsCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnTopPlayers()
    {
        var topPlayers = new List<(int Id, string Name, string? Image, int PlayCount, int WinCount)>
        {
            (1, "John", "john.jpg", 25, 15),
            (2, "Jane", null, 20, 12)
        };

        _playerRepositoryMock.Setup(x => x.GetTopPlayers(4))
            .ReturnsAsync(topPlayers);

        var result = await _sut.GetStatistics();

        result.TopPlayers.Should().HaveCount(2);
        result.TopPlayers[0].Id.Should().Be(1);
        result.TopPlayers[0].Name.Should().Be("John");
        result.TopPlayers[0].Image.Should().Be("john.jpg");
        result.TopPlayers[0].PlayCount.Should().Be(25);
        result.TopPlayers[0].WinCount.Should().Be(15);
        result.TopPlayers[1].Image.Should().BeNull();

        VerifyGetStatisticsCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnRecentAddedGames()
    {
        var game1 = new Game("New Game 1") { Id = 1 };
        game1.UpdateAdditionDate(DateTime.Now.AddDays(-1));
        game1.UpdateBuyingPrice(49.99m);

        var game2 = new Game("New Game 2") { Id = 2 };
        game2.UpdateAdditionDate(DateTime.Now.AddDays(-2));

        _gameRepositoryMock.Setup(x => x.GetRecentlyAddedGames(4))
            .ReturnsAsync([game1, game2]);

        var result = await _sut.GetStatistics();

        result.RecentAddedGames.Should().HaveCount(2);
        result.RecentAddedGames[0].Id.Should().Be(1);
        result.RecentAddedGames[0].Title.Should().Be("New Game 1");
        result.RecentAddedGames[0].AdditionDate.Should().NotBeNull();
        result.RecentAddedGames[0].Price.Should().Be(49.99m);
        result.RecentAddedGames[1].Price.Should().BeNull();

        VerifyGetStatisticsCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnSessionsByDayOfWeek()
    {
        var monday = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var friday = new DateTime(2024, 1, 5, 10, 0, 0, DateTimeKind.Utc);
        var startTimes = Enumerable.Repeat(monday, 5).Concat(Enumerable.Repeat(friday, 3)).ToList();

        _sessionRepositoryMock.Setup(x => x.GetSessionStartTimes())
            .ReturnsAsync(startTimes);

        var result = await _sut.GetStatistics();

        result.SessionsByDayOfWeek.Should().HaveCount(2);
        result.SessionsByDayOfWeek.Should().Contain(x => x.DayOfWeek == DayOfWeek.Monday && x.PlayCount == 5);
        result.SessionsByDayOfWeek.Should().Contain(x => x.DayOfWeek == DayOfWeek.Friday && x.PlayCount == 3);

        VerifyGetStatisticsCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldBucketSessionsByLocalDay()
    {
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(It.IsAny<DateTime>())).Returns((DateTime utc) => utc.AddHours(2));
        var lateSundayUtc = new DateTime(2024, 1, 7, 23, 0, 0, DateTimeKind.Utc);
        _sessionRepositoryMock.Setup(x => x.GetSessionStartTimes()).ReturnsAsync([lateSundayUtc]);

        var result = await _sut.GetStatistics();

        result.SessionsByDayOfWeek.Should().ContainSingle(x => x.DayOfWeek == DayOfWeek.Monday && x.PlayCount == 1);
    }


    #region Helper Methods

    private void SetupDefaultMocks()
    {
        _gameRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);
        _sessionRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);
        _sessionRepositoryMock.Setup(x => x.GetTotalPlayTime()).ReturnsAsync(0);
        _sessionRepositoryMock.Setup(x => x.GetMeanPlayTime()).ReturnsAsync(0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetTotalPayedAsync()).ReturnsAsync((decimal?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMeanPayedAsync()).ReturnsAsync((decimal?)null);
        _gameRepositoryMock.Setup(x => x.GetTotalExpansionCount()).ReturnsAsync(0);

        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(4)).ReturnsAsync([]);
        _gameStatisticsRepositoryMock.Setup(x => x.GetGameStateCounts()).ReturnsAsync([]);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGames(4)).ReturnsAsync(new List<(int, string, string?, int)>());
        _playerRepositoryMock.Setup(x => x.GetTopPlayers(4)).ReturnsAsync(new List<(int, string, string?, int, int)>());
        _gameRepositoryMock.Setup(x => x.GetRecentlyAddedGames(4)).ReturnsAsync([]);
        _sessionRepositoryMock.Setup(x => x.GetSessionStartTimes()).ReturnsAsync([]);
    }

    private static Session CreateSessionWithWinner(
        int gameId,
        string gameTitle,
        string? gameImage,
        int winnerId,
        string winnerName,
        int durationMinutes)
    {
        var game = new Game(gameTitle) { Id = gameId };
        game.UpdateImage(gameImage);

        return new SessionBuilder()
            .ForGame(game)
            .EndingAt(DateTime.Now)
            .LastingMinutes(durationMinutes)
            .WithWinner(new Player(winnerName) { Id = winnerId })
            .Build();
    }

    private static Session CreateSessionWithoutWinner(int gameId, string gameTitle)
    {
        return new SessionBuilder()
            .ForGame(new Game(gameTitle) { Id = gameId })
            .EndingAt(DateTime.Now)
            .Lasting(TimeSpan.FromHours(1))
            .WithPlayer(1)
            .Build();
    }

    #endregion
}
