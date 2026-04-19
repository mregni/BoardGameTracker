using System;
using System.Collections.Generic;
using System.Linq;
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
        _settingsServiceMock.Setup(x => x.GetBggApiKeyAsync()).ReturnsAsync("test-api-key");
        _settingsServiceMock.Setup(x => x.IsBggEnabled()).ReturnsAsync(true);
        _gameFactoryMock = new Mock<IGameFactory>();
        _gameRepositoryMock = new Mock<IGameRepository>();
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
        _bggClientMock.VerifyNoOtherCalls();
        _gameFactoryMock.VerifyNoOtherCalls();
        _gameRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    private static ThingResponse CreateFailedThingResponse()
    {
        return (ThingResponse)RuntimeHelpers.GetUninitializedObject(typeof(ThingResponse));
    }

    private static ThingResponse CreateSucceededThingResponse(IEnumerable<ThingResponse.Item> items)
    {
        return new ThingResponse(items);
    }

    private static CollectionResponse CreateFailedCollectionResponse()
    {
        return (CollectionResponse)RuntimeHelpers.GetUninitializedObject(typeof(CollectionResponse));
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
                search.AdditionDate))
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
            search.AdditionDate), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportGameFromBgg_ShouldPassNullPrice_WhenSearchHasNoPrice()
    {
        var search = new BggSearch
        {
            BggId = 99,
            State = GameState.Wanted,
            HasScoring = false,
            Price = null,
            AdditionDate = null
        };
        var rawItem = new ThingResponse.Item
        {
            Id = 99,
            Thumbnail = "thumb.jpg",
            Image = "image.jpg",
            Description = "A game",
            Type = "boardgame"
        };
        var thingResponse = CreateSucceededThingResponse([rawItem]);
        var createdGame = new Game("No Price Game") { Id = 2 };

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(99))
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
                null))
            .ReturnsAsync(createdGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(createdGame))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _bggImportService.ImportGameFromBgg(search);

        result.Should().Be(createdGame);

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(99), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            false,
            GameState.Wanted,
            null,
            null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportGameFromBgg_ShouldThrowBggFeatureDisabledException_WhenApiKeyIsEmpty()
    {
        var search = new BggSearch { BggId = 42, State = GameState.Owned, HasScoring = false };

        _settingsServiceMock
            .Setup(x => x.IsBggEnabled())
            .ReturnsAsync(false);

        var act = async () => await _bggImportService.ImportGameFromBgg(search);

        await act.Should().ThrowAsync<BggFeatureDisabledException>();

        VerifyNoOtherCalls();
    }

    #endregion

    #region ImportBggCollection Tests

    [Fact]
    public async Task ImportBggCollection_ShouldReturnNull_WhenApiResponseSucceededIsFalse()
    {
        var userName = "testuser";
        var collectionResponse = CreateFailedCollectionResponse();

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().BeNull();

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnResultWithEmptyGames_WhenItemListIsEmpty()
    {
        var userName = "testuser";
        var collectionResponse = CreateSucceededCollectionResponse([]);

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        result.Games.Should().BeEmpty();

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

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        result.Games.Should().HaveCount(1);

        var game = result.Games[0];
        game.BggId.Should().Be(101);
        game.Title.Should().Be("Catan");
        game.ImageUrl.Should().Be("https://example.com/catan.jpg");
        game.State.Should().Be(GameState.Owned);
        game.IsExpansion.Should().BeFalse();
        game.LastModified.Should().Be(lastModified);

        _bggClientMock.Verify(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldSetIsExpansionTrue_WhenSubtypeIsBoardgameExpansion()
    {
        var userName = "testuser";
        var items = new List<CollectionResponse.Item>
        {
            new()
            {
                ObjectId = 202,
                Name = "Catan Expansion",
                Status = new CollectionResponse.Status
                {
                    Owned = true,
                    PreviouslyOwned = false,
                    ForTrade = false,
                    Want = false,
                    LastModified = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc)
                },
                Image = "https://example.com/expansion.jpg",
                SubType = "boardgameexpansion"
            }
        };
        var collectionResponse = CreateSucceededCollectionResponse(items);

        _bggClientMock
            .Setup(x => x.GetCollectionAsync(It.IsAny<CollectionRequest>()))
            .ReturnsAsync(collectionResponse);

        var result = await _bggImportService.ImportBggCollection(userName);

        result!.Games.Should().HaveCount(1);
        result.Games[0].IsExpansion.Should().BeTrue();

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

        result!.Games.Should().HaveCount(1);
        result.Games[0].State.Should().Be(expectedState);

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

        result!.Games.Should().HaveCount(3);
        result.Games[0].Title.Should().Be("Agricola");
        result.Games[1].Title.Should().Be("Catan");
        result.Games[2].Title.Should().Be("Zombicide");

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
                addedDate))
            .ReturnsAsync(createdGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(createdGame))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            true,
            GameState.Owned,
            34.99m,
            addedDate), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
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
        var missingThingResponse = CreateSucceededThingResponse([]);
        var createdGame = new Game("Found Game") { Id = 50 };

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => r.Ids.Contains(100))))
            .ReturnsAsync(foundThingResponse);
        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => r.Ids.Contains(200))))
            .ReturnsAsync(missingThingResponse);
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(
                rawItem,
                false,
                GameState.Owned,
                25.00m,
                addedDate))
            .ReturnsAsync(createdGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(createdGame))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => r.Ids.Contains(100))), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.Is<ThingRequest>(r => r.Ids.Contains(200))), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            false,
            GameState.Owned,
            25.00m,
            addedDate), Times.Once);
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
    public async Task ImportList_ShouldMapSearchFieldsCorrectly_WhenProcessingGame()
    {
        var addedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var importGames = new List<ImportGame>
        {
            new()
            {
                Title = "Mapping Test Game",
                BggId = 555,
                ImageUrl = "img.jpg",
                State = GameState.ForTrade,
                HasScoring = true,
                Price = 19.50,
                AddedDate = addedDate
            }
        };
        var rawItem = new ThingResponse.Item
        {
            Id = 555,
            Thumbnail = "thumb.jpg",
            Image = "img.jpg",
            Description = "Mapping test",
            Type = "boardgame"
        };
        var thingResponse = CreateSucceededThingResponse([rawItem]);
        var createdGame = new Game("Mapping Test Game") { Id = 77 };

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(thingResponse);
        _gameFactoryMock
            .Setup(x => x.CreateFromBggAsync(
                rawItem,
                true,
                GameState.ForTrade,
                19.50m,
                addedDate))
            .ReturnsAsync(createdGame);
        _gameRepositoryMock
            .Setup(x => x.CreateAsync(createdGame))
            .ReturnsAsync(createdGame);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromBggAsync(
            rawItem,
            true,
            GameState.ForTrade,
            19.50m,
            addedDate), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
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

    #endregion
}
