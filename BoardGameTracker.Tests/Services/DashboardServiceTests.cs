using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
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
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameStatisticsRepositoryMock = new Mock<IGameStatisticsRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();

        _sut = new DashboardService(
            _gameRepositoryMock.Object,
            _gameStatisticsRepositoryMock.Object,
            _playerRepositoryMock.Object,
            _sessionRepositoryMock.Object);

        SetupDefaultMocks();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnCorrectScalarValues()
    {
        // Arrange
        _gameRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(25);
        _playerRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(10);
        _sessionRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(100);
        _sessionRepositoryMock.Setup(x => x.GetTotalPlayTime()).ReturnsAsync(5000.0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetTotalPayedAsync()).ReturnsAsync(887.5);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMeanPayedAsync()).ReturnsAsync(35.5);
        _gameRepositoryMock.Setup(x => x.GetTotalExpansionCount()).ReturnsAsync(15);
        _sessionRepositoryMock.Setup(x => x.GetMeanPlayTime()).ReturnsAsync(50.0);

        // Act
        var result = await _sut.GetStatistics();

        // Assert
        result.TotalGames.Should().Be(25);
        result.ActivePlayers.Should().Be(10);
        result.SessionsPlayed.Should().Be(100);
        result.TotalPlayedTime.Should().Be(5000.0);
        result.TotalCollectionValue.Should().Be(887.5);
        result.AvgGamePrice.Should().Be(35.5);
        result.ExpansionsOwned.Should().Be(15);
        result.AvgSessionTime.Should().Be(50.0);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnNullForPrices_WhenNoGamesHavePrices()
    {
        // Arrange
        _gameStatisticsRepositoryMock.Setup(x => x.GetTotalPayedAsync()).ReturnsAsync((double?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMeanPayedAsync()).ReturnsAsync((double?)null);

        // Act
        var result = await _sut.GetStatistics();

        // Assert
        result.TotalCollectionValue.Should().BeNull();
        result.AvgGamePrice.Should().BeNull();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnRecentActivities()
    {
        // Arrange
        var session = CreateSessionWithWinner(
            gameId: 1,
            gameTitle: "Catan",
            gameImage: "catan.jpg",
            winnerId: 1,
            winnerName: "John Doe",
            durationMinutes: 90);

        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(4))
            .ReturnsAsync(new List<Session> { session });

        // Act
        var result = await _sut.GetStatistics();

        // Assert
        result.RecentActivities.Should().HaveCount(1);
        var activity = result.RecentActivities[0];
        activity.GameId.Should().Be(1);
        activity.GameTitle.Should().Be("Catan");
        activity.GameImage.Should().Be("catan.jpg");
        activity.PlayerCount.Should().Be(1);
        activity.DurationInMinutes.Should().BeApproximately(90, 1);
        activity.WinnerId.Should().Be(1);
        activity.WinnerName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnRecentActivityWithNullWinner_WhenNoWinnerExists()
    {
        // Arrange
        var session = CreateSessionWithoutWinner(gameId: 1, gameTitle: "Catan");

        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(4))
            .ReturnsAsync(new List<Session> { session });

        // Act
        var result = await _sut.GetStatistics();

        // Assert
        result.RecentActivities.Should().HaveCount(1);
        result.RecentActivities[0].WinnerId.Should().BeNull();
        result.RecentActivities[0].WinnerName.Should().BeNull();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnCollection()
    {
        // Arrange
        var groupedGames = new List<IGrouping<GameState, Game>>
        {
            CreateGameStateGrouping(GameState.Owned, 3),
            CreateGameStateGrouping(GameState.Wanted, 2)
        };

        _gameStatisticsRepositoryMock.Setup(x => x.GetGamesGroupedByState())
            .ReturnsAsync(groupedGames);

        // Act
        var result = await _sut.GetStatistics();

        // Assert
        result.Collection.Should().HaveCount(2);
        result.Collection.Should().Contain(x => x.Type == GameState.Owned && x.GameCount == 3);
        result.Collection.Should().Contain(x => x.Type == GameState.Wanted && x.GameCount == 2);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnMostPlayedGames()
    {
        // Arrange
        var mostPlayed = new List<(int GameId, string Title, string? Image, int PlayCount)>
        {
            (1, "Catan", "catan.jpg", 20),
            (2, "Ticket to Ride", "ttr.jpg", 15)
        };

        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGames(4))
            .ReturnsAsync(mostPlayed);

        // Act
        var result = await _sut.GetStatistics();

        // Assert
        result.MostPlayedGames.Should().HaveCount(2);
        result.MostPlayedGames[0].Id.Should().Be(1);
        result.MostPlayedGames[0].Title.Should().Be("Catan");
        result.MostPlayedGames[0].Image.Should().Be("catan.jpg");
        result.MostPlayedGames[0].TotalSessions.Should().Be(20);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnTopPlayers()
    {
        // Arrange
        var topPlayers = new List<(int Id, string Name, string? Image, int PlayCount, int WinCount)>
        {
            (1, "John", "john.jpg", 25, 15),
            (2, "Jane", null, 20, 12)
        };

        _playerRepositoryMock.Setup(x => x.GetTopPlayers(4))
            .ReturnsAsync(topPlayers);

        // Act
        var result = await _sut.GetStatistics();

        // Assert
        result.TopPlayers.Should().HaveCount(2);
        result.TopPlayers[0].Id.Should().Be(1);
        result.TopPlayers[0].Name.Should().Be("John");
        result.TopPlayers[0].Image.Should().Be("john.jpg");
        result.TopPlayers[0].PlayCount.Should().Be(25);
        result.TopPlayers[0].WinCount.Should().Be(15);
        result.TopPlayers[1].Image.Should().BeNull();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnRecentAddedGames()
    {
        // Arrange
        var game1 = new Game("New Game 1") { Id = 1 };
        game1.UpdateAdditionDate(DateTime.Now.AddDays(-1));
        game1.UpdateBuyingPrice(49.99m);

        var game2 = new Game("New Game 2") { Id = 2 };
        game2.UpdateAdditionDate(DateTime.Now.AddDays(-2));

        _gameRepositoryMock.Setup(x => x.GetRecentlyAddedGames(4))
            .ReturnsAsync(new List<Game> { game1, game2 });

        // Act
        var result = await _sut.GetStatistics();

        // Assert
        result.RecentAddedGames.Should().HaveCount(2);
        result.RecentAddedGames[0].Id.Should().Be(1);
        result.RecentAddedGames[0].Title.Should().Be("New Game 1");
        result.RecentAddedGames[0].AdditionDate.Should().NotBeNull();
        result.RecentAddedGames[0].Price.Should().Be(49.99m);
        result.RecentAddedGames[1].Price.Should().BeNull();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnSessionsByDayOfWeek()
    {
        // Arrange
        var sessionsByDay = new List<IGrouping<DayOfWeek, Session>>
        {
            CreateDayOfWeekGrouping(DayOfWeek.Monday, 5),
            CreateDayOfWeekGrouping(DayOfWeek.Friday, 3)
        };

        _sessionRepositoryMock.Setup(x => x.GetSessionsByDayOfWeek())
            .ReturnsAsync(sessionsByDay);

        // Act
        var result = await _sut.GetStatistics();

        // Assert
        result.SessionsByDayOfWeek.Should().HaveCount(2);
        result.SessionsByDayOfWeek.Should().Contain(x => x.DayOfWeek == DayOfWeek.Monday && x.PlayCount == 5);
        result.SessionsByDayOfWeek.Should().Contain(x => x.DayOfWeek == DayOfWeek.Friday && x.PlayCount == 3);
    }

    [Fact]
    public async Task GetStatistics_ShouldCallAllRepositoryMethods()
    {
        // Act
        await _sut.GetStatistics();

        // Assert - Scalar values
        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetTotalPlayTime(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetMeanPlayTime(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetTotalPayedAsync(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMeanPayedAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalExpansionCount(), Times.Once);

        // Assert - List data
        _sessionRepositoryMock.Verify(x => x.GetRecentSessions(4), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetGamesGroupedByState(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMostPlayedGames(4), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTopPlayers(4), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetRecentlyAddedGames(4), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetSessionsByDayOfWeek(), Times.Once);
    }

    #region Helper Methods

    private void SetupDefaultMocks()
    {
        // Scalar values
        _gameRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);
        _sessionRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(0);
        _sessionRepositoryMock.Setup(x => x.GetTotalPlayTime()).ReturnsAsync(0);
        _sessionRepositoryMock.Setup(x => x.GetMeanPlayTime()).ReturnsAsync(0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetTotalPayedAsync()).ReturnsAsync((double?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMeanPayedAsync()).ReturnsAsync((double?)null);
        _gameRepositoryMock.Setup(x => x.GetTotalExpansionCount()).ReturnsAsync(0);

        // List data
        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(4)).ReturnsAsync(new List<Session>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ReturnsAsync(new List<IGrouping<GameState, Game>>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGames(4)).ReturnsAsync(new List<(int, string, string?, int)>());
        _playerRepositoryMock.Setup(x => x.GetTopPlayers(4)).ReturnsAsync(new List<(int, string, string?, int, int)>());
        _gameRepositoryMock.Setup(x => x.GetRecentlyAddedGames(4)).ReturnsAsync(new List<Game>());
        _sessionRepositoryMock.Setup(x => x.GetSessionsByDayOfWeek()).ReturnsAsync(new List<IGrouping<DayOfWeek, Session>>());
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

        var startTime = DateTime.Now.AddMinutes(-durationMinutes);
        var endTime = DateTime.Now;
        var session = new Session(gameId, startTime, endTime, "Test session");

        typeof(Session).GetProperty("Game")!.SetValue(session, game);

        session.AddPlayerSession(winnerId, null, false, true);

        var player = new Player(winnerName) { Id = winnerId };
        var playerSession = session.PlayerSessions.First();
        typeof(PlayerSession).GetProperty("Player")!.SetValue(playerSession, player);

        return session;
    }

    private static Session CreateSessionWithoutWinner(int gameId, string gameTitle)
    {
        var game = new Game(gameTitle) { Id = gameId };
        var session = new Session(gameId, DateTime.Now.AddHours(-1), DateTime.Now, "Test session");

        typeof(Session).GetProperty("Game")!.SetValue(session, game);

        session.AddPlayerSession(1, null, false, false);

        return session;
    }

    private static IGrouping<GameState, Game> CreateGameStateGrouping(GameState state, int count)
    {
        var games = Enumerable.Range(1, count)
            .Select(i => new Game($"Game {i}", state: state) { Id = i })
            .ToList();

        return games.GroupBy(_ => state).First();
    }

    private static IGrouping<DayOfWeek, Session> CreateDayOfWeekGrouping(DayOfWeek day, int count)
    {
        var sessions = Enumerable.Range(1, count)
            .Select(i => new Session(1, DateTime.Now.AddHours(-i), DateTime.Now, $"Session {i}"))
            .ToList();

        return sessions.GroupBy(_ => day).First();
    }

    #endregion
}
