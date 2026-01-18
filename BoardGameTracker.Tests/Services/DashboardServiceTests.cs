using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Dashboard;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
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
    private readonly Mock<ILocationRepository> _locationRepositoryMock;
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameStatisticsRepositoryMock = new Mock<IGameStatisticsRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _locationRepositoryMock = new Mock<ILocationRepository>();

        _dashboardService = new DashboardService(
            _gameRepositoryMock.Object,
            _gameStatisticsRepositoryMock.Object,
            _playerRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _locationRepositoryMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _gameStatisticsRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _locationRepositoryMock.VerifyNoOtherCalls();
    }

    #region GetStatistics Tests

    [Fact]
    public async Task GetStatistics_ShouldReturnAllCounts_WhenDataExists()
    {
        // Arrange
        SetupBasicStatistics(gameCount: 25, playerCount: 10, sessionCount: 100, locationCount: 5, expansionCount: 15);
        SetupPlayTimeStatistics(totalPlayTime: 5000.0, meanPlayTime: 50.0);
        SetupPayedStatistics(meanPayed: 35.5, totalPayed: 887.5);
        SetupMostWinningPlayer(null);

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.GameCount.Should().Be(25);
        result.PlayerCount.Should().Be(10);
        result.SessionCount.Should().Be(100);
        result.LocationCount.Should().Be(5);
        result.ExpansionCount.Should().Be(15);
        result.TotalPlayTime.Should().Be(5000.0);
        result.MeanPlayTime.Should().Be(50.0);
        result.MeanPayed.Should().Be(35.5);
        result.TotalCost.Should().Be(887.5);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnZeroCounts_WhenNoDataExists()
    {
        // Arrange
        SetupBasicStatistics(gameCount: 0, playerCount: 0, sessionCount: 0, locationCount: 0, expansionCount: 0);
        SetupPlayTimeStatistics(totalPlayTime: 0, meanPlayTime: 0);
        SetupPayedStatistics(meanPayed: null, totalPayed: null);
        SetupMostWinningPlayer(null);

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.GameCount.Should().Be(0);
        result.PlayerCount.Should().Be(0);
        result.SessionCount.Should().Be(0);
        result.LocationCount.Should().Be(0);
        result.ExpansionCount.Should().Be(0);
        result.TotalPlayTime.Should().Be(0);
        result.MeanPlayTime.Should().Be(0);
        result.MeanPayed.Should().BeNull();
        result.TotalCost.Should().BeNull();
    }

    [Fact]
    public async Task GetStatistics_ShouldIncludeMostWinningPlayer_WhenPlayerExists()
    {
        // Arrange
        var winningPlayer = new Player("John Doe", "john.png") { Id = 1 };
        var expectedWins = 25;

        SetupBasicStatistics(gameCount: 10, playerCount: 5, sessionCount: 50, locationCount: 2, expansionCount: 5);
        SetupPlayTimeStatistics(totalPlayTime: 2500.0, meanPlayTime: 50.0);
        SetupPayedStatistics(meanPayed: 30.0, totalPayed: 300.0);
        SetupMostWinningPlayer(winningPlayer);

        _playerRepositoryMock
            .Setup(x => x.GetTotalWinCount(winningPlayer.Id))
            .ReturnsAsync(expectedWins);

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.MostWinningPlayer.Should().NotBeNull();
        result.MostWinningPlayer!.Id.Should().Be(1);
        result.MostWinningPlayer.Name.Should().Be("John Doe");
        result.MostWinningPlayer.Image.Should().Be("john.png");
        result.MostWinningPlayer.TotalWins.Should().Be(25);

        _playerRepositoryMock.Verify(x => x.GetTotalWinCount(winningPlayer.Id), Times.Once);
    }

    [Fact]
    public async Task GetStatistics_ShouldHaveNullMostWinningPlayer_WhenNoWinnerExists()
    {
        // Arrange
        SetupBasicStatistics(gameCount: 10, playerCount: 5, sessionCount: 0, locationCount: 2, expansionCount: 5);
        SetupPlayTimeStatistics(totalPlayTime: 0, meanPlayTime: 0);
        SetupPayedStatistics(meanPayed: null, totalPayed: null);
        SetupMostWinningPlayer(null);

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.MostWinningPlayer.Should().BeNull();
    }

    [Fact]
    public async Task GetStatistics_ShouldHandlePlayerWithNullImage()
    {
        // Arrange
        var winningPlayer = new Player("Jane Doe") { Id = 2 }; // No image
        var expectedWins = 15;

        SetupBasicStatistics(gameCount: 10, playerCount: 5, sessionCount: 50, locationCount: 2, expansionCount: 5);
        SetupPlayTimeStatistics(totalPlayTime: 2500.0, meanPlayTime: 50.0);
        SetupPayedStatistics(meanPayed: 30.0, totalPayed: 300.0);
        SetupMostWinningPlayer(winningPlayer);

        _playerRepositoryMock
            .Setup(x => x.GetTotalWinCount(winningPlayer.Id))
            .ReturnsAsync(expectedWins);

        // Act
        var result = await _dashboardService.GetStatistics();

        // Assert
        result.MostWinningPlayer.Should().NotBeNull();
        result.MostWinningPlayer!.Image.Should().Be(string.Empty);
    }

    [Fact]
    public async Task GetStatistics_ShouldCallAllRepositories()
    {
        // Arrange
        SetupBasicStatistics(gameCount: 10, playerCount: 5, sessionCount: 50, locationCount: 2, expansionCount: 5);
        SetupPlayTimeStatistics(totalPlayTime: 2500.0, meanPlayTime: 50.0);
        SetupPayedStatistics(meanPayed: 30.0, totalPayed: 300.0);
        SetupMostWinningPlayer(null);

        // Act
        await _dashboardService.GetStatistics();

        // Assert
        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalExpansionCount(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMeanPayedAsync(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetTotalPayedAsync(), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMostWins(), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetTotalPlayTime(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetMeanPlayTime(), Times.Once);
        _locationRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
    }

    #endregion

    #region GetCharts Tests

    [Fact]
    public async Task GetCharts_ShouldReturnGameStateChart_WhenGamesExist()
    {
        // Arrange
        var ownedGames = new List<Game>
        {
            new Game("Game 1") { Id = 1 },
            new Game("Game 2") { Id = 2 },
            new Game("Game 3") { Id = 3 }
        };
        var wantedGames = new List<Game>
        {
            new Game("Game 4", state: GameState.Wanted) { Id = 4 }
        };

        var groupedGames = new List<IGrouping<GameState, Game>>
        {
            CreateGrouping(GameState.Owned, ownedGames),
            CreateGrouping(GameState.Wanted, wantedGames)
        };

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetGamesGroupedByState())
            .ReturnsAsync(groupedGames);

        // Act
        var result = await _dashboardService.GetCharts();

        // Assert
        result.GameState.Should().HaveCount(2);
        result.GameState.Should().Contain(x => x.Type == GameState.Owned && x.GameCount == 3);
        result.GameState.Should().Contain(x => x.Type == GameState.Wanted && x.GameCount == 1);

        _gameStatisticsRepositoryMock.Verify(x => x.GetGamesGroupedByState(), Times.Once);
    }

    [Fact]
    public async Task GetCharts_ShouldReturnEmptyChart_WhenNoGamesExist()
    {
        // Arrange
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetGamesGroupedByState())
            .ReturnsAsync(new List<IGrouping<GameState, Game>>());

        // Act
        var result = await _dashboardService.GetCharts();

        // Assert
        result.GameState.Should().BeEmpty();

        _gameStatisticsRepositoryMock.Verify(x => x.GetGamesGroupedByState(), Times.Once);
    }

    [Fact]
    public async Task GetCharts_ShouldReturnAllGameStates_WhenAllStatesHaveGames()
    {
        // Arrange
        var groupedGames = new List<IGrouping<GameState, Game>>
        {
            CreateGrouping(GameState.Owned, new List<Game> { new Game("Game 1") }),
            CreateGrouping(GameState.Wanted, new List<Game> { new Game("Game 2", state: GameState.Wanted) }),
            CreateGrouping(GameState.PreviouslyOwned, new List<Game> { new Game("Game 3", state: GameState.PreviouslyOwned) }),
            CreateGrouping(GameState.ForTrade, new List<Game> { new Game("Game 4", state: GameState.ForTrade) })
        };

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetGamesGroupedByState())
            .ReturnsAsync(groupedGames);

        // Act
        var result = await _dashboardService.GetCharts();

        // Assert
        result.GameState.Should().HaveCount(4);
        result.GameState.Should().Contain(x => x.Type == GameState.Owned);
        result.GameState.Should().Contain(x => x.Type == GameState.Wanted);
        result.GameState.Should().Contain(x => x.Type == GameState.PreviouslyOwned);
        result.GameState.Should().Contain(x => x.Type == GameState.ForTrade);
    }

    #endregion

    #region Helper Methods

    private void SetupBasicStatistics(int gameCount, int playerCount, int sessionCount, int locationCount, int expansionCount)
    {
        _gameRepositoryMock
            .Setup(x => x.CountAsync())
            .ReturnsAsync(gameCount);

        _gameRepositoryMock
            .Setup(x => x.GetTotalExpansionCount())
            .ReturnsAsync(expansionCount);

        _playerRepositoryMock
            .Setup(x => x.CountAsync())
            .ReturnsAsync(playerCount);

        _sessionRepositoryMock
            .Setup(x => x.CountAsync())
            .ReturnsAsync(sessionCount);

        _locationRepositoryMock
            .Setup(x => x.CountAsync())
            .ReturnsAsync(locationCount);
    }

    private void SetupPlayTimeStatistics(double totalPlayTime, double meanPlayTime)
    {
        _sessionRepositoryMock
            .Setup(x => x.GetTotalPlayTime())
            .ReturnsAsync(totalPlayTime);

        _sessionRepositoryMock
            .Setup(x => x.GetMeanPlayTime())
            .ReturnsAsync(meanPlayTime);
    }

    private void SetupPayedStatistics(double? meanPayed, double? totalPayed)
    {
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetMeanPayedAsync())
            .ReturnsAsync(meanPayed);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetTotalPayedAsync())
            .ReturnsAsync(totalPayed);
    }

    private void SetupMostWinningPlayer(Player? player)
    {
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetMostWins())
            .ReturnsAsync(player);
    }

    private static IGrouping<GameState, Game> CreateGrouping(GameState key, List<Game> games)
    {
        return games.GroupBy(_ => key).First();
    }

    #endregion
}
