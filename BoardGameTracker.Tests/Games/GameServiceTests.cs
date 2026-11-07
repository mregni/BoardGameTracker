using Ardalis.Specification;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Core.ChangeDetection.Interfaces;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Games.Specifications;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Manuals.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class GameServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 14, 30, 0, DateTimeKind.Utc);

    private readonly CancellationToken _token = new CancellationTokenSource().Token;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IReadRepository<Session>> _sessionRepositoryMock;
    private readonly Mock<IBoardGameGeekXmlApi2Client> _bggClientMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<IManualService> _manualServiceMock;
    private readonly Mock<IChangeDetectionClient> _changeDetectionClientMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<ILogger<GameService>> _loggerMock;
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _sessionRepositoryMock = new Mock<IReadRepository<Session>>();
        _bggClientMock = new Mock<IBoardGameGeekXmlApi2Client>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock.Setup(x => x.GetBggApiKeyAsync()).ReturnsAsync("test-api-key");
        _imageServiceMock = new Mock<IImageService>();
        _manualServiceMock = new Mock<IManualService>();
        _changeDetectionClientMock = new Mock<IChangeDetectionClient>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _loggerMock = new Mock<ILogger<GameService>>();

        _gameService = new GameService(
            _gameRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _imageServiceMock.Object,
            _manualServiceMock.Object,
            _bggClientMock.Object,
            _settingsServiceMock.Object,
            _changeDetectionClientMock.Object,
            _unitOfWorkMock.Object,
            _dateTimeProviderMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _bggClientMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
        _manualServiceMock.VerifyNoOtherCalls();
        _changeDetectionClientMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _settingsServiceMock.VerifyNoOtherCalls();
        _dateTimeProviderMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    private static bool Targets(ISpecification<Game> spec, int id) =>
        spec.IsSatisfiedBy(new Game("Target") { Id = id }) && !spec.IsSatisfiedBy(new Game("Other") { Id = id + 1 });

    private static bool IsThing(ThingRequest request, int id) => request.RelativeUrl.ToString() == $"thing?id={id}";

    private static bool IsExpansions(ThingRequest request, params int[] ids) =>
        request.RelativeUrl.ToString() == $"thing?id={string.Join("%2C", ids)}&types=boardgameexpansion";

    private void SetupAdditionDate(DateTime date)
    {
        _dateTimeProviderMock.Setup(x => x.Now).Returns(Now);
        _dateTimeProviderMock.Setup(x => x.ConvertToUtc(date)).Returns(DateTime.SpecifyKind(date, DateTimeKind.Utc));
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(DateTime.SpecifyKind(date, DateTimeKind.Utc))).Returns(date);
    }

    private void VerifyAdditionDateChecked(DateTime date)
    {
        _dateTimeProviderMock.Verify(x => x.Now, Times.Once);
        _dateTimeProviderMock.Verify(x => x.ConvertToUtc(date), Times.Once);
        _dateTimeProviderMock.Verify(x => x.ConvertToLocalTime(DateTime.SpecifyKind(date, DateTimeKind.Utc)), Times.Once);
    }

    private void VerifyApiKeyRead() => _settingsServiceMock.Verify(x => x.GetBggApiKeyAsync(), Times.Once);

    private static ThingResponse CreateSucceededThingResponse(IEnumerable<ThingResponse.Item> items)
    {
        return new ThingResponse(items);
    }

    private static ThingResponse CreateFailedThingResponse()
    {
        return (ThingResponse)RuntimeHelpers.GetUninitializedObject(typeof(ThingResponse));
    }

    private static ThingResponse.Item CreateThingItem(int id, string name)
    {
        return new ThingResponse.Item
        {
            Id = id,
            Name = name
        };
    }

    private static ThingResponse.Item CreateThingItemWithExpansionLinks(int id, string name, IEnumerable<ThingResponse.Link> links)
    {
        return new ThingResponse.Item
        {
            Id = id,
            Name = name,
            Links = links.ToList()
        };
    }

    #region GetGames Tests

    [Fact]
    public async Task GetGames_ShouldReturnAllGames()
    {
        var games = new List<Game>
        {
            new Game("Game 1") { Id = 1 },
            new Game("Game 2") { Id = 2 }
        };

        _gameRepositoryMock
            .Setup(x => x.GetGamesOverviewList())
            .ReturnsAsync(games);

        var result = await _gameService.GetGames();

        result.Should().BeSameAs(games);

        _gameRepositoryMock.Verify(x => x.GetGamesOverviewList(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Fetching all games");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGames_ShouldReturnEmptyList_WhenNoGamesExist()
    {
        _gameRepositoryMock
            .Setup(x => x.GetGamesOverviewList())
            .ReturnsAsync([]);

        var result = await _gameService.GetGames();

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetGamesOverviewList(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Fetching all games");
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetGameById Tests

    [Fact]
    public async Task GetGameById_ShouldReturnGame_WhenGameExists()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameByIdWithDetailsForReadSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);

        var result = await _gameService.GetGameById(gameId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(gameId);

        _gameRepositoryMock.Verify(
            x => x.SingleOrDefaultAsync(
                It.Is<GameByIdWithDetailsForReadSpec>(s => s.IsSatisfiedBy(game)),
                CancellationToken.None),
            Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Fetching game {gameId}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameById_ShouldReturnNull_WhenGameDoesNotExist()
    {
        var gameId = 999;

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameByIdWithDetailsForReadSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync((Game?)null);

        var result = await _gameService.GetGameById(gameId);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(
            x => x.SingleOrDefaultAsync(It.Is<GameByIdWithDetailsForReadSpec>(s => Targets(s, gameId)), CancellationToken.None),
            Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Fetching game {gameId}");
        VerifyNoOtherCalls();
    }

    #endregion

    #region Price Tests

    [Fact]
    public async Task GetGamePriceAsync_ShouldReturnNull_WhenGameDoesNotExist()
    {
        _gameRepositoryMock.Setup(x => x.GetWatchInfo(999)).ReturnsAsync((GameWatchInfo?)null);

        var result = await _gameService.GetGamePriceAsync(999, false, _token);

        result.Should().BeNull();
        _gameRepositoryMock.Verify(x => x.GetWatchInfo(999), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Fetching price for game 999");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGamePriceAsync_ShouldReturnUnavailable_WhenGameHasNoWatchId()
    {
        _gameRepositoryMock.Setup(x => x.GetWatchInfo(1)).ReturnsAsync(new GameWatchInfo(1, null));

        var result = await _gameService.GetGamePriceAsync(1, false, _token);

        result.Should().NotBeNull();
        result!.GameId.Should().Be(1);
        result.Available.Should().BeFalse();
        result.Status.Should().Be(ChangeDetectionStatus.NotConfigured);
        _gameRepositoryMock.Verify(x => x.GetWatchInfo(1), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Fetching price for game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGamePriceAsync_ShouldReturnMappedResult_WhenWatchIdSet()
    {
        var watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        _gameRepositoryMock.Setup(x => x.GetWatchInfo(1)).ReturnsAsync(new GameWatchInfo(1, watchId));
        var checkedAt = new DateTime(2026, 8, 28, 11, 0, 0, DateTimeKind.Utc);
        var fetchedAt = new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
        _changeDetectionClientMock
            .Setup(x => x.GetLatestAsync(watchId, false, _token))
            .ReturnsAsync(new ChangeDetectionResult
            {
                Status = ChangeDetectionStatus.Ok,
                InStock = true,
                Price = 22.5m,
                Currency = "EUR",
                CheckedAt = checkedAt,
                SourceUrl = "https://shop.example.com/brass",
                Title = "Brass: Birmingham | Example Shop",
                RecheckQueued = true,
                FetchedAt = fetchedAt
            });

        var result = await _gameService.GetGamePriceAsync(1, false, _token);

        result.Should().BeEquivalentTo(new GamePriceDto
        {
            GameId = 1,
            WatchId = watchId,
            Available = true,
            Status = ChangeDetectionStatus.Ok,
            InStock = true,
            Price = 22.5m,
            Currency = "EUR",
            CheckedAt = checkedAt,
            ShopUrl = "https://shop.example.com/brass",
            RecheckQueued = true,
            FetchedAt = fetchedAt
        });
        _gameRepositoryMock.Verify(x => x.GetWatchInfo(1), Times.Once);
        _changeDetectionClientMock.Verify(
            x => x.GetLatestAsync(watchId, false, _token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Fetching price for game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTrackedPricesAsync_ShouldReportUnreachable_ForAWatchMissingFromTheBatchResult()
    {
        var watchOne = "e0808154-28da-4b85-9a71-24a409e694f1";
        var watchTwo = "f1919265-39eb-5c96-a082-35b510f705a2";
        var gameOne = new Game("Game One") { Id = 1 };
        gameOne.UpdateChangeDetectionWatchId(watchOne);
        var gameTwo = new Game("Game Two") { Id = 2 };
        gameTwo.UpdateChangeDetectionWatchId(watchTwo);
        _gameRepositoryMock.Setup(x => x.GetTrackedGames()).ReturnsAsync(new List<Game> { gameOne, gameTwo });
        _changeDetectionClientMock
            .Setup(x => x.GetLatestAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { watchOne, watchTwo })), true, _token))
            .ReturnsAsync(new Dictionary<string, ChangeDetectionResult>
            {
                [watchOne] = new() { Status = ChangeDetectionStatus.Ok, Price = 22.5m }
            });

        var result = await _gameService.GetTrackedPricesAsync(true, _token);

        result.Select(x => x.GameId).Should().Equal(1, 2);
        result[0].Price.Should().Be(22.5m);
        var missing = result.Single(x => x.GameId == 2);
        missing.WatchId.Should().Be(watchTwo);
        missing.Available.Should().BeFalse();
        missing.Status.Should().Be(ChangeDetectionStatus.Unreachable);
        _gameRepositoryMock.Verify(x => x.GetTrackedGames(), Times.Once);
        _changeDetectionClientMock.Verify(
            x => x.GetLatestAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { watchOne, watchTwo })), true, _token),
            Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Fetching prices for tracked games");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTrackedPricesAsync_ShouldReturnPriceForEachTrackedGame()
    {
        var watchOne = "e0808154-28da-4b85-9a71-24a409e694f1";
        var watchTwo = "f1919265-39eb-5c96-a082-35b510f705a2";
        var gameOne = new Game("Game One") { Id = 1 };
        gameOne.UpdateChangeDetectionWatchId(watchOne);
        var gameTwo = new Game("Game Two") { Id = 2 };
        gameTwo.UpdateChangeDetectionWatchId(watchTwo);

        _gameRepositoryMock.Setup(x => x.GetTrackedGames())
            .ReturnsAsync(new List<Game> { gameOne, gameTwo });
        _changeDetectionClientMock
            .Setup(x => x.GetLatestAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { watchOne, watchTwo })), false, _token))
            .ReturnsAsync(new Dictionary<string, ChangeDetectionResult>
            {
                [watchOne] = new() { Status = ChangeDetectionStatus.Ok, InStock = true, Price = 22.5m },
                [watchTwo] = new() { Status = ChangeDetectionStatus.Ok, InStock = false, Price = 10m }
            });

        var result = await _gameService.GetTrackedPricesAsync(false, _token);

        result.Should().BeEquivalentTo(new[]
        {
            new GamePriceDto { GameId = 1, WatchId = watchOne, Available = true, Status = ChangeDetectionStatus.Ok, InStock = true, Price = 22.5m },
            new GamePriceDto { GameId = 2, WatchId = watchTwo, Available = true, Status = ChangeDetectionStatus.Ok, InStock = false, Price = 10m }
        });
        _gameRepositoryMock.Verify(x => x.GetTrackedGames(), Times.Once);
        _changeDetectionClientMock.Verify(
            x => x.GetLatestAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { watchOne, watchTwo })),
                false,
                _token),
            Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Fetching prices for tracked games");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldSyncShopUrlFromWatch_WhenWatchIdChanges()
    {
        var watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var command = new UpdateGameCommand
        {
            Id = 1,
            Title = "Brass",
            HasScoring = true,
            State = GameState.Wanted,
            ChangeDetectionWatchId = watchId
        };
        var existingGame = new Game("Brass", true) { Id = 1 };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingGame);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _changeDetectionClientMock
            .Setup(x => x.GetWatchInfoAsync(watchId, CancellationToken.None))
            .ReturnsAsync((ChangeDetectionStatus.Ok,
                new ChangeDetectionWatchInfo("https://shop.example.com/brass", "Brass", null)));

        var result = await _gameService.UpdateGame(command);

        result.ChangeDetectionWatchId.Should().Be(watchId);
        result.ShopUrl.Should().Be("https://shop.example.com/brass");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _changeDetectionClientMock.Verify(x => x.GetWatchInfoAsync(watchId, CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldRejectWatchId_WhenWatchDoesNotExist()
    {
        var watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var command = new UpdateGameCommand
        {
            Id = 1,
            Title = "Brass",
            HasScoring = true,
            State = GameState.Wanted,
            ChangeDetectionWatchId = watchId
        };
        var existingGame = new Game("Brass", true) { Id = 1 };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingGame);
        _changeDetectionClientMock
            .Setup(x => x.GetWatchInfoAsync(watchId, CancellationToken.None))
            .ReturnsAsync((ChangeDetectionStatus.WatchNotFound, (ChangeDetectionWatchInfo?)null));

        var act = async () => await _gameService.UpdateGame(command);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.ChangeDetectionWatchNotFound);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _changeDetectionClientMock.Verify(x => x.GetWatchInfoAsync(watchId, CancellationToken.None), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldCreateWatchAndLinkIt()
    {
        var watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var existingGame = new Game("Brass", true) { Id = 1 };
        var detailedGame = new Game("Brass", true) { Id = 1 };
        detailedGame.AddExpansion(new Expansion("Brass: Expansion", 999, 1));

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingGame);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameByIdWithDetailsForReadSpec>(s => Targets(s, 1)), _token))
            .ReturnsAsync(detailedGame);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(_token)).ReturnsAsync(1);
        _changeDetectionClientMock
            .Setup(x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token))
            .ReturnsAsync((ChangeDetectionStatus.Ok, (string?)watchId));

        var result = await _gameService.CreateWatchForGame(1, " https://shop.example.com/brass ", _token);

        result.Should().BeSameAs(detailedGame);
        existingGame.ChangeDetectionWatchId.Should().Be(watchId);
        existingGame.ShopUrl.Should().Be("https://shop.example.com/brass");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.Verify(
            x => x.SingleOrDefaultAsync(It.Is<GameByIdWithDetailsForReadSpec>(s => s.IsSatisfiedBy(detailedGame)), _token),
            Times.Once);
        _changeDetectionClientMock.Verify(
            x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating changedetection.io watch for game 1");
        _loggerMock.VerifyLog(LogLevel.Information, $"Game 1 linked to changedetection.io watch {watchId}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldRemoveTheNewWatch_WhenSavingFails()
    {
        var watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var existingGame = new Game("Brass", true) { Id = 1 };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingGame);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(_token)).ThrowsAsync(new InvalidOperationException("db down"));
        _changeDetectionClientMock
            .Setup(x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token))
            .ReturnsAsync((ChangeDetectionStatus.Ok, (string?)watchId));
        _changeDetectionClientMock
            .Setup(x => x.DeleteWatchAsync(watchId, CancellationToken.None))
            .ReturnsAsync(ChangeDetectionStatus.Ok);

        var action = async () => await _gameService.CreateWatchForGame(1, "https://shop.example.com/brass", _token);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _changeDetectionClientMock.Verify(
            x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Once);
        _changeDetectionClientMock.Verify(x => x.DeleteWatchAsync(watchId, CancellationToken.None), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating changedetection.io watch for game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldThrow_WhenChangeDetectionNotConfigured()
    {
        var existingGame = new Game("Brass", true) { Id = 1 };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingGame);
        _changeDetectionClientMock
            .Setup(x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token))
            .ReturnsAsync((ChangeDetectionStatus.NotConfigured, (string?)null));

        var act = async () => await _gameService.CreateWatchForGame(1, "https://shop.example.com/brass", _token);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.ChangeDetectionNotConfigured);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _changeDetectionClientMock.Verify(
            x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating changedetection.io watch for game 1");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ChangeDetectionStatus.Unreachable, "e0808154-28da-4b85-9a71-24a409e694f1")]
    [InlineData(ChangeDetectionStatus.Unauthorized, null)]
    [InlineData(ChangeDetectionStatus.Ok, null)]
    public async Task CreateWatchForGame_ShouldThrowCreateWatchFailed_WhenChangeDetectionDoesNotReturnAWatch(
        ChangeDetectionStatus status, string? watchId)
    {
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Game("Brass", true) { Id = 1 });
        _changeDetectionClientMock
            .Setup(x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token))
            .ReturnsAsync((status, watchId));

        var act = async () => await _gameService.CreateWatchForGame(1, "https://shop.example.com/brass", _token);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.ChangeDetectionCreateWatchFailed);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _changeDetectionClientMock.Verify(
            x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating changedetection.io watch for game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldNotCreateASecondWatch_WhenRepointingTheLinkedOneFails()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var existingGame = new Game("Brass", true) { Id = 1 };
        existingGame.UpdateChangeDetectionWatchId(watchId);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingGame);
        _changeDetectionClientMock
            .Setup(x => x.UpdateWatchAsync(watchId, "https://shop.example.com/brass", "Brass", _token))
            .ReturnsAsync(ChangeDetectionStatus.Unreachable);

        var act = async () => await _gameService.CreateWatchForGame(1, "https://shop.example.com/brass", _token);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.ChangeDetectionCreateWatchFailed);
        existingGame.ShopUrl.Should().BeNull();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _changeDetectionClientMock.Verify(
            x => x.UpdateWatchAsync(watchId, "https://shop.example.com/brass", "Brass", _token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating changedetection.io watch for game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldRepointTheExistingWatch_InsteadOfCreatingAnother()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var existingGame = new Game("Brass", true) { Id = 1 };
        existingGame.UpdateChangeDetectionWatchId(watchId);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingGame);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameByIdWithDetailsForReadSpec>(s => Targets(s, 1)), _token))
            .ReturnsAsync(existingGame);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(_token)).ReturnsAsync(1);
        _changeDetectionClientMock
            .Setup(x => x.UpdateWatchAsync(watchId, "https://other-shop.example.com/brass", "Brass", _token))
            .ReturnsAsync(ChangeDetectionStatus.Ok);

        await _gameService.CreateWatchForGame(1, "https://other-shop.example.com/brass", _token);

        existingGame.ChangeDetectionWatchId.Should().Be(watchId);
        existingGame.ShopUrl.Should().Be("https://other-shop.example.com/brass");
        _changeDetectionClientMock.Verify(x => x.UpdateWatchAsync(watchId, "https://other-shop.example.com/brass", "Brass", _token), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.Verify(
            x => x.SingleOrDefaultAsync(It.Is<GameByIdWithDetailsForReadSpec>(s => Targets(s, 1)), _token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating changedetection.io watch for game 1");
        _loggerMock.VerifyLog(LogLevel.Information, $"Game 1 linked to changedetection.io watch {watchId}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldCreateANewWatch_WhenTheLinkedOneIsGone()
    {
        const string oldWatchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        const string newWatchId = "f1919265-39eb-5c96-a082-35b510f705a2";
        var existingGame = new Game("Brass", true) { Id = 1 };
        existingGame.UpdateChangeDetectionWatchId(oldWatchId);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingGame);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameByIdWithDetailsForReadSpec>(s => Targets(s, 1)), _token))
            .ReturnsAsync(existingGame);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(_token)).ReturnsAsync(1);
        _changeDetectionClientMock
            .Setup(x => x.UpdateWatchAsync(oldWatchId, "https://shop.example.com/brass", "Brass", _token))
            .ReturnsAsync(ChangeDetectionStatus.WatchNotFound);
        _changeDetectionClientMock
            .Setup(x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token))
            .ReturnsAsync((ChangeDetectionStatus.Ok, (string?)newWatchId));

        await _gameService.CreateWatchForGame(1, "https://shop.example.com/brass", _token);

        existingGame.ChangeDetectionWatchId.Should().Be(newWatchId);
        _changeDetectionClientMock.Verify(
            x => x.UpdateWatchAsync(oldWatchId, "https://shop.example.com/brass", "Brass", _token), Times.Once);
        _changeDetectionClientMock.Verify(
            x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.Verify(
            x => x.SingleOrDefaultAsync(It.Is<GameByIdWithDetailsForReadSpec>(s => Targets(s, 1)), _token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating changedetection.io watch for game 1");
        _loggerMock.VerifyLog(LogLevel.Information, $"Game 1 linked to changedetection.io watch {newWatchId}");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("http://192.168.1.1/admin")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://localhost:8080")]
    [InlineData("https://nas.local/")]
    public async Task CreateWatchForGame_ShouldRefuseLocalNetworkAddresses_WithoutCallingChangeDetection(string url)
    {
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Game("Brass", true) { Id = 1 });

        var act = async () => await _gameService.CreateWatchForGame(1, url, _token);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.ShopUrlNotPublic);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating changedetection.io watch for game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldRejectInvalidUrl_WithoutCallingChangeDetection()
    {
        var existingGame = new Game("Brass", true) { Id = 1 };
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingGame);

        var act = async () => await _gameService.CreateWatchForGame(1, "not a url", _token);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidShopUrl);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating changedetection.io watch for game 1");
        VerifyNoOtherCalls();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldDeleteGame_WhenGameExists()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };
        game.UpdateImage("game-image.png");

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _gameRepositoryMock
            .Setup(x => x.DeleteAsync(gameId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var manuals = new List<Manual>();
        _manualServiceMock
            .Setup(x => x.GetManualsForGame(gameId))
            .ReturnsAsync(manuals);

        await _gameService.Delete(gameId);

        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("game-image.png"), Times.Once);
        _manualServiceMock.Verify(x => x.GetManualsForGame(gameId), Times.Once);
        _manualServiceMock.Verify(x => x.DeleteManualFiles(manuals), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.DeleteAsync(gameId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Deleting game {gameId}");
        _loggerMock.VerifyLog(LogLevel.Information, $"Game {gameId} deleted");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ChangeDetectionStatus.Ok)]
    [InlineData(ChangeDetectionStatus.Unreachable)]
    public async Task Delete_ShouldRemoveThePriceWatch_WithoutFailingTheDelete(ChangeDetectionStatus status)
    {
        var watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var game = new Game("Brass") { Id = 1 };
        game.UpdateChangeDetectionWatchId(watchId);
        var manuals = new List<Manual>();

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);
        _gameRepositoryMock.Setup(x => x.DeleteAsync(1)).ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _manualServiceMock.Setup(x => x.GetManualsForGame(1)).ReturnsAsync(manuals);
        _changeDetectionClientMock.Setup(x => x.DeleteWatchAsync(watchId, CancellationToken.None)).ReturnsAsync(status);

        await _gameService.Delete(1);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync(null), Times.Once);
        _manualServiceMock.Verify(x => x.GetManualsForGame(1), Times.Once);
        _manualServiceMock.Verify(x => x.DeleteManualFiles(manuals), Times.Once);
        _changeDetectionClientMock.Verify(x => x.DeleteWatchAsync(watchId, CancellationToken.None), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Could not remove price watch {watchId}: {status}", status == ChangeDetectionStatus.Ok ? Times.Never() : Times.Once());
        _loggerMock.VerifyLog(LogLevel.Debug, $"Deleting game 1");
        _loggerMock.VerifyLog(LogLevel.Information, $"Game 1 deleted");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldThrowEntityNotFoundException_WhenGameDoesNotExist()
    {
        var gameId = 999;

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync((Game?)null);

        var action = async () => await _gameService.Delete(gameId);

        await action.Should().ThrowAsync<EntityNotFoundException>().Where(e => e.EntityType == nameof(Game) && (int)e.EntityId == gameId);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Deleting game {gameId}");
        VerifyNoOtherCalls();
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ShouldReturnGameCount()
    {
        _gameRepositoryMock
            .Setup(x => x.CountAsync(_token))
            .ReturnsAsync(42);

        var result = await _gameService.CountAsync(_token);

        result.Should().Be(42);

        _gameRepositoryMock.Verify(x => x.CountAsync(_token), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CreateGameFromCommand Tests

    [Fact]
    public async Task CreateGameFromCommand_ShouldCreateGame_WithAllProperties()
    {
        var command = new CreateGameCommand
        {
            Title = "New Game",
            HasScoring = true,
            HasCoopMode = true,
            CoopByDefault = true,
            State = GameState.Owned,
            YearPublished = 2020,
            Image = "image.png",
            ShopUrl = "https://shop.example.com/game",
            Language = "en",
            Description = "A great game",
            MinPlayers = 2,
            MaxPlayers = 4,
            MinPlayTime = 30,
            MaxPlayTime = 60,
            MinAge = 10,
            BggId = 12345,
            BuyingPrice = 49.99m,
            SoldPrice = 25.00m,
            Rating = 7.5,
            Weight = 2.8,
            AdditionDate = new DateTime(2023, 1, 15)
        };
        SetupAdditionDate(command.AdditionDate.Value);

        _gameRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Game>(g => g.Title == "New Game")))
            .ReturnsAsync((Game g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.CreateGameFromCommand(command);

        result.Should().NotBeNull();
        result.Title.Should().Be("New Game");
        result.HasScoring.Should().BeTrue();
        result.HasCoopMode.Should().BeTrue();
        result.CoopByDefault.Should().BeTrue();
        result.State.Should().Be(GameState.Owned);
        result.YearPublished.Should().Be(2020);
        result.Image.Should().Be("image.png");
        result.ShopUrl.Should().Be("https://shop.example.com/game");
        result.Language.Should().Be("en");
        result.Description.Should().Be("A great game");
        result.PlayerCount.Should().NotBeNull();
        result.PlayerCount!.Min.Should().Be(2);
        result.PlayerCount.Max.Should().Be(4);
        result.PlayTime.Should().NotBeNull();
        result.PlayTime!.MinMinutes.Should().Be(30);
        result.PlayTime.MaxMinutes.Should().Be(60);
        result.MinAge.Should().Be(10);
        result.BggId.Should().Be(12345);
        result.BuyingPrice.Should().NotBeNull();
        result.BuyingPrice!.Amount.Should().Be(49.99m);
        result.SoldPrice!.Amount.Should().Be(25.00m);
        result.Rating!.Value.Should().Be(7.5);
        result.Weight!.Value.Should().Be(2.8);
        result.AdditionDate.Should().Be(new DateTime(2023, 1, 15));
        VerifyAdditionDateChecked(command.AdditionDate.Value);

        _gameRepositoryMock.Verify(x => x.CreateAsync(It.Is<Game>(g => g.Title == "New Game")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating game New Game");
        _loggerMock.VerifyLog(LogLevel.Information, $"Game 0 (New Game) created");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateGameFromCommand_ShouldCreateGame_WithMinimalProperties()
    {
        var command = new CreateGameCommand
        {
            Title = "Simple Game",
            HasScoring = false,
            State = GameState.Wanted
        };

        _gameRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Game>(g => g.Title == "Simple Game")))
            .ReturnsAsync((Game g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.CreateGameFromCommand(command);

        result.Should().NotBeNull();
        result.Title.Should().Be("Simple Game");
        result.HasScoring.Should().BeFalse();
        result.State.Should().Be(GameState.Wanted);

        _gameRepositoryMock.Verify(x => x.CreateAsync(It.Is<Game>(g => g.Title == "Simple Game")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating game Simple Game");
        _loggerMock.VerifyLog(LogLevel.Information, $"Game 0 (Simple Game) created");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateGameFromCommand_ShouldKeepTheCreationTime_WhenAdditionDateIsToday()
    {
        var command = new CreateGameCommand
        {
            Title = "Fresh Game",
            State = GameState.Owned,
            AdditionDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc)
        };
        SetupAdditionDate(command.AdditionDate.Value);

        _gameRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Game>(g => g.Title == "Fresh Game")))
            .ReturnsAsync((Game g) => g);

        var result = await _gameService.CreateGameFromCommand(command);

        result.AdditionDate.Should().NotBe(command.AdditionDate);
        result.AdditionDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        VerifyAdditionDateChecked(command.AdditionDate.Value);

        _gameRepositoryMock.Verify(x => x.CreateAsync(It.Is<Game>(g => g.Title == "Fresh Game")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating game Fresh Game");
        _loggerMock.VerifyLog(LogLevel.Information, $"Game 0 (Fresh Game) created");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateGameFromCommand_ShouldUseTheGivenDate_WhenAdditionDateIsAnotherDay()
    {
        var additionDate = new DateTime(2026, 10, 9, 0, 0, 0, DateTimeKind.Utc);
        var command = new CreateGameCommand
        {
            Title = "Older Game",
            State = GameState.Owned,
            AdditionDate = additionDate
        };
        SetupAdditionDate(additionDate);

        _gameRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Game>(g => g.Title == "Older Game")))
            .ReturnsAsync((Game g) => g);

        var result = await _gameService.CreateGameFromCommand(command);

        result.AdditionDate.Should().Be(additionDate);
        VerifyAdditionDateChecked(additionDate);

        _gameRepositoryMock.Verify(x => x.CreateAsync(It.Is<Game>(g => g.Title == "Older Game")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating game Older Game");
        _loggerMock.VerifyLog(LogLevel.Information, $"Game 0 (Older Game) created");
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateGame Tests

    [Fact]
    public async Task UpdateGame_ShouldUpdateGame_AndSaveChanges()
    {
        var command = new UpdateGameCommand
        {
            Id = 1,
            Title = "Updated Game",
            HasScoring = true,
            HasCoopMode = false,
            CoopByDefault = true,
            State = GameState.Owned,
            Description = "Updated description",
            ShopUrl = "https://shop.example.com/updated",
            Language = "nl",
            BuyingPrice = 39.99m,
            SoldPrice = 25.00m,
            Rating = 7.5,
            Weight = 2.8
        };

        var existingGame = new Game("Original Game", false) { Id = 1 };
        existingGame.UpdateCoopMode(true, true);

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existingGame);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGame(command);

        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Game");
        result.HasScoring.Should().BeTrue();
        result.HasCoopMode.Should().BeFalse();
        result.CoopByDefault.Should().BeFalse();
        result.State.Should().Be(GameState.Owned);
        result.Description.Should().Be("Updated description");
        result.ShopUrl.Should().BeNull();
        result.Language.Should().Be("nl");
        result.BuyingPrice.Should().NotBeNull();
        result.BuyingPrice!.Amount.Should().Be(39.99m);
        result.SoldPrice.Should().NotBeNull();
        result.SoldPrice!.Amount.Should().Be(25.00m);
        result.Rating.Should().NotBeNull();
        result.Rating!.Value.Should().Be(7.5);
        result.Weight.Should().NotBeNull();
        result.Weight!.Value.Should().Be(2.8);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldDeleteThePreviousImage_WhenItChanges()
    {
        var command = new UpdateGameCommand
        {
            Id = 1,
            Title = "Updated Game",
            HasScoring = false,
            State = GameState.Owned,
            Image = "/images/cover/new.webp"
        };

        var existingGame = new Game("Original Game", false) { Id = 1 };
        existingGame.UpdateImage("/images/cover/old.webp");

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existingGame);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGame(command);

        result.Image.Should().Be("/images/cover/new.webp");

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("/images/cover/old.webp"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldRemoveTheNewImage_WhenSavingFails()
    {
        var command = new UpdateGameCommand
        {
            Id = 1,
            Title = "Updated Game",
            HasScoring = false,
            State = GameState.Owned,
            Image = "/images/cover/new.webp"
        };
        var existingGame = new Game("Original Game", false) { Id = 1 };
        existingGame.UpdateImage("/images/cover/old.webp");

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingGame);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ThrowsAsync(new InvalidOperationException("db down"));

        var action = async () => await _gameService.UpdateGame(command);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("/images/cover/new.webp"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateGameFromCommand_ShouldRemoveTheUploadedImage_WhenSavingFails()
    {
        var command = new CreateGameCommand
        {
            Title = "Simple Game",
            HasScoring = false,
            State = GameState.Wanted,
            Image = "/images/cover/new.webp"
        };

        _gameRepositoryMock.Setup(x => x.CreateAsync(It.Is<Game>(g => g.Title == "Simple Game"))).ReturnsAsync((Game g) => g);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ThrowsAsync(new InvalidOperationException("db down"));

        var action = async () => await _gameService.CreateGameFromCommand(command);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        _gameRepositoryMock.Verify(x => x.CreateAsync(It.Is<Game>(g => g.Title == "Simple Game")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("/images/cover/new.webp"), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Creating game Simple Game");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldThrowEntityNotFoundException_WhenGameDoesNotExist()
    {
        var command = new UpdateGameCommand
        {
            Id = 999,
            Title = "Non-existent",
            HasScoring = false,
            State = GameState.Owned
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Game?)null);

        var action = async () => await _gameService.UpdateGame(command);

        await action.Should().ThrowAsync<EntityNotFoundException>();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(999), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating game 999");
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetGameExpansions Tests

    [Fact]
    public async Task GetGameExpansions_ShouldReturnExpansions()
    {
        var expansionIds = new List<int> { 1, 2, 3 };
        var expansions = new List<Expansion>
        {
            new Expansion("Expansion 1", 100, 1) { Id = 1 },
            new Expansion("Expansion 2", 101, 1) { Id = 2 },
            new Expansion("Expansion 3", 102, 1) { Id = 3 }
        };

        _gameRepositoryMock
            .Setup(x => x.GetExpansions(1, expansionIds))
            .ReturnsAsync(expansions);

        var result = await _gameService.GetGameExpansions(1, expansionIds);

        result.Should().BeSameAs(expansions);

        _gameRepositoryMock.Verify(x => x.GetExpansions(1, expansionIds), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameExpansions_ShouldThrowValidationException_WhenAnExpansionDoesNotBelongToTheGame()
    {
        var expansionIds = new List<int> { 1, 2 };
        _gameRepositoryMock
            .Setup(x => x.GetExpansions(1, expansionIds))
            .ReturnsAsync([new Expansion("Expansion 1", 100, 1) { Id = 1 }]);

        var act = () => _gameService.GetGameExpansions(1, expansionIds);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidExpansion);
        _gameRepositoryMock.Verify(x => x.GetExpansions(1, expansionIds), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region DeleteExpansion Tests

    [Fact]
    public async Task DeleteExpansion_ShouldDeleteExpansion_AndSaveChanges()
    {
        var gameId = 1;
        var expansionId = 5;

        _gameRepositoryMock
            .Setup(x => x.DeleteExpansion(gameId, expansionId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _gameService.DeleteExpansion(gameId, expansionId);

        _gameRepositoryMock.Verify(x => x.DeleteExpansion(gameId, expansionId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Deleting expansion {expansionId} from game {gameId}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteExpansion_ShouldThrowEntityNotFoundException_WhenExpansionDoesNotExist()
    {
        _gameRepositoryMock
            .Setup(x => x.DeleteExpansion(1, 5))
            .ReturnsAsync(false);

        var act = () => _gameService.DeleteExpansion(1, 5);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _gameRepositoryMock.Verify(x => x.DeleteExpansion(1, 5), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Deleting expansion 5 from game 1");
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetSessionsForGame Tests

    [Fact]
    public async Task GetSessionsForGame_ShouldReturnSessions()
    {
        var gameId = 1;
        var count = 10;
        var sessions = new List<Session>
        {
            new Session(gameId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1).AddHours(2), "Session 1"),
            new Session(gameId, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2).AddHours(3), "Session 2")
        };
        var otherGameSession = new Session(2, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, "Other game");

        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<SessionsByGameSpec>(s => s.Take == count && s.IsSatisfiedBy(sessions[0]) && !s.IsSatisfiedBy(otherGameSession)), CancellationToken.None))
            .ReturnsAsync(sessions);

        var result = await _gameService.GetSessionsForGame(gameId, count);

        result.Should().BeSameAs(sessions);

        _sessionRepositoryMock.Verify(
            x => x.ListAsync(
                It.Is<SessionsByGameSpec>(s => s.Take == count && s.IsSatisfiedBy(sessions[0]) && !s.IsSatisfiedBy(otherGameSession)),
                CancellationToken.None),
            Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Fetching sessions for game {gameId}");
        VerifyNoOtherCalls();
    }

    #endregion

    #region SearchExpansionsForGame Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SearchExpansionsForGame_ShouldThrowBggFeatureDisabledException_WhenApiKeyIsMissing(string? apiKey)
    {
        _settingsServiceMock
            .Setup(x => x.GetBggApiKeyAsync())
            .ReturnsAsync(apiKey);

        var action = async () => await _gameService.SearchExpansionsForGame(1);

        await action.Should().ThrowAsync<BggFeatureDisabledException>();

        _loggerMock.VerifyLog(LogLevel.Debug, $"Searching expansions for game 1");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldReturnEmptyArray_WhenGameDoesNotExist()
    {
        var gameId = 999;

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync((Game?)null);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Searching expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldReturnEmptyArray_WhenGameHasNoBggId()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Searching expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldReturnEmptyArray_WhenBggApiCallFails()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };
        game.UpdateBggId(12345);

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))))
            .ReturnsAsync(CreateFailedThingResponse());

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Searching expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldReturnEmptyArray_WhenBggApiReturnsEmptyResult()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };
        game.UpdateBggId(12345);

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))))
            .ReturnsAsync(CreateSucceededThingResponse([]));

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Searching expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldReturnExpansions_WhenBggApiReturnsValidGame()
    {
        var gameId = 1;
        var bggId = 12345;
        var game = new Game("Test Game") { Id = gameId };
        game.UpdateBggId(bggId);

        var expansionLinks = new[]
        {
            new ThingResponse.Link { Type = Constants.Bgg.Expansion, Id = 101, Value = "Expansion One" },
            new ThingResponse.Link { Type = Constants.Bgg.Expansion, Id = 102, Value = "Expansion Two" }
        };
        var thingItem = CreateThingItemWithExpansionLinks(bggId, "Test Game", expansionLinks);
        var thingResponse = CreateSucceededThingResponse([thingItem]);

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, bggId))))
            .ReturnsAsync(thingResponse);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEquivalentTo(new[]
        {
            new ExpansionData { Title = "Expansion One", BggId = 101 },
            new ExpansionData { Title = "Expansion Two", BggId = 102 }
        });

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, bggId))), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Searching expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldExcludeNonExpansionLinks_WhenBggApiReturnsMultipleLinkTypes()
    {
        var gameId = 1;
        var bggId = 12345;
        var game = new Game("Test Game") { Id = gameId };
        game.UpdateBggId(bggId);

        var links = new[]
        {
            new ThingResponse.Link { Type = Constants.Bgg.Expansion, Id = 101, Value = "Expansion One" },
            new ThingResponse.Link { Type = Constants.Bgg.Designer, Id = 200, Value = "Some Designer" },
            new ThingResponse.Link { Type = Constants.Bgg.Publisher, Id = 300, Value = "Some Publisher" }
        };
        var thingItem = CreateThingItemWithExpansionLinks(bggId, "Test Game", links);
        var thingResponse = CreateSucceededThingResponse([thingItem]);

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, bggId))))
            .ReturnsAsync(thingResponse);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEquivalentTo(new[] { new ExpansionData { Title = "Expansion One", BggId = 101 } });

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, bggId))), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Searching expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldExcludeExpansionLinks_WithBlankTitle()
    {
        var gameId = 1;
        var bggId = 12345;
        var game = new Game("Test Game") { Id = gameId };
        game.UpdateBggId(bggId);

        var links = new[]
        {
            new ThingResponse.Link { Type = Constants.Bgg.Expansion, Id = 101, Value = "Expansion One" },
            new ThingResponse.Link { Type = Constants.Bgg.Expansion, Id = 102, Value = " " },
            new ThingResponse.Link { Type = Constants.Bgg.Expansion, Id = 103, Value = "" }
        };
        var thingItem = CreateThingItemWithExpansionLinks(bggId, "Test Game", links);
        var thingResponse = CreateSucceededThingResponse([thingItem]);

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, bggId))))
            .ReturnsAsync(thingResponse);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEquivalentTo(new[] { new ExpansionData { Title = "Expansion One", BggId = 101 } });

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, bggId))), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Searching expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldReturnEmptyArray_WhenAllLinksAreNonExpansionType()
    {
        var gameId = 1;
        var bggId = 12345;
        var game = new Game("Test Game") { Id = gameId };
        game.UpdateBggId(bggId);

        var links = new[]
        {
            new ThingResponse.Link { Type = Constants.Bgg.Designer, Id = 200, Value = "Some Designer" },
            new ThingResponse.Link { Type = Constants.Bgg.Publisher, Id = 300, Value = "Some Publisher" }
        };
        var thingItem = CreateThingItemWithExpansionLinks(bggId, "Test Game", links);
        var thingResponse = CreateSucceededThingResponse([thingItem]);

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, bggId))))
            .ReturnsAsync(thingResponse);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, bggId))), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Searching expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateGameExpansions Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task UpdateGameExpansions_ShouldThrowBggFeatureDisabledException_WhenApiKeyIsMissing(string? apiKey)
    {
        _settingsServiceMock
            .Setup(x => x.GetBggApiKeyAsync())
            .ReturnsAsync(apiKey);

        var action = async () => await _gameService.UpdateGameExpansions(1, [100]);

        await action.Should().ThrowAsync<BggFeatureDisabledException>();

        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldThrowArgumentNullException_WhenExpansionIdsIsNull()
    {
        var action = async () => await _gameService.UpdateGameExpansions(1, null!);

        await action.Should().ThrowAsync<ArgumentNullException>().WithParameterName("expansionIds");

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldThrowEntityNotFoundException_WhenGameDoesNotExist()
    {
        var gameId = 999;

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync((Game?)null);

        var act = () => _gameService.UpdateGameExpansions(gameId, [100]);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldRemoveExpansions_ThatAreNotInProvidedIds()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };
        var existingExpansion = new Expansion("Old Expansion", 200, gameId);
        game.AddExpansion(existingExpansion);

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, []);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldAddNewExpansions_WhenBggApiReturnsValidData()
    {
        var gameId = 1;
        var expansionBggId = 101;
        var game = new Game("Test Game") { Id = gameId };

        var thingItem = CreateThingItem(expansionBggId, "New Expansion");
        var thingResponse = CreateSucceededThingResponse([thingItem]);

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, expansionBggId))))
            .ReturnsAsync(thingResponse);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, [expansionBggId]);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("New Expansion");
        result[0].BggId.Should().Be(expansionBggId);

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, expansionBggId))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldSkipExpansion_WhenBggApiReturnsEmptyResult()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101))))
            .ReturnsAsync(CreateSucceededThingResponse([]));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, [101]);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldNotCallBggApi_ForExpansionsThatAlreadyExist()
    {
        var gameId = 1;
        var existingBggId = 200;
        var game = new Game("Test Game") { Id = gameId };
        var existingExpansion = new Expansion("Existing Expansion", existingBggId, gameId);
        game.AddExpansion(existingExpansion);

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, [existingBggId]);

        result.Should().HaveCount(1);
        result[0].BggId.Should().Be(existingBggId);

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldFetchNewExpansionsInOneBggRequest_AndSaveOnce()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };

        var item1 = CreateThingItem(101, "Expansion One");
        var item2 = CreateThingItem(102, "Expansion Two");

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101, 102))))
            .ReturnsAsync(CreateSucceededThingResponse([item1, item2]));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, [101, 102]);

        result.Select(x => (x.Title, x.BggId)).Should().Equal(("Expansion One", 101), ("Expansion Two", 102));

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101, 102))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldSkipMalformedBggResults()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101, 102))))
            .ReturnsAsync(CreateSucceededThingResponse([CreateThingItem(101, " "), CreateThingItem(0, "Zero Id Expansion")]));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, [101, 102]);

        result.Should().BeEmpty();

        _loggerMock.VerifyLog(LogLevel.Warning, "Skipping malformed BGG expansion (id 101) for game 1");
        _loggerMock.VerifyLog(LogLevel.Warning, "Skipping malformed BGG expansion (id 0) for game 1");
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101, 102))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldKeepManualExpansions_WhenSyncingBggSelections()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };
        game.AddExpansion(new Expansion("Homemade promo", null, gameId));
        game.AddExpansion(new Expansion("Old Expansion", 200, gameId));

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, []);

        result.Should().ContainSingle().Which.Title.Should().Be("Homemade promo");

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating expansions for game {gameId}");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    #endregion

    #region AddManualExpansion

    [Fact]
    public async Task AddManualExpansion_ShouldAddATrimmedExpansionWithoutBggId()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _gameService.AddManualExpansion(gameId, "  Kickstarter promo pack ");

        result.Title.Should().Be("Kickstarter promo pack");
        result.BggId.Should().BeNull();
        game.Expansions.Should().ContainSingle().Which.Should().BeSameAs(result);

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Adding manual expansion   Kickstarter promo pack  to game {gameId}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddManualExpansion_ShouldRefuseADuplicateTitle_IgnoringCase()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };
        game.AddExpansion(new Expansion("Promo pack", null, gameId));
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);

        var act = () => _gameService.AddManualExpansion(gameId, "promo PACK");

        await act.Should().ThrowAsync<DomainException>().WithMessage(BoardGameTracker.Common.Constants.Errors.ExpansionAlreadyExists);

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, gameId)), CancellationToken.None), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Adding manual expansion promo PACK to game {gameId}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddManualExpansion_ShouldThrowEntityNotFoundException_WhenGameDoesNotExist()
    {
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, 99)), CancellationToken.None))
            .ReturnsAsync((Game?)null);

        var act = () => _gameService.AddManualExpansion(99, "Promo");

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, 99)), CancellationToken.None), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Adding manual expansion Promo to game 99");
        VerifyNoOtherCalls();
    }

    #endregion

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistsAsync_ShouldAskTheRepositoryForThatGame(bool exists)
    {
        var game = new Game("Brass") { Id = 5 };
        _gameRepositoryMock
            .Setup(x => x.AnyAsync(It.Is<GameByIdSpec>(s => Targets(s, 5)), CancellationToken.None))
            .ReturnsAsync(exists);

        var result = await _gameService.ExistsAsync(5);

        result.Should().Be(exists);
        _gameRepositoryMock.Verify(
            x => x.AnyAsync(It.Is<GameByIdSpec>(spec => spec.IsSatisfiedBy(game)), CancellationToken.None),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public async Task HasCoopModeAsync_ShouldAskTheRepositoryForThatGame(bool? hasCoopMode)
    {
        _gameRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.Is<GameHasCoopModeSpec>(s => Targets(s, 5)), CancellationToken.None))
            .ReturnsAsync(hasCoopMode);

        var result = await _gameService.HasCoopModeAsync(5);

        result.Should().Be(hasCoopMode);
        _gameRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.Is<GameHasCoopModeSpec>(s => Targets(s, 5)), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(ValidationException), "BGG API key is invalid or expired")]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(BggRateLimitException), "BGG rate-limited the expansion request for game 1")]
    public async Task UpdateGameExpansions_ShouldTranslateBggErrors_WithoutSaving(HttpStatusCode status, Type expected, string expectedLog)
    {
        var failure = new BoardGameGeekHttpException("BGG refused", status);
        var game = new Game("Test Game") { Id = 1 };
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, 1)), CancellationToken.None))
            .ReturnsAsync(game);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101))))
            .ThrowsAsync(failure);

        var act = () => _gameService.UpdateGameExpansions(1, [101]);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeOfType(expected);
        _loggerMock.VerifyLog(LogLevel.Warning, expectedLog, failure);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, 1)), CancellationToken.None), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101))), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Updating expansions for game 1");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayStatsPerGame_ShouldReturnTheStatsOfTheRepository()
    {
        var stats = new Dictionary<int, GamePlayStats> { [1] = new(3, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)) };
        _gameRepositoryMock.Setup(x => x.GetPlayStatsPerGame()).ReturnsAsync(stats);

        var result = await _gameService.GetPlayStatsPerGame();

        result.Should().BeSameAs(stats);
        _gameRepositoryMock.Verify(x => x.GetPlayStatsPerGame(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGamePriceAsync_ShouldForwardTheForceRefresh()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        _gameRepositoryMock.Setup(x => x.GetWatchInfo(1)).ReturnsAsync(new GameWatchInfo(1, watchId));
        _changeDetectionClientMock
            .Setup(x => x.GetLatestAsync(watchId, true, _token))
            .ReturnsAsync(ChangeDetectionResult.Unavailable(ChangeDetectionStatus.Unreachable));

        var result = await _gameService.GetGamePriceAsync(1, true, _token);

        result!.Available.Should().BeFalse();
        result.Status.Should().Be(ChangeDetectionStatus.Unreachable);
        result.WatchId.Should().Be(watchId);
        _gameRepositoryMock.Verify(x => x.GetWatchInfo(1), Times.Once);
        _changeDetectionClientMock.Verify(x => x.GetLatestAsync(watchId, true, _token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Fetching price for game 1");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetGamePriceAsync_ShouldReportNotConfigured_WhenTheWatchIdIsBlank(string watchId)
    {
        _gameRepositoryMock.Setup(x => x.GetWatchInfo(1)).ReturnsAsync(new GameWatchInfo(1, watchId));

        var result = await _gameService.GetGamePriceAsync(1, false, _token);

        result!.Status.Should().Be(ChangeDetectionStatus.NotConfigured);
        _gameRepositoryMock.Verify(x => x.GetWatchInfo(1), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Fetching price for game 1");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("ftp://shop.example.com/brass")]
    [InlineData("  ")]
    public async Task CreateWatchForGame_ShouldRejectAnythingButAHttpUrl(string? url)
    {
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Game("Brass", true) { Id = 1 });

        var act = () => _gameService.CreateWatchForGame(1, url!, _token);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidShopUrl);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Creating changedetection.io watch for game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldThrow_WhenTheGameDoesNotExist()
    {
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(9)).ReturnsAsync((Game?)null);

        var act = () => _gameService.CreateWatchForGame(9, "https://shop.example.com/brass", _token);

        await act.Should().ThrowAsync<EntityNotFoundException>().Where(e => e.EntityType == nameof(Game) && (int)e.EntityId == 9);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(9), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Creating changedetection.io watch for game 9");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldKeepTheLinkedWatch_WhenSavingFails()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var game = new Game("Brass", true) { Id = 1 };
        game.UpdateChangeDetectionWatchId(watchId);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);
        _changeDetectionClientMock
            .Setup(x => x.UpdateWatchAsync(watchId, "https://shop.example.com/brass", "Brass", _token))
            .ReturnsAsync(ChangeDetectionStatus.Ok);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(_token)).ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _gameService.CreateWatchForGame(1, "https://shop.example.com/brass", _token);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _changeDetectionClientMock.Verify(x => x.UpdateWatchAsync(watchId, "https://shop.example.com/brass", "Brass", _token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Creating changedetection.io watch for game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldLogAWarning_WhenRemovingTheNewWatchFailsToo()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var failure = new HttpRequestException("gone");
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Game("Brass", true) { Id = 1 });
        _changeDetectionClientMock
            .Setup(x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token))
            .ReturnsAsync((ChangeDetectionStatus.Ok, (string?)watchId));
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(_token)).ThrowsAsync(new InvalidOperationException("db down"));
        _changeDetectionClientMock.Setup(x => x.DeleteWatchAsync(watchId, CancellationToken.None)).ThrowsAsync(failure);

        var act = () => _gameService.CreateWatchForGame(1, "https://shop.example.com/brass", _token);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _changeDetectionClientMock.Verify(x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Once);
        _changeDetectionClientMock.Verify(x => x.DeleteWatchAsync(watchId, CancellationToken.None), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Creating changedetection.io watch for game 1");
        _loggerMock.VerifyLog(LogLevel.Warning, $"Could not remove price watch {watchId}", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldReturnTheUpdatedGame_WhenTheReloadFindsNothing()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var game = new Game("Brass", true) { Id = 1 };
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameByIdWithDetailsForReadSpec>(s => Targets(s, 1)), _token))
            .ReturnsAsync((Game?)null);
        _changeDetectionClientMock
            .Setup(x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token))
            .ReturnsAsync((ChangeDetectionStatus.Ok, (string?)watchId));

        var result = await _gameService.CreateWatchForGame(1, "https://shop.example.com/brass", _token);

        result.Should().BeSameAs(game);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameByIdWithDetailsForReadSpec>(s => Targets(s, 1)), _token), Times.Once);
        _changeDetectionClientMock.Verify(x => x.CreateWatchAsync("https://shop.example.com/brass", "Brass", _token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Creating changedetection.io watch for game 1");
        _loggerMock.VerifyLog(LogLevel.Information, $"Game 1 linked to changedetection.io watch {watchId}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchForGame_ShouldSayNotConfigured_WhenRepointingFindsNoConfiguration()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var game = new Game("Brass", true) { Id = 1 };
        game.UpdateChangeDetectionWatchId(watchId);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);
        _changeDetectionClientMock
            .Setup(x => x.UpdateWatchAsync(watchId, "https://shop.example.com/brass", "Brass", _token))
            .ReturnsAsync(ChangeDetectionStatus.NotConfigured);

        var act = () => _gameService.CreateWatchForGame(1, "https://shop.example.com/brass", _token);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.ChangeDetectionNotConfigured);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _changeDetectionClientMock.Verify(x => x.UpdateWatchAsync(watchId, "https://shop.example.com/brass", "Brass", _token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Creating changedetection.io watch for game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldKeepTheDelete_WhenRemovingThePriceWatchThrows()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var failure = new HttpRequestException("unreachable");
        var game = new Game("Brass") { Id = 1 };
        game.UpdateChangeDetectionWatchId(watchId);
        var manuals = new List<Manual>();
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);
        _manualServiceMock.Setup(x => x.GetManualsForGame(1)).ReturnsAsync(manuals);
        _changeDetectionClientMock.Setup(x => x.DeleteWatchAsync(watchId, CancellationToken.None)).ThrowsAsync(failure);

        await _gameService.Delete(1);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync(null), Times.Once);
        _manualServiceMock.Verify(x => x.GetManualsForGame(1), Times.Once);
        _manualServiceMock.Verify(x => x.DeleteManualFiles(manuals), Times.Once);
        _changeDetectionClientMock.Verify(x => x.DeleteWatchAsync(watchId, CancellationToken.None), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Deleting game 1");
        _loggerMock.VerifyLog(LogLevel.Warning, $"Could not remove price watch {watchId}", failure);
        _loggerMock.VerifyLog(LogLevel.Information, "Game 1 deleted");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldKeepTheFiles_WhenSavingFails()
    {
        var game = new Game("Brass") { Id = 1 };
        game.UpdateImage("/images/cover/brass.webp");
        var manuals = new List<Manual>();
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);
        _manualServiceMock.Setup(x => x.GetManualsForGame(1)).ReturnsAsync(manuals);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _gameService.Delete(1);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Once);
        _manualServiceMock.Verify(x => x.GetManualsForGame(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Deleting game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateGameFromCommand_ShouldTakeTheShopUrlFromTheWatch_WhenAWatchIsLinked()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var command = new CreateGameCommand { Title = "Brass", State = GameState.Wanted, ShopUrl = "https://old.example.com/brass", ChangeDetectionWatchId = watchId };
        _gameRepositoryMock.Setup(x => x.CreateAsync(It.Is<Game>(g => g.Title == "Brass"))).ReturnsAsync((Game g) => g);
        _changeDetectionClientMock
            .Setup(x => x.GetWatchInfoAsync(watchId, CancellationToken.None))
            .ReturnsAsync((ChangeDetectionStatus.Ok, new ChangeDetectionWatchInfo("https://shop.example.com/brass", "Brass", null)));

        var result = await _gameService.CreateGameFromCommand(command);

        result.ShopUrl.Should().Be("https://shop.example.com/brass");
        result.ChangeDetectionWatchId.Should().Be(watchId);
        _gameRepositoryMock.Verify(x => x.CreateAsync(It.Is<Game>(g => g.Title == "Brass")), Times.Once);
        _changeDetectionClientMock.Verify(x => x.GetWatchInfoAsync(watchId, CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Creating game Brass");
        _loggerMock.VerifyLog(LogLevel.Information, "Game 0 (Brass) created");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateGameFromCommand_ShouldNotCreateTheGame_WhenTheWatchDoesNotExist()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var command = new CreateGameCommand { Title = "Brass", State = GameState.Wanted, ChangeDetectionWatchId = watchId };
        _changeDetectionClientMock
            .Setup(x => x.GetWatchInfoAsync(watchId, CancellationToken.None))
            .ReturnsAsync((ChangeDetectionStatus.WatchNotFound, (ChangeDetectionWatchInfo?)null));

        var act = () => _gameService.CreateGameFromCommand(command);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.ChangeDetectionWatchNotFound);
        _changeDetectionClientMock.Verify(x => x.GetWatchInfoAsync(watchId, CancellationToken.None), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Creating game Brass");
        VerifyNoOtherCalls();
    }

    public static TheoryData<ChangeDetectionStatus, string?> UnusableWatchInfo => new()
    {
        { ChangeDetectionStatus.Unreachable, "https://shop.example.com/brass" },
        { ChangeDetectionStatus.Ok, null },
        { ChangeDetectionStatus.Ok, "ftp://shop.example.com/brass" },
        { ChangeDetectionStatus.Ok, "not a url" }
    };

    [Theory]
    [MemberData(nameof(UnusableWatchInfo))]
    public async Task UpdateGame_ShouldKeepTheCurrentShopUrl_WhenTheWatchGivesNoUsableUrl(ChangeDetectionStatus status, string? url)
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var game = new Game("Brass") { Id = 1 };
        game.UpdateShopUrl("https://old.example.com/brass");
        var command = new UpdateGameCommand { Id = 1, Title = "Brass", State = GameState.Wanted, ChangeDetectionWatchId = watchId };
        var info = url == null ? null : new ChangeDetectionWatchInfo(url, "Brass", null);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);
        _changeDetectionClientMock.Setup(x => x.GetWatchInfoAsync(watchId, CancellationToken.None)).ReturnsAsync((status, info));

        var result = await _gameService.UpdateGame(command);

        result.ShopUrl.Should().Be("https://old.example.com/brass");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _changeDetectionClientMock.Verify(x => x.GetWatchInfoAsync(watchId, CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldNotAskForTheWatch_WhenTheWatchStaysTheSame()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var game = new Game("Brass") { Id = 1 };
        game.UpdateChangeDetectionWatchId(watchId);
        var command = new UpdateGameCommand { Id = 1, Title = "Brass", State = GameState.Wanted, ChangeDetectionWatchId = watchId };
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _gameService.UpdateGame(command);

        result.ChangeDetectionWatchId.Should().Be(watchId);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldNotDeleteAnImage_WhenTheImageStaysTheSame()
    {
        var game = new Game("Brass") { Id = 1 };
        game.UpdateImage("/images/cover/same.webp");
        var command = new UpdateGameCommand { Id = 1, Title = "Brass", State = GameState.Owned, Image = "/images/cover/same.webp" };
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _gameService.UpdateGame(command);

        result.Image.Should().Be("/images/cover/same.webp");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldNotDeleteAnImage_WhenTheGameHadNoImage()
    {
        var game = new Game("Brass") { Id = 1 };
        var command = new UpdateGameCommand { Id = 1, Title = "Brass", State = GameState.Owned, Image = "/images/cover/new.webp" };
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _gameService.UpdateGame(command);

        result.Image.Should().Be("/images/cover/new.webp");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldKeepTheSharedImage_WhenSavingFailsWithoutAnImageChange()
    {
        var game = new Game("Brass") { Id = 1 };
        game.UpdateImage("/images/cover/same.webp");
        var command = new UpdateGameCommand { Id = 1, Title = "Brass", State = GameState.Owned, Image = "/images/cover/same.webp" };
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _gameService.UpdateGame(command);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldWriteEveryFieldOfTheCommand()
    {
        var additionDate = new DateTime(2021, 3, 4, 0, 0, 0, DateTimeKind.Utc);
        var game = new Game("Brass") { Id = 1 };
        var command = new UpdateGameCommand
        {
            Id = 1,
            Title = "Brass",
            State = GameState.Sold,
            YearPublished = 2018,
            MinPlayers = 2,
            MaxPlayers = 4,
            MinPlayTime = 60,
            MaxPlayTime = 120,
            MinAge = 14,
            BggId = 224517,
            AdditionDate = additionDate
        };
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _gameService.UpdateGame(command);

        result.State.Should().Be(GameState.Sold);
        result.YearPublished.Should().Be(2018);
        result.PlayerCount!.Min.Should().Be(2);
        result.PlayerCount.Max.Should().Be(4);
        result.PlayTime!.MinMinutes.Should().Be(60);
        result.PlayTime.MaxMinutes.Should().Be(120);
        result.MinAge.Should().Be(14);
        result.BggId.Should().Be(224517);
        result.AdditionDate.Should().Be(additionDate);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldKeepTheAdditionDateAndAnEmptyDescription_WhenTheCommandHasNone()
    {
        var game = new Game("Brass") { Id = 1 };
        game.UpdateDescription("Old");
        var added = new DateTime(2021, 3, 4, 0, 0, 0, DateTimeKind.Utc);
        game.UpdateAdditionDate(added);
        var command = new UpdateGameCommand { Id = 1, Title = "Brass", State = GameState.Owned };
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);

        var result = await _gameService.UpdateGame(command);

        result.AdditionDate.Should().Be(added);
        result.Description.Should().BeEmpty();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Updating game 1");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldReturnEmptyArray_WhenBggSendsNoLinks()
    {
        var game = new Game("Test Game") { Id = 1 };
        game.UpdateBggId(12345);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(game);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))))
            .ReturnsAsync(CreateSucceededThingResponse([CreateThingItem(12345, "Test Game")]));

        var result = await _gameService.SearchExpansionsForGame(1);

        result.Should().BeEmpty();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Searching expansions for game 1");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldAskBggInBatchesOfTwenty()
    {
        var game = new Game("Test Game") { Id = 1 };
        var firstBatch = Enumerable.Range(1, 20).ToArray();
        _gameRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, 1)), CancellationToken.None)).ReturnsAsync(game);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, firstBatch))))
            .ReturnsAsync(CreateSucceededThingResponse(firstBatch.Select(id => CreateThingItem(id, $"Expansion {id}"))));
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 21))))
            .ReturnsAsync(CreateFailedThingResponse());

        var result = await _gameService.UpdateGameExpansions(1, [.. firstBatch, 21]);

        result.Select(x => x.BggId!.Value).Should().Equal(firstBatch);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, 1)), CancellationToken.None), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, firstBatch))), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 21))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Updating expansions for game 1");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldAskForADuplicatedIdOnce_AndOnlyForWhatIsNew()
    {
        var game = new Game("Test Game") { Id = 1 };
        game.AddExpansion(new Expansion("Kept", 200, 1));
        game.AddExpansion(new Expansion("Removed", 300, 1));
        _gameRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, 1)), CancellationToken.None)).ReturnsAsync(game);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101))))
            .ReturnsAsync(CreateSucceededThingResponse([CreateThingItem(101, "New")]));

        var result = await _gameService.UpdateGameExpansions(1, [200, 101, 101]);

        result.Select(x => (x.Title, x.BggId)).Should().Equal(("Kept", 200), ("New", 101));
        result[1].GameId.Should().Be(1);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, 1)), CancellationToken.None), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Updating expansions for game 1");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldPropagateOtherBggErrors_WithoutSaving()
    {
        var failure = new BoardGameGeekHttpException("BGG down", HttpStatusCode.ServiceUnavailable);
        var game = new Game("Test Game") { Id = 1 };
        _gameRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, 1)), CancellationToken.None)).ReturnsAsync(game);
        _bggClientMock.Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101)))).ThrowsAsync(failure);

        var act = () => _gameService.UpdateGameExpansions(1, [101]);

        (await act.Should().ThrowAsync<BoardGameGeekHttpException>()).Which.Should().BeSameAs(failure);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithExpansionsSpec>(s => Targets(s, 1)), CancellationToken.None), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsExpansions(r, 101))), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Updating expansions for game 1");
        VerifyApiKeyRead();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameExpansions_ShouldReturnNothing_WhenNoIdsAreAsked()
    {
        var expansionIds = new List<int>();
        _gameRepositoryMock.Setup(x => x.GetExpansions(1, expansionIds)).ReturnsAsync([]);

        var result = await _gameService.GetGameExpansions(1, expansionIds);

        result.Should().BeEmpty();
        _gameRepositoryMock.Verify(x => x.GetExpansions(1, expansionIds), Times.Once);
        VerifyNoOtherCalls();
    }
}
