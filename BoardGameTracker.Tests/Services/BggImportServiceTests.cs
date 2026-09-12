using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class BggImportServiceTests
{
    private readonly Mock<IBoardGameGeekXmlApi2Client> _bggClientMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IGameFactory> _gameFactoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<BggImportService>> _loggerMock;
    private readonly BggImportService _bggImportService;

    public BggImportServiceTests()
    {
        _bggClientMock = new Mock<IBoardGameGeekXmlApi2Client>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock.Setup(x => x.IsBggEnabled()).ReturnsAsync(true);
        _gameFactoryMock = new Mock<IGameFactory>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameRepositoryMock.Setup(x => x.GetGameByBggId(It.IsAny<int>())).ReturnsAsync((Game?)null);
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<BggImportService>>();

        _bggImportService = new BggImportService(
            _bggClientMock.Object,
            _settingsServiceMock.Object,
            _gameFactoryMock.Object,
            _gameRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _settingsServiceMock.Verify(x => x.IsBggEnabled(), Times.Once);
        _settingsServiceMock.VerifyNoOtherCalls();
        _bggClientMock.VerifyNoOtherCalls();
        _gameFactoryMock.VerifyNoOtherCalls();
        _gameRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
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
    public async Task ImportGameFromBgg_ShouldReturnExistingGame_WhenGameAlreadyExistsInRepository()
    {
        var search = new BggSearch { BggId = 12345, State = GameState.Owned, HasScoring = true };
        var existingGame = new Game("Existing Game") { Id = 1 };
        existingGame.UpdateBggId(12345);

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(12345))
            .ReturnsAsync(existingGame);

        var result = await _bggImportService.ImportGameFromBgg(search);

        result.Should().NotBeNull();
        result.Should().Be(existingGame);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(thingResponse);

        var result = await _bggImportService.ImportGameFromBgg(search);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(thingResponse);

        var result = await _bggImportService.ImportGameFromBgg(search);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
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
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            true,
            GameState.Owned,
            29.99m,
            search.AdditionDate,
            null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
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
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            false,
            GameState.Wanted,
            null,
            null,
            "https://shop.example.com/game"), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ThrowsAsync(new BoardGameGeekHttpException("Unauthorized", HttpStatusCode.Unauthorized));

        var act = async () => await _bggImportService.ImportGameFromBgg(search);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Invalid BGG API key. Please check your API key in settings.");

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ThrowsAsync(new BoardGameGeekHttpException("Service unavailable", HttpStatusCode.ServiceUnavailable));

        var result = await _bggImportService.ImportGameFromBgg(search);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ThrowsAsync(new BoardGameGeekHttpException("Too many requests", HttpStatusCode.TooManyRequests));

        var act = async () => await _bggImportService.ImportGameFromBgg(search);

        await act.Should().ThrowAsync<BggRateLimitException>();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(12345), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ImportBggCollection Tests

    [Fact]
    public async Task ImportBggCollection_ShouldReturnEmptyList_WhenItemListIsEmpty()
    {
        var userName = "testuser";
        var collectionResponse = CreateSucceededCollectionResponse([]);

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().BeEmpty();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnEmptyList_WhenResponseResultIsNull()
    {
        var collectionResponse = CreateFailedCollectionResponse();

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection("testuser");

        result.Should().BeEmpty();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
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
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection("testuser");

        result.Should().HaveCount(1);
        result[0].ImageUrl.Should().BeEmpty();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnMappedGames_WhenApiReturnsItemsSuccessfully()
    {
        var userName = "testuser";
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
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().HaveCount(1);

        var game = result[0];
        game.BggId.Should().Be(101);
        game.Title.Should().Be("Catan");
        game.ImageUrl.Should().Be("https://example.com/catan.jpg");
        game.State.Should().Be(GameState.Owned);
        game.LastModified.Should().Be(lastModified);

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
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
        var userName = "testuser";
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
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().HaveCount(1);
        result[0].State.Should().Be(expectedState);

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnGamesSortedByName()
    {
        var userName = "testuser";
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
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().HaveCount(3);
        result[0].Title.Should().Be("Agricola");
        result[1].Title.Should().Be("Catan");
        result[2].Title.Should().Be("Zombicide");

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
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

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldThrowValidationException_WhenBggReturnsUnauthorized()
    {
        const string userName = "testuser";

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ThrowsAsync(new BoardGameGeekHttpException("Unauthorized", HttpStatusCode.Unauthorized));

        var act = async () => await _bggImportService.ImportBggCollection(userName);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Invalid BGG API key. Please check your API key in settings.");

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldPropagateBggHttpException_WhenBggHttpRequestFails()
    {
        const string userName = "testuser";

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ThrowsAsync(new BoardGameGeekHttpException("Service unavailable", HttpStatusCode.ServiceUnavailable));

        var act = async () => await _bggImportService.ImportBggCollection(userName);

        await act.Should().ThrowAsync<BoardGameGeekHttpException>();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldThrowBggCollectionPreparingException_WhenRetriesExhausted()
    {
        const string userName = "testuser";

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ThrowsAsync(new Exception("Retries exhausted"));

        var act = async () => await _bggImportService.ImportBggCollection(userName);

        await act.Should().ThrowAsync<BggCollectionPreparingException>();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldThrowBggRateLimitException_WhenBggReturnsTooManyRequests()
    {
        const string userName = "testuser";

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ThrowsAsync(new BoardGameGeekHttpException("Too many requests", HttpStatusCode.TooManyRequests));

        var act = async () => await _bggImportService.ImportBggCollection(userName);

        await act.Should().ThrowAsync<BggRateLimitException>();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldRequestSingleBoardgameSubtype()
    {
        var collectionResponse = CreateSucceededCollectionResponse([]);

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ReturnsAsync(collectionResponse);

        await _bggImportService.ImportBggCollection("testuser");

        _bggClientMock.Verify(x => x.GetCollectionAsync(
            It.Is<CollectionRequest>(r =>
                r.RelativeUrl.ToString().Contains("subtype=boardgame")
                && !r.RelativeUrl.ToString().Contains("boardgameexpansion"))),
            Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ImportList Tests

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
                Price = 34.99,
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
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
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            true,
            GameState.Owned,
            34.99m,
            addedDate,
            null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync((ThingRequest request) => CreateSucceededThingResponse(
                request.Ids.Select(id => new ThingResponse.Item { Id = id, Type = "boardgame" })));
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(
                It.IsAny<ThingResponse.Item>(), It.IsAny<bool>(), It.IsAny<GameState>(),
                It.IsAny<decimal?>(), It.IsAny<DateTime?>(), It.IsAny<string?>()))
            .ReturnsAsync((ThingResponse.Item item, bool _, GameState _, decimal? _, DateTime? _, string? _) =>
                new Game($"Game {item.Id}"));
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Game>()))
            .ReturnsAsync((Game game) => game);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Exactly(2));
        _gameRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Game>()), Times.Exactly(25));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
                Price = 10.00,
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(thingResponse);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(9999), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(thingResponse);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(9999), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
                Price = 25.00,
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
        var foundThingResponse = CreateSucceededThingResponse([rawItem]);
        var createdGame = new Game("Found Game") { Id = 50 };

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(foundThingResponse);
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
        _bggClientMock.Verify(
            x => x.GetThingAsync(It.Is<ThingRequest>(r => r.Ids.Contains(100) && r.Ids.Contains(200))),
            Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            false,
            GameState.Owned,
            25.00m,
            addedDate,
            null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
                Price = 10.00,
                AddedDate = addedDate
            },
            new()
            {
                Title = "Working Game",
                BggId = 200,
                ImageUrl = "working.jpg",
                State = GameState.Owned,
                HasScoring = false,
                Price = 20.00,
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(CreateSucceededThingResponse([brokenItem, workingItem]));
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(brokenItem, false, GameState.Owned, 10.00m, addedDate, null))
            .ThrowsAsync(new InvalidOperationException("factory failed"));
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
        _bggClientMock.Verify(
            x => x.GetThingAsync(It.Is<ThingRequest>(r => r.Ids.Contains(100) && r.Ids.Contains(200))),
            Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(brokenItem, false, GameState.Owned, 10.00m, addedDate, null), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(workingItem, false, GameState.Owned, 20.00m, addedDate, null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ThrowsAsync(new BoardGameGeekHttpException("Unauthorized", HttpStatusCode.Unauthorized));

        var act = async () => await _bggImportService.ImportList(importGames);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Invalid BGG API key. Please check your API key in settings.");

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(5555), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ThrowsAsync(new BoardGameGeekHttpException("Too many requests", HttpStatusCode.TooManyRequests));

        var act = async () => await _bggImportService.ImportList(importGames);

        await act.Should().ThrowAsync<BggRateLimitException>();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(5555), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MaxValue)]
    public async Task ImportList_ShouldPassNullPriceToFactory_WhenPriceIsNotRepresentable(double price)
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
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
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(rawItem, false, GameState.Owned, null, addedDate, null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
