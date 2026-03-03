using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class ShameServiceTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<ILogger<ShameService>> _loggerMock;
    private readonly ShameService _shameService;

    public ShameServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _configRepositoryMock = new Mock<IConfigRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _loggerMock = new Mock<ILogger<ShameService>>();
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        _shameService = new ShameService(
            _gameRepositoryMock.Object,
            _configRepositoryMock.Object,
            _dateTimeProviderMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _configRepositoryMock.VerifyNoOtherCalls();
    }

    #region CountShelfOfShameGames Tests

    [Fact]
    public async Task CountShelfOfShameGames_ShouldReturnCount_WhenFeatureEnabled()
    {
        // Arrange
        var configuredMonths = 6;
        var expectedCount = 5;

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled))
            .ReturnsAsync(true);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(configuredMonths);

        _gameRepositoryMock
            .Setup(x => x.CountGamesWithNoRecentSessions(It.IsAny<DateTime>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _shameService.CountShelfOfShameGames();

        // Assert
        result.Should().Be(expectedCount);

        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths), Times.Once);
        _gameRepositoryMock.Verify(x => x.CountGamesWithNoRecentSessions(It.IsAny<DateTime>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountShelfOfShameGames_ShouldReturnZero_WhenFeatureDisabled()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled))
            .ReturnsAsync(false);

        // Act
        var result = await _shameService.CountShelfOfShameGames();

        // Assert
        result.Should().Be(0);

        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountShelfOfShameGames_ShouldUseCutoffDate_BasedOnConfiguredMonths()
    {
        // Arrange
        var configuredMonths = 12;
        DateTime capturedCutoffDate = default;

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled))
            .ReturnsAsync(true);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(configuredMonths);

        _gameRepositoryMock
            .Setup(x => x.CountGamesWithNoRecentSessions(It.IsAny<DateTime>()))
            .Callback<DateTime>(date => capturedCutoffDate = date)
            .ReturnsAsync(0);

        // Act
        await _shameService.CountShelfOfShameGames();

        // Assert
        var expectedCutoffDate = DateTime.UtcNow.AddMonths(-configuredMonths);
        capturedCutoffDate.Should().BeCloseTo(expectedCutoffDate, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region GetShameGames Tests

    [Fact]
    public async Task GetShameGames_ShouldReturnShameGames_WithLastSessionDate()
    {
        // Arrange
        var configuredMonths = 6;
        var shameGames = new List<ShameGame>
        {
            new ShameGame
            {
                Id = 1,
                Title = "Unplayed Game 1",
                Price = 50.00m,
                LastSessionDate = DateTime.UtcNow.AddMonths(-8)
            },
            new ShameGame
            {
                Id = 2,
                Title = "Unplayed Game 2",
                Price = 30.00m,
                LastSessionDate = null
            }
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(configuredMonths);

        _gameRepositoryMock
            .Setup(x => x.GetShameGames(It.IsAny<DateTime>()))
            .ReturnsAsync(shameGames);

        // Act
        var result = await _shameService.GetShameGames();

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Unplayed Game 1");
        result[0].LastSessionDate.Should().NotBeNull();
        result[1].LastSessionDate.Should().BeNull();

        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetShameGames(It.IsAny<DateTime>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetShameGames_ShouldReturnEmptyList_WhenNoGames()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(6);

        _gameRepositoryMock
            .Setup(x => x.GetShameGames(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<ShameGame>());

        // Act
        var result = await _shameService.GetShameGames();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetShameStatistics Tests

    [Fact]
    public async Task GetShameStatistics_ShouldReturnCorrectStatistics_WithPricedGames()
    {
        // Arrange
        var shameGames = new List<ShameGame>
        {
            new ShameGame { Id = 1, Title = "Game 1", Price = 50.00m },
            new ShameGame { Id = 2, Title = "Game 2", Price = 30.00m },
            new ShameGame { Id = 3, Title = "Game 3", Price = 20.00m }
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(6);

        _gameRepositoryMock
            .Setup(x => x.GetShameGames(It.IsAny<DateTime>()))
            .ReturnsAsync(shameGames);

        // Act
        var result = await _shameService.GetShameStatistics();

        // Assert
        result.Count.Should().Be(3);
        result.TotalValue.Should().Be(100.00m);
        result.AverageValue.Should().BeApproximately(33.33m, 0.01m);
    }

    [Fact]
    public async Task GetShameStatistics_ShouldHandleGamesWithoutPrice()
    {
        // Arrange
        var shameGames = new List<ShameGame>
        {
            new ShameGame { Id = 1, Title = "Game 1", Price = 50.00m },
            new ShameGame { Id = 2, Title = "Game 2", Price = null },
            new ShameGame { Id = 3, Title = "Game 3", Price = 30.00m }
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(6);

        _gameRepositoryMock
            .Setup(x => x.GetShameGames(It.IsAny<DateTime>()))
            .ReturnsAsync(shameGames);

        // Act
        var result = await _shameService.GetShameStatistics();

        // Assert
        result.Count.Should().Be(3);
        result.TotalValue.Should().Be(80.00m);
        result.AverageValue.Should().Be(40.00m); // Average of games with price only
    }

    [Fact]
    public async Task GetShameStatistics_ShouldReturnNullValues_WhenNoGamesHavePrice()
    {
        // Arrange
        var shameGames = new List<ShameGame>
        {
            new ShameGame { Id = 1, Title = "Game 1", Price = null },
            new ShameGame { Id = 2, Title = "Game 2", Price = null }
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(6);

        _gameRepositoryMock
            .Setup(x => x.GetShameGames(It.IsAny<DateTime>()))
            .ReturnsAsync(shameGames);

        // Act
        var result = await _shameService.GetShameStatistics();

        // Assert
        result.Count.Should().Be(2);
        result.TotalValue.Should().BeNull();
        result.AverageValue.Should().BeNull();
    }

    [Fact]
    public async Task GetShameStatistics_ShouldReturnZeroCount_WhenNoGames()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(6);

        _gameRepositoryMock
            .Setup(x => x.GetShameGames(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<ShameGame>());

        // Act
        var result = await _shameService.GetShameStatistics();

        // Assert
        result.Count.Should().Be(0);
        result.TotalValue.Should().BeNull();
        result.AverageValue.Should().BeNull();
    }

    #endregion
}
