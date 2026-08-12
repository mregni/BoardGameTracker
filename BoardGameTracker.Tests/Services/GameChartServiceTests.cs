using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Games.Specifications;
using BoardGameTracker.Core.Sessions.Specifications;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

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

    #region GetPlayByDayChart Tests

    [Fact]
    public async Task GetPlayByDayChart_ShouldReturnAllDaysOfWeek()
    {
        // Arrange
        var gameId = 1;
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetPlayByDayChart(gameId))
            .ReturnsAsync([]);

        // Act
        var result = await _gameChartService.GetPlayByDayChart(gameId);

        // Assert
        result.Should().HaveCount(7);

        _gameStatisticsRepositoryMock.Verify(x => x.GetPlayByDayChart(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetPlayerCountChart Tests

    [Fact]
    public async Task GetPlayerCountChart_ShouldReturnPlayerCounts()
    {
        // Arrange
        var gameId = 1;
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetPlayerCountChart(gameId))
            .ReturnsAsync([]);

        // Act
        var result = await _gameChartService.GetPlayerCountChart(gameId);

        // Assert
        result.Should().BeEmpty();

        _gameStatisticsRepositoryMock.Verify(x => x.GetPlayerCountChart(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetPlayerScoringChart Tests

    [Fact]
    public async Task GetPlayerScoringChart_ShouldReturnNull_WhenGameHasNoScoring()
    {
        // Arrange
        var gameId = 1;

        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)false);

        // Act
        var result = await _gameChartService.GetPlayerScoringChart(gameId);

        // Assert
        result.Should().BeNull();

        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerScoringChart_ShouldReturnNull_WhenGameDoesNotExist()
    {
        // Arrange
        var gameId = 999;

        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)null);

        // Act
        var result = await _gameChartService.GetPlayerScoringChart(gameId);

        // Assert
        result.Should().BeNull();

        _gameRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<GameHasScoringSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetTopPlayers Tests

    [Fact]
    public async Task GetTopPlayers_ShouldReturnEmptyList_WhenNoSessions()
    {
        // Arrange
        var gameId = 1;
        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _gameChartService.GetTopPlayers(gameId);

        // Assert
        result.Should().BeEmpty();

        _sessionRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<Session>>(s => s is SessionsByGameSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
