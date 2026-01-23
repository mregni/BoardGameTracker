using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Dashboard;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IGameStatisticsRepository> _gameStatisticsRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameStatisticsRepositoryMock = new Mock<IGameStatisticsRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();

        _dashboardService = new DashboardService(
            _gameRepositoryMock.Object,
            _gameStatisticsRepositoryMock.Object,
            _playerRepositoryMock.Object,
            _sessionRepositoryMock.Object);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnAllCounts_WhenDataExists()
    {
        // Arrange
        SetupBasicStatistics(totalGames: 25, activePlayers: 10, sessionsPlayed: 100, expansionsOwned: 15);
        SetupPlayTimeStatistics(totalPlayedTime: 5000.0, avgSessionTime: 50.0);
        SetupPayedStatistics(avgGamePrice: 35.5, totalCollectionValue: 887.5);
        SetupListData();

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.TotalGames.Should().Be(25);
        result.ActivePlayers.Should().Be(10);
        result.SessionsPlayed.Should().Be(100);
        result.ExpansionsOwned.Should().Be(15);
        result.TotalPlayedTime.Should().Be(5000.0);
        result.AvgSessionTime.Should().Be(50.0);
        result.AvgGamePrice.Should().Be(35.5);
        result.TotalCollectionValue.Should().Be(887.5);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnZeroCounts_WhenNoDataExists()
    {
        // Arrange
        SetupBasicStatistics(totalGames: 0, activePlayers: 0, sessionsPlayed: 0, expansionsOwned: 0);
        SetupPlayTimeStatistics(totalPlayedTime: 0, avgSessionTime: 0);
        SetupPayedStatistics(avgGamePrice: null, totalCollectionValue: null);
        SetupListData();

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.TotalGames.Should().Be(0);
        result.ActivePlayers.Should().Be(0);
        result.SessionsPlayed.Should().Be(0);
        result.ExpansionsOwned.Should().Be(0);
        result.TotalPlayedTime.Should().Be(0);
        result.AvgSessionTime.Should().Be(0);
        result.AvgGamePrice.Should().BeNull();
        result.TotalCollectionValue.Should().BeNull();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnRecentActivities_WhenSessionsExist()
    {
        // Arrange
        SetupBasicStatistics(totalGames: 10, activePlayers: 5, sessionsPlayed: 50, expansionsOwned: 5);
        SetupPlayTimeStatistics(totalPlayedTime: 2500.0, avgSessionTime: 50.0);
        SetupPayedStatistics(avgGamePrice: 30.0, totalCollectionValue: 300.0);

        var game = new Game("Catan") { Id = 1 };
        var startTime = DateTime.Now.AddMinutes(-90);
        var endTime = DateTime.Now;
        var session = new Session(1, startTime, endTime, "Test session");
        typeof(Session).GetProperty("Game")!.SetValue(session, game);
        session.AddPlayerSession(1, null, false, true);

        // Set up the player on the PlayerSession for winner info
        var player = new Player("John Doe") { Id = 1 };
        var playerSession = session.PlayerSessions.First();
        typeof(Common.Entities.Helpers.PlayerSession).GetProperty("Player")!.SetValue(playerSession, player);

        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5)).ReturnsAsync(new List<Session> { session });
        SetupOtherListData();

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.RecentActivities.Should().HaveCount(1);
        result.RecentActivities[0].GameTitle.Should().Be("Catan");
        result.RecentActivities[0].PlayerCount.Should().Be(1);
        result.RecentActivities[0].DurationInMinutes.Should().BeApproximately(90, 1);
        result.RecentActivities[0].WinnerId.Should().Be(1);
        result.RecentActivities[0].WinnerName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnGameStateCollection_WhenGamesExist()
    {
        // Arrange
        SetupBasicStatistics(totalGames: 4, activePlayers: 5, sessionsPlayed: 50, expansionsOwned: 5);
        SetupPlayTimeStatistics(totalPlayedTime: 2500.0, avgSessionTime: 50.0);
        SetupPayedStatistics(avgGamePrice: 30.0, totalCollectionValue: 300.0);

        var ownedGames = new List<Game>
        {
            new("Game 1") { Id = 1 },
            new("Game 2") { Id = 2 },
            new("Game 3") { Id = 3 }
        };
        var wantedGames = new List<Game>
        {
            new("Game 4", state: GameState.Wanted) { Id = 4 }
        };

        var groupedGames = new List<IGrouping<GameState, Game>>
        {
            CreateGrouping(GameState.Owned, ownedGames),
            CreateGrouping(GameState.Wanted, wantedGames)
        };

        _gameStatisticsRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ReturnsAsync(groupedGames);
        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5)).ReturnsAsync(new List<Session>());
        SetupOtherListData(setupGameStates: false);

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.Collection.Should().HaveCount(2);
        result.Collection.Should().Contain(x => x.Type == GameState.Owned && x.GameCount == 3);
        result.Collection.Should().Contain(x => x.Type == GameState.Wanted && x.GameCount == 1);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnMostPlayedGames_WhenDataExists()
    {
        // Arrange
        SetupBasicStatistics(totalGames: 10, activePlayers: 5, sessionsPlayed: 50, expansionsOwned: 5);
        SetupPlayTimeStatistics(totalPlayedTime: 2500.0, avgSessionTime: 50.0);
        SetupPayedStatistics(avgGamePrice: 30.0, totalCollectionValue: 300.0);

        var mostPlayedGames = new List<(int GameId, string Title, string? Image, int PlayCount)>
        {
            (1, "Catan", "catan.jpg", 20),
            (2, "Ticket to Ride", "ttr.jpg", 15)
        };

        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGames(5)).ReturnsAsync(mostPlayedGames);
        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5)).ReturnsAsync(new List<Session>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ReturnsAsync(new List<IGrouping<GameState, Game>>());
        SetupOtherListData(setupMostPlayedGames: false);

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.MostPlayedGames.Should().HaveCount(2);
        result.MostPlayedGames[0].Title.Should().Be("Catan");
        result.MostPlayedGames[0].TotalSessions.Should().Be(20);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnTopPlayers_WhenDataExists()
    {
        // Arrange
        SetupBasicStatistics(totalGames: 10, activePlayers: 5, sessionsPlayed: 50, expansionsOwned: 5);
        SetupPlayTimeStatistics(totalPlayedTime: 2500.0, avgSessionTime: 50.0);
        SetupPayedStatistics(avgGamePrice: 30.0, totalCollectionValue: 300.0);

        var topPlayers = new List<(int Id, string Name, string? Image, int PlayCount, int WinCount)>
        {
            (1, "John", "john.jpg", 25, 15),
            (2, "Jane", "jane.jpg", 20, 12)
        };

        _playerRepositoryMock.Setup(x => x.GetTopPlayers(5)).ReturnsAsync(topPlayers);
        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5)).ReturnsAsync(new List<Session>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ReturnsAsync(new List<IGrouping<GameState, Game>>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGames(5)).ReturnsAsync(new List<(int, string, string?, int)>());
        SetupOtherListData(setupTopPlayers: false);

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.TopPlayers.Should().HaveCount(2);
        result.TopPlayers[0].Name.Should().Be("John");
        result.TopPlayers[0].PlayCount.Should().Be(25);
        result.TopPlayers[0].WinCount.Should().Be(15);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnRecentAddedGames_WhenDataExists()
    {
        // Arrange
        SetupBasicStatistics(totalGames: 10, activePlayers: 5, sessionsPlayed: 50, expansionsOwned: 5);
        SetupPlayTimeStatistics(totalPlayedTime: 2500.0, avgSessionTime: 50.0);
        SetupPayedStatistics(avgGamePrice: 30.0, totalCollectionValue: 300.0);

        var recentGames = new List<Game>
        {
            new("New Game 1") { Id = 1 },
            new("New Game 2") { Id = 2 }
        };
        recentGames[0].UpdateAdditionDate(DateTime.Now.AddDays(-1));
        recentGames[1].UpdateAdditionDate(DateTime.Now.AddDays(-2));

        _gameRepositoryMock.Setup(x => x.GetRecentlyAddedGames(5)).ReturnsAsync(recentGames);
        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5)).ReturnsAsync(new List<Session>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ReturnsAsync(new List<IGrouping<GameState, Game>>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGames(5)).ReturnsAsync(new List<(int, string, string?, int)>());
        _playerRepositoryMock.Setup(x => x.GetTopPlayers(5)).ReturnsAsync(new List<(int, string, string?, int, int)>());
        _sessionRepositoryMock.Setup(x => x.GetSessionsByDayOfWeek()).ReturnsAsync(new List<IGrouping<DayOfWeek, Session>>());

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.RecentAddedGames.Should().HaveCount(2);
        result.RecentAddedGames[0].Title.Should().Be("New Game 1");
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnSessionsByDayOfWeek_WhenDataExists()
    {
        // Arrange
        SetupBasicStatistics(totalGames: 10, activePlayers: 5, sessionsPlayed: 50, expansionsOwned: 5);
        SetupPlayTimeStatistics(totalPlayedTime: 2500.0, avgSessionTime: 50.0);
        SetupPayedStatistics(avgGamePrice: 30.0, totalCollectionValue: 300.0);

        var mondaySessions = new List<Session>
        {
            new(1, new DateTime(2026, 1, 13, 10, 0, 0), new DateTime(2026, 1, 13, 12, 0, 0), "Monday session 1"),
            new(1, new DateTime(2026, 1, 20, 10, 0, 0), new DateTime(2026, 1, 20, 12, 0, 0), "Monday session 2")
        };
        var fridaySessions = new List<Session>
        {
            new(1, new DateTime(2026, 1, 17, 10, 0, 0), new DateTime(2026, 1, 17, 12, 0, 0), "Friday session")
        };

        var sessionsByDay = new List<IGrouping<DayOfWeek, Session>>
        {
            CreateDayGrouping(DayOfWeek.Monday, mondaySessions),
            CreateDayGrouping(DayOfWeek.Friday, fridaySessions)
        };

        _sessionRepositoryMock.Setup(x => x.GetSessionsByDayOfWeek()).ReturnsAsync(sessionsByDay);
        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5)).ReturnsAsync(new List<Session>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ReturnsAsync(new List<IGrouping<GameState, Game>>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGames(5)).ReturnsAsync(new List<(int, string, string?, int)>());
        _playerRepositoryMock.Setup(x => x.GetTopPlayers(5)).ReturnsAsync(new List<(int, string, string?, int, int)>());
        _gameRepositoryMock.Setup(x => x.GetRecentlyAddedGames(5)).ReturnsAsync(new List<Game>());

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.SessionsByDayOfWeek.Should().HaveCount(2);
        result.SessionsByDayOfWeek.Should().Contain(x => x.DayOfWeek == DayOfWeek.Monday && x.PlayCount == 2);
        result.SessionsByDayOfWeek.Should().Contain(x => x.DayOfWeek == DayOfWeek.Friday && x.PlayCount == 1);
    }

    [Fact]
    public async Task GetStatistics_ShouldCallAllRepositories()
    {
        // Arrange
        SetupBasicStatistics(totalGames: 10, activePlayers: 5, sessionsPlayed: 50, expansionsOwned: 5);
        SetupPlayTimeStatistics(totalPlayedTime: 2500.0, avgSessionTime: 50.0);
        SetupPayedStatistics(avgGamePrice: 30.0, totalCollectionValue: 300.0);
        SetupListData();

        // Act
        await _dashboardService.GetStatistics();

        // Assert
        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalExpansionCount(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetRecentlyAddedGames(5), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMeanPayedAsync(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetTotalPayedAsync(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetGamesGroupedByState(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMostPlayedGames(5), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTopPlayers(5), Times.Once);
        _sessionRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetTotalPlayTime(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetMeanPlayTime(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetRecentSessions(5), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetSessionsByDayOfWeek(), Times.Once);
    }

    #region Helper Methods

    private void SetupBasicStatistics(int totalGames, int activePlayers, int sessionsPlayed, int expansionsOwned)
    {
        _gameRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(totalGames);
        _gameRepositoryMock.Setup(x => x.GetTotalExpansionCount()).ReturnsAsync(expansionsOwned);
        _playerRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(activePlayers);
        _sessionRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(sessionsPlayed);
    }

    private void SetupPlayTimeStatistics(double totalPlayedTime, double avgSessionTime)
    {
        _sessionRepositoryMock.Setup(x => x.GetTotalPlayTime()).ReturnsAsync(totalPlayedTime);
        _sessionRepositoryMock.Setup(x => x.GetMeanPlayTime()).ReturnsAsync(avgSessionTime);
    }

    private void SetupPayedStatistics(double? avgGamePrice, double? totalCollectionValue)
    {
        _gameStatisticsRepositoryMock.Setup(x => x.GetMeanPayedAsync()).ReturnsAsync(avgGamePrice);
        _gameStatisticsRepositoryMock.Setup(x => x.GetTotalPayedAsync()).ReturnsAsync(totalCollectionValue);
    }

    private void SetupListData()
    {
        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5)).ReturnsAsync(new List<Session>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ReturnsAsync(new List<IGrouping<GameState, Game>>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGames(5)).ReturnsAsync(new List<(int, string, string?, int)>());
        _playerRepositoryMock.Setup(x => x.GetTopPlayers(5)).ReturnsAsync(new List<(int, string, string?, int, int)>());
        _gameRepositoryMock.Setup(x => x.GetRecentlyAddedGames(5)).ReturnsAsync(new List<Game>());
        _sessionRepositoryMock.Setup(x => x.GetSessionsByDayOfWeek()).ReturnsAsync(new List<IGrouping<DayOfWeek, Session>>());
    }

    private void SetupOtherListData(bool setupMostPlayedGames = true, bool setupTopPlayers = true, bool setupGameStates = true)
    {
        if (setupGameStates)
            _gameStatisticsRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ReturnsAsync(new List<IGrouping<GameState, Game>>());
        if (setupMostPlayedGames)
            _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGames(5)).ReturnsAsync(new List<(int, string, string?, int)>());
        if (setupTopPlayers)
            _playerRepositoryMock.Setup(x => x.GetTopPlayers(5)).ReturnsAsync(new List<(int, string, string?, int, int)>());
        _gameRepositoryMock.Setup(x => x.GetRecentlyAddedGames(5)).ReturnsAsync(new List<Game>());
        _sessionRepositoryMock.Setup(x => x.GetSessionsByDayOfWeek()).ReturnsAsync(new List<IGrouping<DayOfWeek, Session>>());
    }

    private static IGrouping<GameState, Game> CreateGrouping(GameState key, List<Game> games)
    {
        return games.GroupBy(_ => key).First();
    }

    private static IGrouping<DayOfWeek, Session> CreateDayGrouping(DayOfWeek key, List<Session> sessions)
    {
        return sessions.GroupBy(_ => key).First();
    }

    #endregion
}
