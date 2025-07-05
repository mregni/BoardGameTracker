using System;
using System.Collections;
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
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<ILocationRepository> _locationRepositoryMock;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _locationRepositoryMock = new Mock<ILocationRepository>();

        _service = new DashboardService(
            _gameRepositoryMock.Object,
            _playerRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _locationRepositoryMock.Object);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnDashboardStatistics_WhenAllRepositoriesReturnData()
    {
        const int expectedGameCount = 10;
        const double expectedMeanPayed = 25.50;
        const double expectedTotalPayed = 255.0;
        const int expectedPlayerCount = 5;
        const int expectedSessionCount = 20;
        const double expectedTotalPlayTime = 1200.0;
        const double expectedMeanPlayTime = 60.0;
        const int expectedLocationCount = 3;
        const int expectedExpansionCount = 5;
        var mostWinPlayer = new Player {Id = 1, Name = "Top Player", Image = "player.jpg"};
        const int expectedWins = 15;

        _gameRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(expectedGameCount);
        _gameRepositoryMock.Setup(x => x.GetMeanPayedAsync()).ReturnsAsync(expectedMeanPayed);
        _gameRepositoryMock.Setup(x => x.GetTotalPayedAsync()).ReturnsAsync(expectedTotalPayed);
        _gameRepositoryMock.Setup(x => x.GetMostWins()).ReturnsAsync(mostWinPlayer);
        _gameRepositoryMock.Setup(x => x.GetTotalExpansionCount()).ReturnsAsync(expectedExpansionCount);
        _playerRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(expectedPlayerCount);
        _playerRepositoryMock.Setup(x => x.GetTotalWinCount(mostWinPlayer.Id)).ReturnsAsync(expectedWins);
        _sessionRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(expectedSessionCount);
        _sessionRepositoryMock.Setup(x => x.GetTotalPlayTime()).ReturnsAsync(expectedTotalPlayTime);
        _sessionRepositoryMock.Setup(x => x.GetMeanPlayTime()).ReturnsAsync(expectedMeanPlayTime);
        _locationRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(expectedLocationCount);

        var result = await _service.GetStatistics();

        result.Should().NotBeNull();
        result.GameCount.Should().Be(expectedGameCount);
        result.MeanPayed.Should().Be(expectedMeanPayed);
        result.TotalCost.Should().Be(expectedTotalPayed);
        result.PlayerCount.Should().Be(expectedPlayerCount);
        result.SessionCount.Should().Be(expectedSessionCount);
        result.TotalPlayTime.Should().Be(expectedTotalPlayTime);
        result.MeanPlayTime.Should().Be(expectedMeanPlayTime);
        result.LocationCount.Should().Be(expectedLocationCount);
        result.ExpansionCount.Should().Be(expectedExpansionCount);
        result.MostWinningPlayer.Should().NotBeNull();
        result.MostWinningPlayer.Id.Should().Be(mostWinPlayer.Id);
        result.MostWinningPlayer.Name.Should().Be(mostWinPlayer.Name);
        result.MostWinningPlayer.Image.Should().Be(mostWinPlayer.Image);
        result.MostWinningPlayer.TotalWins.Should().Be(expectedWins);

        VerifyAllRepositoryCallsOnce(mostWinPlayer.Id);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnStatisticsWithoutMostWinningPlayer_WhenGetMostWinsReturnsNull()
    {
        const int expectedGameCount = 5;
        const int expectedPlayerCount = 3;
        const int expectedSessionCount = 10;
        const int expectedLocationCount = 2;
        const int expectedExpansionCount = 5;

        _gameRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(expectedGameCount);
        _gameRepositoryMock.Setup(x => x.GetMeanPayedAsync()).ReturnsAsync(0.0);
        _gameRepositoryMock.Setup(x => x.GetTotalPayedAsync()).ReturnsAsync(0.0);
        _gameRepositoryMock.Setup(x => x.GetMostWins()).ReturnsAsync((Player?) null);
        _gameRepositoryMock.Setup(x => x.GetTotalExpansionCount()).ReturnsAsync(expectedExpansionCount);
        _playerRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(expectedPlayerCount);
        _sessionRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(expectedSessionCount);
        _sessionRepositoryMock.Setup(x => x.GetTotalPlayTime()).ReturnsAsync(0.0);
        _sessionRepositoryMock.Setup(x => x.GetMeanPlayTime()).ReturnsAsync(0.0);
        _locationRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(expectedLocationCount);

        var result = await _service.GetStatistics();

        result.Should().NotBeNull();
        result.GameCount.Should().Be(expectedGameCount);
        result.PlayerCount.Should().Be(expectedPlayerCount);
        result.SessionCount.Should().Be(expectedSessionCount);
        result.LocationCount.Should().Be(expectedLocationCount);
        result.ExpansionCount.Should().Be(expectedExpansionCount);
        result.MostWinningPlayer.Should().BeNull();

        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetMeanPayedAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalPayedAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetMostWins(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalExpansionCount(), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTotalWinCount(It.IsAny<int>()), Times.Never);
        _sessionRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetTotalPlayTime(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetMeanPlayTime(), Times.Once);
        _locationRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(100, 50.0, 500.0, 25, 200, 3000.0, 15.0, 8, 10)]
    [InlineData(1, 10.5, 10.5, 1, 1, 60.0, 60.0, 1, 5)]
    [InlineData(999, 0.0, 0.0, 500, 1000, 50000.0, 50.0, 100, 20)]
    public async Task GetStatistics_ShouldReturnCorrectValues_WithDifferentDataSets(
        int gameCount, double meanPayed, double totalPayed, int playerCount,
        int sessionCount, double totalPlayTime, double meanPlayTime, int locationCount, int expectedExpansionCount)
    {
        _gameRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(gameCount);
        _gameRepositoryMock.Setup(x => x.GetMeanPayedAsync()).ReturnsAsync(meanPayed);
        _gameRepositoryMock.Setup(x => x.GetTotalPayedAsync()).ReturnsAsync(totalPayed);
        _gameRepositoryMock.Setup(x => x.GetTotalExpansionCount()).ReturnsAsync(expectedExpansionCount);
        _gameRepositoryMock.Setup(x => x.GetMostWins()).ReturnsAsync((Player?) null);
        _playerRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(playerCount);
        _sessionRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(sessionCount);
        _sessionRepositoryMock.Setup(x => x.GetTotalPlayTime()).ReturnsAsync(totalPlayTime);
        _sessionRepositoryMock.Setup(x => x.GetMeanPlayTime()).ReturnsAsync(meanPlayTime);
        _locationRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(locationCount);

        var result = await _service.GetStatistics();

        result.GameCount.Should().Be(gameCount);
        result.MeanPayed.Should().Be(meanPayed);
        result.TotalCost.Should().Be(totalPayed);
        result.PlayerCount.Should().Be(playerCount);
        result.SessionCount.Should().Be(sessionCount);
        result.TotalPlayTime.Should().Be(totalPlayTime);
        result.MeanPlayTime.Should().Be(meanPlayTime);
        result.LocationCount.Should().Be(locationCount);
        result.ExpansionCount.Should().Be(expectedExpansionCount);

        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetMeanPayedAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalPayedAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetMostWins(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalExpansionCount(), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetTotalPlayTime(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetMeanPlayTime(), Times.Once);
        _locationRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldThrowException_WhenGameRepositoryThrows()
    {
        var expectedException = new InvalidOperationException("Game repository error");
        _gameRepositoryMock.Setup(x => x.CountAsync()).ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetStatistics());

        exception.Should().Be(expectedException);
        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _locationRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldThrowException_WhenPlayerRepositoryThrows()
    {
        var expectedException = new ArgumentException("Player repository error");
        _gameRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(5);
        _gameRepositoryMock.Setup(x => x.GetMeanPayedAsync()).ReturnsAsync(10.0);
        _gameRepositoryMock.Setup(x => x.GetTotalPayedAsync()).ReturnsAsync(50.0);
        _playerRepositoryMock.Setup(x => x.CountAsync()).ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetStatistics());

        exception.Should().Be(expectedException);
        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetMeanPayedAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalPayedAsync(), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _locationRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetCharts_ShouldReturnDashboardCharts_WhenRepositoryReturnsData()
    {
        var gameStates = new List<IGrouping<GameState, Game>>
        {
            CreateGrouping(GameState.Owned, new List<Game> {new(), new(), new()}),
            CreateGrouping(GameState.Wanted, new List<Game> {new(), new()}),
            CreateGrouping(GameState.PreviouslyOwned, new List<Game> {new()})
        };

        _gameRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ReturnsAsync(gameStates);

        var result = await _service.GetCharts();

        result.Should().NotBeNull();
        result.GameState.Should().NotBeNull();
        result.GameState.Should().HaveCount(3);

        var gameStateList = result.GameState.ToList();
        gameStateList[0].Type.Should().Be(GameState.Owned);
        gameStateList[0].GameCount.Should().Be(3);
        gameStateList[1].Type.Should().Be(GameState.Wanted);
        gameStateList[1].GameCount.Should().Be(2);
        gameStateList[2].Type.Should().Be(GameState.PreviouslyOwned);
        gameStateList[2].GameCount.Should().Be(1);

        _gameRepositoryMock.Verify(x => x.GetGamesGroupedByState(), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _locationRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetCharts_ShouldReturnEmptyGameState_WhenRepositoryReturnsEmptyList()
    {
        var gameStates = new List<IGrouping<GameState, Game>>();

        _gameRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ReturnsAsync(gameStates);

        var result = await _service.GetCharts();

        result.Should().NotBeNull();
        result.GameState.Should().NotBeNull();
        result.GameState.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetGamesGroupedByState(), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _locationRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetCharts_ShouldThrowException_WhenGameRepositoryThrows()
    {
        var expectedException = new TimeoutException("Repository timeout");
        _gameRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => _service.GetCharts());

        exception.Should().Be(expectedException);
        _gameRepositoryMock.Verify(x => x.GetGamesGroupedByState(), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _locationRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetCharts_ShouldMapCorrectly_WhenMultipleGameStatesWithDifferentCounts()
    {
        var gameStates = new List<IGrouping<GameState, Game>>
        {
            CreateGrouping(GameState.Owned, Enumerable.Range(1, 10).Select(_ => new Game())),
            CreateGrouping(GameState.Wanted, Enumerable.Range(1, 5).Select(_ => new Game())),
            CreateGrouping(GameState.PreviouslyOwned, Enumerable.Range(1, 2).Select(_ => new Game())),
            CreateGrouping(GameState.ForTrade, Enumerable.Range(1, 1).Select(_ => new Game()))
        };

        _gameRepositoryMock.Setup(x => x.GetGamesGroupedByState()).ReturnsAsync(gameStates);

        var result = await _service.GetCharts();

        result.GameState.Should().HaveCount(4);
        var gameStateArray = result.GameState.ToArray();

        gameStateArray.Should().Contain(x => x.Type == GameState.Owned && x.GameCount == 10);
        gameStateArray.Should().Contain(x => x.Type == GameState.Wanted && x.GameCount == 5);
        gameStateArray.Should().Contain(x => x.Type == GameState.PreviouslyOwned && x.GameCount == 2);
        gameStateArray.Should().Contain(x => x.Type == GameState.ForTrade && x.GameCount == 1);

        _gameRepositoryMock.Verify(x => x.GetGamesGroupedByState(), Times.Once);
        _gameRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _locationRepositoryMock.VerifyNoOtherCalls();
    }

    private void VerifyAllRepositoryCallsOnce(int playerId)
    {
        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetMeanPayedAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalPayedAsync(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetMostWins(), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalExpansionCount(), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTotalWinCount(playerId), Times.Once);
        _sessionRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetTotalPlayTime(), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetMeanPlayTime(), Times.Once);
        _locationRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _locationRepositoryMock.VerifyNoOtherCalls();
    }

    private static IGrouping<GameState, Game> CreateGrouping(GameState key, IEnumerable<Game> games)
    {
        return new TestGrouping<GameState, Game>(key, games);
    }
}

public class TestGrouping<TKey, TElement>(TKey key, IEnumerable<TElement> elements) : IGrouping<TKey, TElement>
{
    public TKey Key { get; } = key;
    public IEnumerator<TElement> GetEnumerator() => elements.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}