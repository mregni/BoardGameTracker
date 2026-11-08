using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class BggImportServiceTests
{
    private const string UserName = "testuser";

    private static readonly BoardGameGeekHttpException Unauthorized = new("Unauthorized", HttpStatusCode.Unauthorized);
    private static readonly BoardGameGeekHttpException ServiceUnavailable = new("Service unavailable", HttpStatusCode.ServiceUnavailable);
    private static readonly BoardGameGeekHttpException TooManyRequests = new("Too many requests", HttpStatusCode.TooManyRequests);

    private readonly Mock<IBoardGameGeekXmlApi2Client> _bggClientMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IGameFactory> _gameFactoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<ILogger<BggImportService>> _loggerMock;
    private readonly BggImportService _bggImportService;

    public BggImportServiceTests()
    {
        _bggClientMock = new Mock<IBoardGameGeekXmlApi2Client>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock.Setup(x => x.IsBggEnabled()).ReturnsAsync(true);
        _gameFactoryMock = new Mock<IGameFactory>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _imageServiceMock = new Mock<IImageService>();
        _loggerMock = new Mock<ILogger<BggImportService>>();

        _bggImportService = new BggImportService(
            _bggClientMock.Object,
            _settingsServiceMock.Object,
            _gameFactoryMock.Object,
            _gameRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _imageServiceMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _settingsServiceMock.VerifyNoOtherCalls();
        _bggClientMock.VerifyNoOtherCalls();
        _gameFactoryMock.VerifyNoOtherCalls();
        _gameRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    private static bool IsThing(ThingRequest request, params int[] ids) =>
        request.RelativeUrl.ToString() == $"thing?id={string.Join("%2C", ids)}&stats=1";

    private static bool IsBoardGameCollection(CollectionRequest request) =>
        request.RelativeUrl.ToString() == $"collection?username={UserName}&subtype=boardgame";

    private void VerifyStartLog() =>
        _loggerMock.VerifyLog(LogLevel.Information, $"Starting BGG collection import for user {UserName}");

    private void VerifyImportLogs(int imported, int total)
    {
        _loggerMock.VerifyLog(LogLevel.Information, $"Importing {total} games from BGG");
        _loggerMock.VerifyLog(LogLevel.Information, $"BGG import completed, {imported}/{total} games imported");
    }

    private static ThingResponse CreateFailedThingResponse()
    {
        var response = (ThingResponse)RuntimeHelpers.GetUninitializedObject(typeof(ThingResponse));
        response.Succeeded.Should().BeFalse();
        return response;
    }

    private static CollectionResponse CreateFailedCollectionResponse()
    {
        var response = (CollectionResponse)RuntimeHelpers.GetUninitializedObject(typeof(CollectionResponse));
        response.Result.Should().BeNull();
        return response;
    }

    private static ThingResponse CreateSucceededThingResponse(IEnumerable<ThingResponse.Item> items)
    {
        return new ThingResponse(items);
    }

    private static CollectionResponse CreateSucceededCollectionResponse(IEnumerable<CollectionResponse.Item> items)
    {
        var itemCollection = new CollectionResponse.ItemCollection(items);
        return new CollectionResponse(itemCollection);
    }

    #region ImportGameFromBgg Tests

    [Fact]
    public async Task ImportGameFromBgg_ShouldThrowDomainException_WhenGameAlreadyExistsInRepository()
    {
        var search = new BggSearch { BggId = 12345, State = GameState.Owned, HasScoring = true };
        var existingGame = new Game("Existing Game") { Id = 1 };
        existingGame.UpdateBggId(12345);

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(12345))
            .ReturnsAsync(existingGame);

        var action = async () => await _bggImportService.ImportGameFromBgg(search);

        await action.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.GameAlreadyExists);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportGameFromBgg_ShouldReturnNull_WhenBggApiReturnsFailedResponse()
    {
        var search = new BggSearch { BggId = 12345, State = GameState.Owned, HasScoring = false };
        var thingResponse = CreateFailedThingResponse();

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(12345))
            .ReturnsAsync((Game?)null);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))))
            .ReturnsAsync(thingResponse);

        var result = await _bggImportService.ImportGameFromBgg(search);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Searching BGG for game with id 12345");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportGameFromBgg_ShouldReturnNull_WhenBggApiReturnsEmptyItemList()
    {
        var search = new BggSearch { BggId = 12345, State = GameState.Owned, HasScoring = false };
        var thingResponse = CreateSucceededThingResponse([]);

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(12345))
            .ReturnsAsync((Game?)null);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))))
            .ReturnsAsync(thingResponse);

        var result = await _bggImportService.ImportGameFromBgg(search);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Searching BGG for game with id 12345");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportGameFromBgg_ShouldCreateAndReturnGame_WhenGameNotExistsAndBggReturnsItem()
    {
        var search = new BggSearch
        {
            BggId = 42,
            State = GameState.Owned,
            HasScoring = true,
            Price = 29.99,
            AdditionDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };
        var rawItem = new ThingResponse.Item
        {
            Id = 42,
            Thumbnail = "thumb.jpg",
            Image = "image.jpg",
            Description = "A great game",
            Type = "boardgame"
        };
        var thingResponse = CreateSucceededThingResponse([rawItem]);
        var createdGame = new Game("Test Game") { Id = 1 };

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(42))
            .ReturnsAsync((Game?)null);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 42))))
            .ReturnsAsync(thingResponse);
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(
                rawItem,
                true,
                GameState.Owned,
                29.99m,
                search.AdditionDate,
                null))
            .ReturnsAsync(createdGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(createdGame))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _bggImportService.ImportGameFromBgg(search);

        result.Should().NotBeNull();
        result.Should().Be(createdGame);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(42), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 42))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            true,
            GameState.Owned,
            29.99m,
            search.AdditionDate,
            null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Searching BGG for game with id 42");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportGameFromBgg_ShouldDeleteTheDownloadedCover_WhenSavingFails()
    {
        var search = new BggSearch { BggId = 42, State = GameState.Owned, HasScoring = true };
        var rawItem = new ThingResponse.Item { Id = 42, Image = "image.jpg", Type = "boardgame" };
        var createdGame = new Game("Test Game");
        createdGame.UpdateImage("/images/cover/42.webp");
        var failure = new DbUpdateException("duplicate key value violates unique constraint");

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(42))
            .ReturnsAsync((Game?)null);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 42))))
            .ReturnsAsync(CreateSucceededThingResponse([rawItem]));
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(rawItem, true, GameState.Owned, null, null, null))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ThrowsAsync(failure);

        var action = async () => await _bggImportService.ImportGameFromBgg(search);

        (await action.Should().ThrowAsync<DbUpdateException>()).Which.Should().BeSameAs(failure);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(42), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 42))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(rawItem, true, GameState.Owned, null, null, null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/cover/42.webp"), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Searching BGG for game with id 42");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-5d)]
    public async Task ImportGameFromBgg_ShouldStoreNoPrice_WhenThePriceIsNotPositive(double price)
    {
        var addedOn = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var search = new BggSearch { BggId = 88, State = GameState.Owned, HasScoring = true, Price = price, AdditionDate = addedOn };
        var rawItem = new ThingResponse.Item { Id = 88, Image = "image.jpg", Type = "boardgame" };
        var createdGame = new Game("Free Game") { Id = 4 };
        _gameRepositoryMock.Setup(x => x.GetGameByBggId(88)).ReturnsAsync((Game?)null);
        _bggClientMock.Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 88)))).ReturnsAsync(CreateSucceededThingResponse([rawItem]));
        _gameFactoryMock.Setup(x => x.CreateFromBggAsync(rawItem, true, GameState.Owned, null, addedOn, null)).ReturnsAsync(createdGame);
        _gameRepositoryMock.Setup(x => x.CreateAsync(createdGame)).ReturnsAsync(createdGame);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _bggImportService.ImportGameFromBgg(search);

        result.Should().Be(createdGame);
        _gameRepositoryMock.Verify(x => x.GetGameByBggId(88), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 88))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(rawItem, true, GameState.Owned, null, addedOn, null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Searching BGG for game with id 88");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportGameFromBgg_ShouldForwardShopUrl_WhenSearchHasShopUrl()
    {
        var search = new BggSearch
        {
            BggId = 77,
            State = GameState.Wanted,
            HasScoring = false,
            Price = null,
            AdditionDate = null,
            ShopUrl = "https://shop.example.com/game"
        };
        var rawItem = new ThingResponse.Item
        {
            Id = 77,
            Thumbnail = "thumb.jpg",
            Image = "image.jpg",
            Description = "A wanted game",
            Type = "boardgame"
        };
        var thingResponse = CreateSucceededThingResponse([rawItem]);
        var createdGame = new Game("Wanted Game") { Id = 3 };

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(77))
            .ReturnsAsync((Game?)null);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 77))))
            .ReturnsAsync(thingResponse);
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(
                rawItem,
                false,
                GameState.Wanted,
                null,
                null,
                "https://shop.example.com/game"))
            .ReturnsAsync(createdGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(createdGame))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _bggImportService.ImportGameFromBgg(search);

        result.Should().Be(createdGame);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(77), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 77))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            false,
            GameState.Wanted,
            null,
            null,
            "https://shop.example.com/game"), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Searching BGG for game with id 77");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportGameFromBgg_ShouldThrowBggFeatureDisabledException_WhenBggIsDisabled()
    {
        var search = new BggSearch { BggId = 42, State = GameState.Owned, HasScoring = false };

        _settingsServiceMock
            .Setup(x => x.IsBggEnabled())
            .ReturnsAsync(false);

        var act = async () => await _bggImportService.ImportGameFromBgg(search);

        await act.Should().ThrowAsync<BggFeatureDisabledException>();

        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportGameFromBgg_ShouldThrowValidationException_WhenBggReturnsUnauthorized()
    {
        var search = new BggSearch { BggId = 12345, State = GameState.Owned, HasScoring = true };

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(12345))
            .ReturnsAsync((Game?)null);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))))
            .ThrowsAsync(Unauthorized);

        var act = async () => await _bggImportService.ImportGameFromBgg(search);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Invalid BGG API key. Please check your API key in settings.");

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Searching BGG for game with id 12345");
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG API key is invalid or expired", Unauthorized);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportGameFromBgg_ShouldReturnNull_WhenBggHttpRequestFails()
    {
        var search = new BggSearch { BggId = 12345, State = GameState.Owned, HasScoring = false };

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(12345))
            .ReturnsAsync((Game?)null);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))))
            .ThrowsAsync(ServiceUnavailable);

        var result = await _bggImportService.ImportGameFromBgg(search);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Searching BGG for game with id 12345");
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG API request failed for game 12345", ServiceUnavailable);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportGameFromBgg_ShouldThrowBggRateLimitException_WhenBggReturnsTooManyRequests()
    {
        var search = new BggSearch { BggId = 12345, State = GameState.Owned, HasScoring = false };

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(12345))
            .ReturnsAsync((Game?)null);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))))
            .ThrowsAsync(TooManyRequests);

        var act = async () => await _bggImportService.ImportGameFromBgg(search);

        await act.Should().ThrowAsync<BggRateLimitException>();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 12345))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Searching BGG for game with id 12345");
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG rate-limited the request for game 12345", TooManyRequests);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ImportBggCollection Tests

    [Fact]
    public async Task ImportBggCollection_ShouldReturnEmptyList_WhenItemListIsEmpty()
    {
        var userName = UserName;
        var collectionResponse = CreateSucceededCollectionResponse([]);

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().BeEmpty();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnEmptyList_WhenResponseResultIsNull()
    {
        var collectionResponse = CreateFailedCollectionResponse();

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection("testuser");

        result.Should().BeEmpty();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldMapNullImageToEmptyString()
    {
        var items = new List<CollectionResponse.Item>
        {
            new()
            {
                ObjectId = 404,
                Name = "No Image Game",
                Status = new CollectionResponse.Status
                {
                    Owned = true,
                    PreviouslyOwned = false,
                    ForTrade = false,
                    Want = false,
                    LastModified = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                Image = null,
                SubType = "boardgame"
            }
        };
        var collectionResponse = CreateSucceededCollectionResponse(items);

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection("testuser");

        result.Should().HaveCount(1);
        result[0].ImageUrl.Should().BeEmpty();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnMappedGames_WhenApiReturnsItemsSuccessfully()
    {
        var userName = UserName;
        var lastModified = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc);
        var items = new List<CollectionResponse.Item>
        {
            new()
            {
                ObjectId = 101,
                Name = "Catan",
                Status = new CollectionResponse.Status
                {
                    Owned = true,
                    PreviouslyOwned = false,
                    ForTrade = false,
                    Want = false,
                    LastModified = lastModified
                },
                Image = "https://example.com/catan.jpg",
                SubType = "boardgame"
            }
        };
        var collectionResponse = CreateSucceededCollectionResponse(items);

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().HaveCount(1);

        var game = result[0];
        game.BggId.Should().Be(101);
        game.Title.Should().Be("Catan");
        game.ImageUrl.Should().Be("https://example.com/catan.jpg");
        game.State.Should().Be(GameState.Owned);
        game.LastModified.Should().Be(lastModified);

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, false, false, true, GameState.Wanted)]
    [InlineData(false, false, true, false, GameState.ForTrade)]
    [InlineData(false, true, false, false, GameState.PreviouslyOwned)]
    [InlineData(true, false, false, false, GameState.Owned)]
    public async Task ImportBggCollection_ShouldMapGameState_BasedOnStatusFlags(
        bool owned, bool prevOwned, bool forTrade, bool want, GameState expectedState)
    {
        var userName = UserName;
        var items = new List<CollectionResponse.Item>
        {
            new()
            {
                ObjectId = 303,
                Name = "State Game",
                Status = new CollectionResponse.Status
                {
                    Owned = owned,
                    PreviouslyOwned = prevOwned,
                    ForTrade = forTrade,
                    Want = want,
                    LastModified = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                Image = "https://example.com/img.jpg",
                SubType = "boardgame"
            }
        };
        var collectionResponse = CreateSucceededCollectionResponse(items);

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().HaveCount(1);
        result[0].State.Should().Be(expectedState);

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldSkipItemsThatAreOnlyRatedOrPlayed()
    {
        var lastModified = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var items = new List<CollectionResponse.Item>
        {
            new()
            {
                ObjectId = 501,
                Name = "Owned Game",
                Status = new CollectionResponse.Status { Owned = true, LastModified = lastModified },
                Image = "owned.jpg",
                SubType = "boardgame"
            },
            new()
            {
                ObjectId = 502,
                Name = "Played Once",
                Status = new CollectionResponse.Status { LastModified = lastModified },
                Image = "played.jpg",
                SubType = "boardgame"
            }
        };

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ReturnsAsync(CreateSucceededCollectionResponse(items));

        var result = await _bggImportService.ImportBggCollection("testuser");

        result.Should().ContainSingle();
        result[0].BggId.Should().Be(501);
        result[0].State.Should().Be(GameState.Owned);

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnGamesSortedByName()
    {
        var userName = UserName;
        var defaultStatus = new CollectionResponse.Status
        {
            Owned = true,
            PreviouslyOwned = false,
            ForTrade = false,
            Want = false,
            LastModified = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var items = new List<CollectionResponse.Item>
        {
            new()
            {
                ObjectId = 3,
                Name = "Zombicide",
                Status = defaultStatus,
                Image = "z.jpg",
                SubType = "boardgame"
            },
            new()
            {
                ObjectId = 1,
                Name = "Agricola",
                Status = defaultStatus,
                Image = "a.jpg",
                SubType = "boardgame"
            },
            new()
            {
                ObjectId = 2,
                Name = "Catan",
                Status = defaultStatus,
                Image = "c.jpg",
                SubType = "boardgame"
            }
        };
        var collectionResponse = CreateSucceededCollectionResponse(items);

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().HaveCount(3);
        result[0].Title.Should().Be("Agricola");
        result[1].Title.Should().Be("Catan");
        result[2].Title.Should().Be("Zombicide");

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldThrowBggFeatureDisabledException_WhenBggIsDisabled()
    {
        _settingsServiceMock
            .Setup(x => x.IsBggEnabled())
            .ReturnsAsync(false);

        var act = async () => await _bggImportService.ImportBggCollection("testuser");

        await act.Should().ThrowAsync<BggFeatureDisabledException>();

        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldThrowValidationException_WhenBggReturnsUnauthorized()
    {
        const string userName = UserName;

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ThrowsAsync(Unauthorized);

        var act = async () => await _bggImportService.ImportBggCollection(userName);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Invalid BGG API key. Please check your API key in settings.");

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG API key is invalid or expired", Unauthorized);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldPropagateBggHttpException_WhenBggHttpRequestFails()
    {
        const string userName = UserName;

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ThrowsAsync(ServiceUnavailable);

        var act = async () => await _bggImportService.ImportBggCollection(userName);

        (await act.Should().ThrowAsync<BoardGameGeekHttpException>()).Which.Should().BeSameAs(ServiceUnavailable);

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG API request failed for collection import of user testuser", ServiceUnavailable);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldThrowBggCollectionPreparingException_WhenRetriesExhausted()
    {
        const string userName = UserName;

        var retriesExhausted = new Exception("Retries exhausted");
        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ThrowsAsync(retriesExhausted);

        var act = async () => await _bggImportService.ImportBggCollection(userName);

        await act.Should().ThrowAsync<BggCollectionPreparingException>();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG collection for user testuser was still being prepared after retries", retriesExhausted);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldThrowBggRateLimitException_WhenBggReturnsTooManyRequests()
    {
        const string userName = UserName;

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ThrowsAsync(TooManyRequests);

        var act = async () => await _bggImportService.ImportBggCollection(userName);

        await act.Should().ThrowAsync<BggRateLimitException>();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG rate-limited the collection request for user testuser", TooManyRequests);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldRequestSingleBoardgameSubtype()
    {
        var collectionResponse = CreateSucceededCollectionResponse([]);

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ReturnsAsync(collectionResponse);

        await _bggImportService.ImportBggCollection("testuser");

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldNotCallBgg_WhenTheTokenIsAlreadyCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = () => _bggImportService.ImportBggCollection(UserName, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldStopWaiting_WhenTheTokenIsCancelledDuringTheCall()
    {
        using var cancellation = new CancellationTokenSource();
        var pending = new TaskCompletionSource<CollectionResponse>();
        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .Returns(pending.Task);

        var import = _bggImportService.ImportBggCollection(UserName, cancellation.Token);
        await cancellation.CancelAsync();
        var act = () => import;

        await act.Should().ThrowAsync<OperationCanceledException>();
        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldPropagateAnyOtherException_WhenItIsNotAboutRetries()
    {
        var failure = new InvalidOperationException("Something else");
        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))))
            .ThrowsAsync(failure);

        var act = () => _bggImportService.ImportBggCollection(UserName);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        _bggClientMock.Verify(x => x.GetCollectionAsync(It.Is<CollectionRequest>(r => IsBoardGameCollection(r))), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyStartLog();
        VerifyNoOtherCalls();
    }

    #endregion

    #region ImportList Tests

    [Fact]
    public async Task ImportList_ShouldDeleteDownloadedImages_WhenSavingFails()
    {
        var addedDate = new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc);
        var importGames = new List<ImportGame>
        {
            new() { Title = "Game One", BggId = 1001, ImageUrl = "img1.jpg", State = GameState.Owned, HasScoring = true, AddedDate = addedDate },
            new() { Title = "Game Two", BggId = 1002, ImageUrl = "img2.jpg", State = GameState.Owned, HasScoring = true, AddedDate = addedDate }
        };
        var first = new ThingResponse.Item { Id = 1001, Image = "img1.jpg", Type = "boardgame" };
        var second = new ThingResponse.Item { Id = 1002, Image = "img2.jpg", Type = "boardgame" };
        var firstGame = new Game("Game One");
        firstGame.UpdateImage("/images/cover/1001.webp");
        var secondGame = new Game("Game Two");
        secondGame.UpdateImage("/images/cover/1002.webp");

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 1001, 1002))))
            .ReturnsAsync(CreateSucceededThingResponse([first, second]));
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(first, true, GameState.Owned, null, addedDate, null))
            .ReturnsAsync(firstGame);
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(second, true, GameState.Owned, null, addedDate, null))
            .ReturnsAsync(secondGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ThrowsAsync(new DbUpdateException("duplicate key value violates unique constraint"));

        var act = () => _bggImportService.ImportList(importGames);

        await act.Should().ThrowAsync<DbUpdateException>();
        _imageServiceMock.Verify(x => x.DeleteImage("/images/cover/1001.webp"), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/cover/1002.webp"), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetGameByBggId(1001), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetGameByBggId(1002), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(firstGame), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(secondGame), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(first, true, GameState.Owned, null, addedDate, null), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(second, true, GameState.Owned, null, addedDate, null), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 1001, 1002))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Importing 2 games from BGG");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldProcessAllGames_AndSaveChangesOnce_WhenAllGamesFound()
    {
        var addedDate = new DateTime(2024, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        var importGames = new List<ImportGame>
        {
            new()
            {
                Title = "Game One",
                BggId = 1001,
                ImageUrl = "img1.jpg",
                State = GameState.Owned,
                HasScoring = true,
                Price = 34.99m,
                AddedDate = addedDate
            }
        };
        var rawItem = new ThingResponse.Item
        {
            Id = 1001,
            Thumbnail = "thumb.jpg",
            Image = "img1.jpg",
            Description = "Great game",
            Type = "boardgame"
        };
        var thingResponse = CreateSucceededThingResponse([rawItem]);
        var createdGame = new Game("Game One") { Id = 10 };

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 1001))))
            .ReturnsAsync(thingResponse);
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(
                rawItem,
                true,
                GameState.Owned,
                34.99m,
                addedDate,
                null))
            .ReturnsAsync(createdGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(createdGame))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(1001), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 1001))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            true,
            GameState.Owned,
            34.99m,
            addedDate,
            null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(1, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldBatchBggLookups_WhenImportingManyGames()
    {
        var addedDate = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var importGames = Enumerable.Range(1, 25)
            .Select(id => new ImportGame
            {
                Title = $"Game {id}",
                BggId = id,
                ImageUrl = "img.jpg",
                State = GameState.Owned,
                HasScoring = true,
                Price = 0,
                AddedDate = addedDate
            })
            .ToList<ImportGame>();
        var firstBatch = Enumerable.Range(1, 20).ToArray();
        var secondBatch = Enumerable.Range(21, 5).ToArray();
        var games = importGames.ToDictionary(x => x.BggId, x => new Game(x.Title));

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, firstBatch))))
            .ReturnsAsync(CreateSucceededThingResponse(firstBatch.Select(id => new ThingResponse.Item { Id = id, Type = "boardgame" })));
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, secondBatch))))
            .ReturnsAsync(CreateSucceededThingResponse(secondBatch.Select(id => new ThingResponse.Item { Id = id, Type = "boardgame" })));
        foreach (var (id, game) in games)
        {
            _gameFactoryMock
                .Setup(x => x.CreateFromBggAsync(It.Is<ThingResponse.Item>(item => item.Id == id), true, GameState.Owned, null, addedDate, null))
                .ReturnsAsync(game);
            _gameRepositoryMock.Setup(x => x.CreateAsync(game)).ReturnsAsync(game);
        }

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, firstBatch))), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, secondBatch))), Times.Once);
        foreach (var (id, game) in games)
        {
            _gameRepositoryMock.Verify(x => x.GetGameByBggId(id), Times.Once);
            _gameFactoryMock.Verify(x => x.CreateFromBggAsync(It.Is<ThingResponse.Item>(item => item.Id == id), true, GameState.Owned, null, addedDate, null), Times.Once);
            _gameRepositoryMock.Verify(x => x.CreateAsync(game), Times.Once);
        }

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(25, 25);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldImportADuplicatedBggIdOnce()
    {
        var addedDate = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var importGames = Enumerable.Range(0, 2)
            .Select(_ => new ImportGame
            {
                Title = "Brass",
                BggId = 4242,
                ImageUrl = "img.jpg",
                State = GameState.Owned,
                HasScoring = true,
                Price = 0,
                AddedDate = addedDate
            })
            .ToList<ImportGame>();
        var createdGame = new Game("Brass");
        var item = new ThingResponse.Item { Id = 4242, Type = "boardgame" };

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 4242))))
            .ReturnsAsync(CreateSucceededThingResponse([item]));
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(item, true, GameState.Owned, null, addedDate, null))
            .ReturnsAsync(createdGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(createdGame))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(4242), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 4242))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(item, true, GameState.Owned, null, addedDate, null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(1, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldSkipGame_WhenAlreadyInDatabase()
    {
        var importGames = new List<ImportGame>
        {
            new()
            {
                Title = "Existing Game",
                BggId = 4242,
                ImageUrl = "img.jpg",
                State = GameState.Owned,
                HasScoring = true,
                Price = 10.00m,
                AddedDate = DateTime.UtcNow
            }
        };
        var existingGame = new Game("Existing Game") { Id = 7 };
        existingGame.UpdateBggId(4242);

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(4242))
            .ReturnsAsync(existingGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(0);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(4242), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(0, 1);
        _loggerMock.VerifyLog(LogLevel.Debug, "BGG game with id 4242 already imported, skipping");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldSkipGame_WhenFetchThingReturnsNoItems()
    {
        var importGames = new List<ImportGame>
        {
            new()
            {
                Title = "Missing Game",
                BggId = 9999,
                ImageUrl = "img.jpg",
                State = GameState.Owned,
                HasScoring = false,
                Price = 0,
                AddedDate = DateTime.UtcNow
            }
        };
        var thingResponse = CreateSucceededThingResponse([]);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 9999))))
            .ReturnsAsync(thingResponse);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(9999), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 9999))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(0, 1);
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG game with id 9999 not found, skipping");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldSkipGame_WhenFetchThingReturnsFailedResponse()
    {
        var importGames = new List<ImportGame>
        {
            new()
            {
                Title = "Missing Game",
                BggId = 9999,
                ImageUrl = "img.jpg",
                State = GameState.Owned,
                HasScoring = false,
                Price = 0,
                AddedDate = DateTime.UtcNow
            }
        };
        var thingResponse = CreateFailedThingResponse();

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 9999))))
            .ReturnsAsync(thingResponse);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(9999), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 9999))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(0, 1);
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG game with id 9999 not found, skipping");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldOnlySaveChangesOnce_WhenSomeGamesNotFound()
    {
        var addedDate = new DateTime(2024, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var importGames = new List<ImportGame>
        {
            new()
            {
                Title = "Found Game",
                BggId = 100,
                ImageUrl = "found.jpg",
                State = GameState.Owned,
                HasScoring = false,
                Price = 25.00m,
                AddedDate = addedDate
            },
            new()
            {
                Title = "Missing Game",
                BggId = 200,
                ImageUrl = "missing.jpg",
                State = GameState.Wanted,
                HasScoring = false,
                Price = 0,
                AddedDate = addedDate
            }
        };
        var rawItem = new ThingResponse.Item
        {
            Id = 100,
            Thumbnail = "thumb.jpg",
            Image = "found.jpg",
            Description = "Found",
            Type = "boardgame"
        };
        var thingResponse = CreateSucceededThingResponse([rawItem]);
        var createdGame = new Game("Found Game") { Id = 50 };

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 100, 200))))
            .ReturnsAsync(thingResponse);
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(
                rawItem,
                false,
                GameState.Owned,
                25.00m,
                addedDate,
                null))
            .ReturnsAsync(createdGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(createdGame))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(100), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetGameByBggId(200), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 100, 200))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            false,
            GameState.Owned,
            25.00m,
            addedDate,
            null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(1, 2);
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG game with id 200 not found, skipping");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldSaveChanges_WhenListIsEmpty()
    {
        var importGames = new List<ImportGame>();

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(0, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldThrowBggFeatureDisabledException_WhenBggIsDisabled()
    {
        _settingsServiceMock
            .Setup(x => x.IsBggEnabled())
            .ReturnsAsync(false);

        var act = async () => await _bggImportService.ImportList(new List<ImportGame>());

        await act.Should().ThrowAsync<BggFeatureDisabledException>();

        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldSkipGameAndContinue_WhenFactoryThrows()
    {
        var addedDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var importGames = new List<ImportGame>
        {
            new()
            {
                Title = "Broken Game",
                BggId = 100,
                ImageUrl = "broken.jpg",
                State = GameState.Owned,
                HasScoring = false,
                Price = 10.00m,
                AddedDate = addedDate
            },
            new()
            {
                Title = "Working Game",
                BggId = 200,
                ImageUrl = "working.jpg",
                State = GameState.Owned,
                HasScoring = false,
                Price = 20.00m,
                AddedDate = addedDate
            }
        };
        var brokenItem = new ThingResponse.Item
        {
            Id = 100,
            Thumbnail = "thumb.jpg",
            Image = "broken.jpg",
            Description = "Broken",
            Type = "boardgame"
        };
        var workingItem = new ThingResponse.Item
        {
            Id = 200,
            Thumbnail = "thumb.jpg",
            Image = "working.jpg",
            Description = "Working",
            Type = "boardgame"
        };
        var createdGame = new Game("Working Game") { Id = 60 };

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 100, 200))))
            .ReturnsAsync(CreateSucceededThingResponse([brokenItem, workingItem]));
        var factoryFailure = new InvalidOperationException("factory failed");
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(brokenItem, false, GameState.Owned, 10.00m, addedDate, null))
            .ThrowsAsync(factoryFailure);
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(workingItem, false, GameState.Owned, 20.00m, addedDate, null))
            .ReturnsAsync(createdGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(createdGame))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(100), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetGameByBggId(200), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 100, 200))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(brokenItem, false, GameState.Owned, 10.00m, addedDate, null), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(workingItem, false, GameState.Owned, 20.00m, addedDate, null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(1, 2);
        _loggerMock.VerifyLog(LogLevel.Warning, "Failed to import BGG game 100, skipping", factoryFailure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldDeleteTheDownloadedCover_WhenAddingTheGameFails()
    {
        var addedDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var importGames = new List<ImportGame>
        {
            new() { Title = "Broken Game", BggId = 100, ImageUrl = "broken.jpg", State = GameState.Owned, AddedDate = addedDate },
            new() { Title = "Working Game", BggId = 200, ImageUrl = "working.jpg", State = GameState.Owned, AddedDate = addedDate }
        };
        var brokenItem = new ThingResponse.Item { Id = 100, Image = "broken.jpg", Type = "boardgame" };
        var workingItem = new ThingResponse.Item { Id = 200, Image = "working.jpg", Type = "boardgame" };
        var brokenGame = new Game("Broken Game");
        brokenGame.UpdateImage("/images/cover/100.webp");
        var workingGame = new Game("Working Game");
        workingGame.UpdateImage("/images/cover/200.webp");
        var failure = new InvalidOperationException("tracking failed");

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 100, 200))))
            .ReturnsAsync(CreateSucceededThingResponse([brokenItem, workingItem]));
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(brokenItem, false, GameState.Owned, null, addedDate, null))
            .ReturnsAsync(brokenGame);
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(workingItem, false, GameState.Owned, null, addedDate, null))
            .ReturnsAsync(workingGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(brokenGame))
            .ThrowsAsync(failure);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(workingGame))
            .ReturnsAsync(workingGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(100), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetGameByBggId(200), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 100, 200))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(brokenItem, false, GameState.Owned, null, addedDate, null), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(workingItem, false, GameState.Owned, null, addedDate, null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(brokenGame), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(workingGame), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/cover/100.webp"), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(1, 2);
        _loggerMock.VerifyLog(LogLevel.Warning, "Failed to import BGG game 100, skipping", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldRethrowValidationExceptionWithoutSaving_WhenBggReturnsUnauthorized()
    {
        var importGames = new List<ImportGame>
        {
            new()
            {
                Title = "Any Game",
                BggId = 5555,
                ImageUrl = "img.jpg",
                State = GameState.Owned,
                HasScoring = false,
                Price = 0,
                AddedDate = DateTime.UtcNow
            }
        };

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 5555))))
            .ThrowsAsync(Unauthorized);

        var act = async () => await _bggImportService.ImportList(importGames);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Invalid BGG API key. Please check your API key in settings.");

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(5555), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 5555))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Importing 1 games from BGG");
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG API key is invalid or expired", Unauthorized);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldRethrowBggRateLimitExceptionWithoutSaving_WhenBggReturnsTooManyRequests()
    {
        var importGames = new List<ImportGame>
        {
            new()
            {
                Title = "Any Game",
                BggId = 5555,
                ImageUrl = "img.jpg",
                State = GameState.Owned,
                HasScoring = false,
                Price = 0,
                AddedDate = DateTime.UtcNow
            }
        };

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 5555))))
            .ThrowsAsync(TooManyRequests);

        var act = async () => await _bggImportService.ImportList(importGames);

        await act.Should().ThrowAsync<BggRateLimitException>();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(5555), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 5555))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Importing 1 games from BGG");
        _loggerMock.VerifyLog(LogLevel.Warning, "BGG rate-limited the request while importing games", TooManyRequests);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task ImportList_ShouldPassNullPriceToFactory_WhenPriceIsMissingOrNotPositive(int? price)
    {
        var addedDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var importGames = new List<ImportGame>
        {
            new()
            {
                Title = "Odd Price Game",
                BggId = 300,
                ImageUrl = "img.jpg",
                State = GameState.Owned,
                HasScoring = false,
                Price = price,
                AddedDate = addedDate
            }
        };
        var rawItem = new ThingResponse.Item
        {
            Id = 300,
            Thumbnail = "thumb.jpg",
            Image = "img.jpg",
            Description = "Odd price",
            Type = "boardgame"
        };
        var createdGame = new Game("Odd Price Game") { Id = 70 };

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 300))))
            .ReturnsAsync(CreateSucceededThingResponse([rawItem]));
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(rawItem, false, GameState.Owned, null, addedDate, null))
            .ReturnsAsync(createdGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(createdGame))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(300), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 300))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(rawItem, false, GameState.Owned, null, addedDate, null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(1, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldThrow_WhenTheListIsNull()
    {
        var act = () => _bggImportService.ImportList(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("games");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldKeepTheOtherBatches_WhenOneBggRequestFails()
    {
        var addedDate = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var importGames = Enumerable.Range(1, 21)
            .Select(id => new ImportGame { Title = $"Game {id}", BggId = id, ImageUrl = "img.jpg", State = GameState.Owned, AddedDate = addedDate })
            .ToList();
        var firstBatch = Enumerable.Range(1, 20).ToArray();
        var item = new ThingResponse.Item { Id = 21, Type = "boardgame" };
        var createdGame = new Game("Game 21");
        _bggClientMock.Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, firstBatch)))).ThrowsAsync(ServiceUnavailable);
        _bggClientMock.Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 21)))).ReturnsAsync(CreateSucceededThingResponse([item]));
        _gameFactoryMock.Setup(x => x.CreateFromBggAsync(item, false, GameState.Owned, null, addedDate, null)).ReturnsAsync(createdGame);
        _gameRepositoryMock.Setup(x => x.CreateAsync(createdGame)).ReturnsAsync(createdGame);

        await _bggImportService.ImportList(importGames);

        foreach (var id in Enumerable.Range(1, 21))
        {
            _gameRepositoryMock.Verify(x => x.GetGameByBggId(id), Times.Once);
        }

        foreach (var id in firstBatch)
        {
            _loggerMock.VerifyLog(LogLevel.Warning, $"BGG game with id {id} not found, skipping");
        }

        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, firstBatch))), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 21))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(item, false, GameState.Owned, null, addedDate, null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"BGG API request failed for games {string.Join(", ", firstBatch)}", ServiceUnavailable);
        VerifyImportLogs(1, 21);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldDeleteTheImagesAndNotSave_WhenTheFactoryThrowsAValidationException()
    {
        var addedDate = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var importGames = new List<ImportGame>
        {
            new() { Title = "First", BggId = 1, ImageUrl = "1.jpg", State = GameState.Owned, AddedDate = addedDate },
            new() { Title = "Second", BggId = 2, ImageUrl = "2.jpg", State = GameState.Owned, AddedDate = addedDate }
        };
        var first = new ThingResponse.Item { Id = 1, Type = "boardgame" };
        var second = new ThingResponse.Item { Id = 2, Type = "boardgame" };
        var firstGame = new Game("First");
        firstGame.UpdateImage("/images/cover/1.webp");
        _bggClientMock.Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 1, 2)))).ReturnsAsync(CreateSucceededThingResponse([first, second]));
        _gameFactoryMock.Setup(x => x.CreateFromBggAsync(first, false, GameState.Owned, null, addedDate, null)).ReturnsAsync(firstGame);
        _gameFactoryMock.Setup(x => x.CreateFromBggAsync(second, false, GameState.Owned, null, addedDate, null)).ThrowsAsync(new ValidationException("not valid"));
        _gameRepositoryMock.Setup(x => x.CreateAsync(firstGame)).ReturnsAsync(firstGame);

        var act = () => _bggImportService.ImportList(importGames);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("not valid");
        _gameRepositoryMock.Verify(x => x.GetGameByBggId(1), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetGameByBggId(2), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 1, 2))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(first, false, GameState.Owned, null, addedDate, null), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(second, false, GameState.Owned, null, addedDate, null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(firstGame), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/cover/1.webp"), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Importing 2 games from BGG");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldPassThePrice_WhenItIsTheSmallestPositiveAmount()
    {
        var addedDate = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var importGames = new List<ImportGame> { new() { Title = "Cheap", BggId = 3, ImageUrl = "3.jpg", State = GameState.Owned, Price = 0.01m, AddedDate = addedDate } };
        var item = new ThingResponse.Item { Id = 3, Type = "boardgame" };
        var game = new Game("Cheap");
        _bggClientMock.Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 3)))).ReturnsAsync(CreateSucceededThingResponse([item]));
        _gameFactoryMock.Setup(x => x.CreateFromBggAsync(item, false, GameState.Owned, 0.01m, addedDate, null)).ReturnsAsync(game);
        _gameRepositoryMock.Setup(x => x.CreateAsync(game)).ReturnsAsync(game);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(3), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => IsThing(r, 3))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(item, false, GameState.Owned, 0.01m, addedDate, null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(game), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        VerifyImportLogs(1, 1);
        VerifyNoOtherCalls();
    }

    #endregion
}
