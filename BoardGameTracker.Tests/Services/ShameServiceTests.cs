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
    private static readonly DateTime FixedUtcNow = new(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);

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
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(FixedUtcNow);

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

    private void SetupShameGames(int configuredMonths, List<ShameGame> shameGames)
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(configuredMonths);

        _gameRepositoryMock
            .Setup(x => x.GetShameGames(FixedUtcNow.AddMonths(-configuredMonths)))
            .ReturnsAsync(shameGames);
    }

    private void VerifyShameGamesCalls(int configuredMonths)
    {
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetShameGames(FixedUtcNow.AddMonths(-configuredMonths)), Times.Once);
        VerifyNoOtherCalls();
    }

    #region CountShelfOfShameGames Tests

    [Theory]
    [InlineData(6)]
    [InlineData(12)]
    public async Task CountShelfOfShameGames_ShouldCountGamesOlderThanConfiguredCutoff_WhenFeatureEnabled(int configuredMonths)
    {
        var expectedCount = 5;

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled))
            .ReturnsAsync(true);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(configuredMonths);

        _gameRepositoryMock
            .Setup(x => x.CountGamesWithNoRecentSessions(FixedUtcNow.AddMonths(-configuredMonths)))
            .ReturnsAsync(expectedCount);

        var result = await _shameService.CountShelfOfShameGames();

        result.Should().Be(expectedCount);

        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths), Times.Once);
        _gameRepositoryMock.Verify(x => x.CountGamesWithNoRecentSessions(FixedUtcNow.AddMonths(-configuredMonths)), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountShelfOfShameGames_ShouldReturnZero_WhenFeatureDisabled()
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled))
            .ReturnsAsync(false);

        var result = await _shameService.CountShelfOfShameGames();

        result.Should().Be(0);

        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetShameGames Tests

    [Fact]
    public async Task GetShameGames_ShouldReturnShameGames_WithLastSessionDate()
    {
        var configuredMonths = 6;
        var shameGames = new List<ShameGame>
        {
            new ShameGame
            {
                Id = 1,
                Title = "Unplayed Game 1",
                Price = 50.00m,
                LastSessionDate = FixedUtcNow.AddMonths(-8)
            },
            new ShameGame
            {
                Id = 2,
                Title = "Unplayed Game 2",
                Price = 30.00m,
                LastSessionDate = null
            }
        };

        SetupShameGames(configuredMonths, shameGames);

        var result = await _shameService.GetShameGames();

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Unplayed Game 1");
        result[0].LastSessionDate.Should().Be(FixedUtcNow.AddMonths(-8));
        result[1].LastSessionDate.Should().BeNull();

        VerifyShameGamesCalls(configuredMonths);
    }

    [Fact]
    public async Task GetShameGames_ShouldReturnEmptyList_WhenNoGames()
    {
        SetupShameGames(6, []);

        var result = await _shameService.GetShameGames();

        result.Should().BeEmpty();

        VerifyShameGamesCalls(6);
    }

    #endregion

    #region GetShameStatistics Tests

    [Fact]
    public async Task GetShameStatistics_ShouldReturnCorrectStatistics_WithPricedGames()
    {
        var shameGames = new List<ShameGame>
        {
            new ShameGame { Id = 1, Title = "Game 1", Price = 50.00m },
            new ShameGame { Id = 2, Title = "Game 2", Price = 30.00m },
            new ShameGame { Id = 3, Title = "Game 3", Price = 20.00m }
        };

        SetupShameGames(6, shameGames);

        var result = await _shameService.GetShameStatistics();

        result.Count.Should().Be(3);
        result.TotalValue.Should().Be(100.00m);
        result.AverageValue.Should().BeApproximately(33.33m, 0.01m);

        VerifyShameGamesCalls(6);
    }

    [Fact]
    public async Task GetShameStatistics_ShouldHandleGamesWithoutPrice()
    {
        var shameGames = new List<ShameGame>
        {
            new ShameGame { Id = 1, Title = "Game 1", Price = 50.00m },
            new ShameGame { Id = 2, Title = "Game 2", Price = null },
            new ShameGame { Id = 3, Title = "Game 3", Price = 30.00m }
        };

        SetupShameGames(6, shameGames);

        var result = await _shameService.GetShameStatistics();

        result.Count.Should().Be(3);
        result.TotalValue.Should().Be(80.00m);
        result.AverageValue.Should().Be(40.00m);

        VerifyShameGamesCalls(6);
    }

    [Fact]
    public async Task GetShameStatistics_ShouldReturnNullValues_WhenNoGamesHavePrice()
    {
        var shameGames = new List<ShameGame>
        {
            new ShameGame { Id = 1, Title = "Game 1", Price = null },
            new ShameGame { Id = 2, Title = "Game 2", Price = null }
        };

        SetupShameGames(6, shameGames);

        var result = await _shameService.GetShameStatistics();

        result.Count.Should().Be(2);
        result.TotalValue.Should().BeNull();
        result.AverageValue.Should().BeNull();

        VerifyShameGamesCalls(6);
    }

    [Fact]
    public async Task GetShameStatistics_ShouldReturnNullTotalValueAndZeroAverage_WhenAllPricesAreZero()
    {
        var shameGames = new List<ShameGame>
        {
            new ShameGame { Id = 1, Title = "Game 1", Price = 0m },
            new ShameGame { Id = 2, Title = "Game 2", Price = 0m }
        };

        SetupShameGames(6, shameGames);

        var result = await _shameService.GetShameStatistics();

        result.Count.Should().Be(2);
        result.TotalValue.Should().BeNull();
        result.AverageValue.Should().Be(0m);

        VerifyShameGamesCalls(6);
    }

    [Fact]
    public async Task GetShameStatistics_ShouldReturnZeroCount_WhenNoGames()
    {
        SetupShameGames(6, []);

        var result = await _shameService.GetShameStatistics();

        result.Count.Should().Be(0);
        result.TotalValue.Should().BeNull();
        result.AverageValue.Should().BeNull();

        VerifyShameGamesCalls(6);
    }

    #endregion
}
