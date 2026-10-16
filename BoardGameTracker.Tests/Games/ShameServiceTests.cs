using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class ShameServiceTests
{
    private static readonly DateTime FixedUtcNow = new(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IReadRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<ILogger<ShameService>> _loggerMock;
    private readonly ShameService _shameService;

    public ShameServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameNightRepositoryMock = new Mock<IReadRepository<GameNight>>();
        _configRepositoryMock = new Mock<IConfigRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _loggerMock = new Mock<ILogger<ShameService>>();
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(FixedUtcNow);

        _shameService = new ShameService(
            _gameRepositoryMock.Object,
            _gameNightRepositoryMock.Object,
            _configRepositoryMock.Object,
            _dateTimeProviderMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _configRepositoryMock.VerifyNoOtherCalls();
    }

    private void SetupFeature(int configuredMonths, bool gameNightsEnabled = true, List<UpcomingGameNight>? nights = null)
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false))
            .ReturnsAsync(true);
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameMonths, Constants.AppConfig.DefaultShelfOfShameMonths))
            .ReturnsAsync(configuredMonths);
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true))
            .ReturnsAsync(gameNightsEnabled);
        _gameNightRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<UpcomingGameNightsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nights ?? []);
    }

    private void SetupShameGames(int configuredMonths, List<ShameGame> shameGames, int? effectiveMonths = null, bool gameNightsEnabled = true, List<UpcomingGameNight>? nights = null)
    {
        SetupFeature(configuredMonths, gameNightsEnabled, nights);
        _gameRepositoryMock
            .Setup(x => x.GetShameGames(FixedUtcNow.AddMonths(-(effectiveMonths ?? configuredMonths))))
            .ReturnsAsync(shameGames);
    }

    private void SetupOwnedSummary(int count, decimal value)
    {
        _gameRepositoryMock
            .Setup(x => x.GetOwnedCollectionSummary(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OwnedCollectionSummary(count, value));
    }

    private void VerifyShameGamesCalls(int effectiveMonths, bool gameNightsEnabled = true)
    {
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameMonths, Constants.AppConfig.DefaultShelfOfShameMonths), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetShameGames(FixedUtcNow.AddMonths(-effectiveMonths)), Times.Once);
        if (gameNightsEnabled)
        {
            _gameNightRepositoryMock.Verify(x => x.ListAsync(It.Is<UpcomingGameNightsSpec>(s => s != null), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    private void VerifyStatisticsCalls(int effectiveMonths, bool gameNightsEnabled = true)
    {
        VerifyShameGamesCalls(effectiveMonths, gameNightsEnabled);
        _gameRepositoryMock.Verify(x => x.GetOwnedCollectionSummary(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    private void SetupDisabled()
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false))
            .ReturnsAsync(false);
    }

    #region CountShelfOfShameGames Tests

    [Theory]
    [InlineData(6)]
    [InlineData(12)]
    public async Task CountShelfOfShameGames_ShouldCountGamesOlderThanConfiguredCutoff_WhenFeatureEnabled(int configuredMonths)
    {
        var expectedCount = 5;

        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false))
            .ReturnsAsync(true);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameMonths, Constants.AppConfig.DefaultShelfOfShameMonths))
            .ReturnsAsync(configuredMonths);

        _gameRepositoryMock
            .Setup(x => x.CountGamesWithNoRecentSessions(FixedUtcNow.AddMonths(-configuredMonths), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        var result = await _shameService.CountShelfOfShameGames();

        result.Should().Be(expectedCount);

        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameMonths, Constants.AppConfig.DefaultShelfOfShameMonths), Times.Once);
        _gameRepositoryMock.Verify(x => x.CountGamesWithNoRecentSessions(FixedUtcNow.AddMonths(-configuredMonths), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountShelfOfShameGames_ShouldReturnZero_WhenFeatureDisabled()
    {
        SetupDisabled();

        var result = await _shameService.CountShelfOfShameGames();

        result.Should().Be(0);

        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetShameGames Tests

    [Fact]
    public async Task GetShameGames_ShouldThrowFeatureDisabled_WhenFeatureDisabled()
    {
        SetupDisabled();

        var action = () => _shameService.GetShameGames();

        await action.Should().ThrowAsync<FeatureDisabledException>()
            .Where(e => e.Feature == Constants.AppConfig.ShelfOfShameEnabled && e.Message == Constants.Errors.FeatureDisabled);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetShameStatistics_ShouldThrowFeatureDisabled_WhenFeatureDisabled()
    {
        SetupDisabled();

        var action = () => _shameService.GetShameStatistics();

        await action.Should().ThrowAsync<FeatureDisabledException>();
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetShameGames_ShouldReturnShameGames_WithLastSessionDate()
    {
        var shameGames = new List<ShameGame>
        {
            new() { Id = 1, Title = "Unplayed Game 1", Price = 50.00m, LastSessionDate = FixedUtcNow.AddMonths(-8) },
            new() { Id = 2, Title = "Unplayed Game 2", Price = 30.00m, LastSessionDate = null }
        };

        SetupShameGames(6, shameGames);

        var result = await _shameService.GetShameGames();

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Unplayed Game 1");
        result[0].LastSessionDate.Should().Be(FixedUtcNow.AddMonths(-8));
        result[1].LastSessionDate.Should().BeNull();

        VerifyShameGamesCalls(6);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetShameGames_ShouldReturnEmptyList_WhenNoGames()
    {
        SetupShameGames(6, []);

        var result = await _shameService.GetShameGames();

        result.Should().BeEmpty();

        VerifyShameGamesCalls(6);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(6, null, 6)]
    [InlineData(6, 12, 12)]
    [InlineData(6, 24, 24)]
    [InlineData(12, 6, 12)]
    [InlineData(12, 1, 12)]
    public async Task GetShameGames_ShouldNeverUseAShorterPeriodThanTheSetting(int configuredMonths, int? requestedMonths, int effectiveMonths)
    {
        SetupShameGames(configuredMonths, [], effectiveMonths);

        await _shameService.GetShameGames(requestedMonths);

        VerifyShameGamesCalls(effectiveMonths);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetShameGames_ShouldMarkGamesPlannedForTheSoonestNightThatSuggestsThem()
    {
        var friday = new UpcomingGameNight { Id = 4, Title = "Friday", StartDate = FixedUtcNow.AddDays(3), SuggestedGameIds = [2] };
        var saturday = new UpcomingGameNight { Id = 5, Title = "Saturday", StartDate = FixedUtcNow.AddDays(10), SuggestedGameIds = [2, 3] };
        var shameGames = new List<ShameGame>
        {
            new() { Id = 1, Title = "Root" },
            new() { Id = 2, Title = "Everdell" },
            new() { Id = 3, Title = "Spirit Island" }
        };

        SetupShameGames(6, shameGames, nights: [friday, saturday]);

        var result = await _shameService.GetShameGames();

        result[0].PlannedGameNightId.Should().BeNull();
        result[0].PlannedFor.Should().BeNull();
        result[1].PlannedGameNightId.Should().Be(4);
        result[1].PlannedFor.Should().Be(friday.StartDate);
        result[2].PlannedGameNightId.Should().Be(5);
        result[2].PlannedFor.Should().Be(saturday.StartDate);

        VerifyShameGamesCalls(6);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetShameGames_ShouldNotLookForNights_WhenGameNightsAreTurnedOff()
    {
        SetupShameGames(6, [new ShameGame { Id = 1, Title = "Root" }], gameNightsEnabled: false);

        var result = await _shameService.GetShameGames();

        result.Should().ContainSingle().Which.PlannedGameNightId.Should().BeNull();

        VerifyShameGamesCalls(6, gameNightsEnabled: false);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetShameStatistics Tests

    [Fact]
    public async Task GetShameStatistics_ShouldReturnCorrectStatistics_WithPricedGames()
    {
        var shameGames = new List<ShameGame>
        {
            new() { Id = 1, Title = "Game 1", Price = 50.00m },
            new() { Id = 2, Title = "Game 2", Price = 30.00m },
            new() { Id = 3, Title = "Game 3", Price = 20.00m }
        };

        SetupShameGames(6, shameGames);
        SetupOwnedSummary(10, 400m);

        var result = await _shameService.GetShameStatistics();

        result.Count.Should().Be(3);
        result.TotalValue.Should().Be(100.00m);
        result.AverageValue.Should().BeApproximately(33.33m, 0.01m);
        result.PricedGameCount.Should().Be(3);

        VerifyStatisticsCalls(6);
    }

    [Fact]
    public async Task GetShameStatistics_ShouldHandleGamesWithoutPrice()
    {
        var shameGames = new List<ShameGame>
        {
            new() { Id = 1, Title = "Game 1", Price = 50.00m },
            new() { Id = 2, Title = "Game 2", Price = null },
            new() { Id = 3, Title = "Game 3", Price = 30.00m }
        };

        SetupShameGames(6, shameGames);
        SetupOwnedSummary(10, 400m);

        var result = await _shameService.GetShameStatistics();

        result.Count.Should().Be(3);
        result.TotalValue.Should().Be(80.00m);
        result.AverageValue.Should().Be(40.00m);
        result.PricedGameCount.Should().Be(2);

        VerifyStatisticsCalls(6);
    }

    [Fact]
    public async Task GetShameStatistics_ShouldReturnNullValues_WhenNoGamesHavePrice()
    {
        var shameGames = new List<ShameGame>
        {
            new() { Id = 1, Title = "Game 1", Price = null },
            new() { Id = 2, Title = "Game 2", Price = null }
        };

        SetupShameGames(6, shameGames);
        SetupOwnedSummary(2, 0m);

        var result = await _shameService.GetShameStatistics();

        result.Count.Should().Be(2);
        result.TotalValue.Should().BeNull();
        result.AverageValue.Should().BeNull();
        result.PricedGameCount.Should().Be(0);
        result.NeverPlayedValue.Should().Be(0m);

        VerifyStatisticsCalls(6);
    }

    [Fact]
    public async Task GetShameStatistics_ShouldReturnNullTotalValueAndZeroAverage_WhenAllPricesAreZero()
    {
        var shameGames = new List<ShameGame>
        {
            new() { Id = 1, Title = "Game 1", Price = 0m },
            new() { Id = 2, Title = "Game 2", Price = 0m }
        };

        SetupShameGames(6, shameGames);
        SetupOwnedSummary(2, 0m);

        var result = await _shameService.GetShameStatistics();

        result.Count.Should().Be(2);
        result.TotalValue.Should().BeNull();
        result.AverageValue.Should().Be(0m);

        VerifyStatisticsCalls(6);
    }

    [Fact]
    public async Task GetShameStatistics_ShouldReturnZeroCount_WhenNoGames()
    {
        SetupShameGames(6, []);
        SetupOwnedSummary(0, 0m);

        var result = await _shameService.GetShameStatistics();

        result.Count.Should().Be(0);
        result.TotalValue.Should().BeNull();
        result.AverageValue.Should().BeNull();
        result.NeverPlayedCount.Should().Be(0);
        result.PlannedCount.Should().Be(0);
        result.NextGameNight.Should().BeNull();

        VerifyStatisticsCalls(6);
    }

    [Fact]
    public async Task GetShameStatistics_ShouldReportTheCollection_NeverPlayed_PlannedAndTheNextNight()
    {
        var friday = new UpcomingGameNight { Id = 4, Title = "Friday", StartDate = FixedUtcNow.AddDays(3), SuggestedGameIds = [2, 99] };
        var saturday = new UpcomingGameNight { Id = 5, Title = "Saturday", StartDate = FixedUtcNow.AddDays(10), SuggestedGameIds = [3] };
        var shameGames = new List<ShameGame>
        {
            new() { Id = 1, Title = "Root", Price = null },
            new() { Id = 2, Title = "Everdell", Price = 52m },
            new() { Id = 3, Title = "Wingspan", Price = 55m, LastSessionDate = FixedUtcNow.AddMonths(-20) }
        };

        SetupShameGames(6, shameGames, 12, nights: [friday, saturday]);
        SetupOwnedSummary(41, 1240m);

        var result = await _shameService.GetShameStatistics(12);

        result.Months.Should().Be(12);
        result.OwnedCount.Should().Be(41);
        result.CollectionValue.Should().Be(1240m);
        result.NeverPlayedCount.Should().Be(2);
        result.NeverPlayedValue.Should().Be(52m);
        result.TotalValue.Should().Be(107m);
        result.PlannedCount.Should().Be(2);
        result.NextGameNight.Should().BeSameAs(friday);

        VerifyStatisticsCalls(12);
    }

    [Fact]
    public async Task GetShameStatistics_ShouldHaveNoNextNight_WhenGameNightsAreTurnedOff()
    {
        SetupShameGames(6, [new ShameGame { Id = 1, Title = "Root" }], gameNightsEnabled: false);
        SetupOwnedSummary(1, 0m);

        var result = await _shameService.GetShameStatistics();

        result.NextGameNight.Should().BeNull();
        result.PlannedCount.Should().Be(0);

        VerifyStatisticsCalls(6, gameNightsEnabled: false);
    }

    #endregion
}
