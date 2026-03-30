using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Bgg.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Refit;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class BggImportServiceTests
{
    private readonly Mock<IBggApi> _bggApiMock;
    private readonly Mock<IBggGameTranslator> _bggGameTranslatorMock;
    private readonly Mock<IGameFactory> _gameFactoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<BggImportService>> _loggerMock;
    private readonly BggImportService _bggImportService;

    public BggImportServiceTests()
    {
        _bggApiMock = new Mock<IBggApi>();
        _bggGameTranslatorMock = new Mock<IBggGameTranslator>();
        _gameFactoryMock = new Mock<IGameFactory>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<BggImportService>>();

        _bggImportService = new BggImportService(
            _bggApiMock.Object,
            _bggGameTranslatorMock.Object,
            _gameFactoryMock.Object,
            _gameRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _bggApiMock.VerifyNoOtherCalls();
        _bggGameTranslatorMock.VerifyNoOtherCalls();
        _gameFactoryMock.VerifyNoOtherCalls();
        _gameRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    #region GetGameByBggId Tests

    [Fact]
    public async Task GetGameByBggId_ShouldReturnGame_WhenGameExists()
    {
        // Arrange
        var bggId = 12345;
        var game = new Game("Test Game") { Id = 1 };
        game.UpdateBggId(bggId);

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(bggId))
            .ReturnsAsync(game);

        // Act
        var result = await _bggImportService.GetGameByBggId(bggId);

        // Assert
        result.Should().NotBeNull();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(bggId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameByBggId_ShouldReturnNull_WhenGameDoesNotExist()
    {
        // Arrange
        var bggId = 999;

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(bggId))
            .ReturnsAsync((Game?)null);

        // Act
        var result = await _bggImportService.GetGameByBggId(bggId);

        // Assert
        result.Should().BeNull();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(bggId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region SearchGame Tests

    [Fact]
    public async Task SearchGame_ShouldReturnTranslatedBggGame_WhenApiReturnsSuccessWithGame()
    {
        var bggId = 12345;
        var rawGame = new BggRawGame
        {
            Thumbnail = "thumb.jpg",
            Image = "image.jpg",
            Description = "A great game",
            Type = "boardgame"
        };
        var bggGame = new BggGame
        {
            Thumbnail = "thumb.jpg",
            Image = "image.jpg",
            Description = "A great game",
            BggId = bggId
        };
        var apiGames = new BggApiGames { Games = [rawGame] };
        var response = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            apiGames,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.SearchGame(bggId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        _bggGameTranslatorMock
            .Setup(x => x.TranslateRawGame(rawGame))
            .Returns(bggGame);

        var result = await _bggImportService.SearchGame(bggId);

        result.Should().NotBeNull();
        result.Should().Be(bggGame);

        _bggApiMock.Verify(x => x.SearchGame(bggId, 1, It.IsAny<CancellationToken>()), Times.Once);
        _bggGameTranslatorMock.Verify(x => x.TranslateRawGame(rawGame), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchGame_ShouldReturnNull_WhenApiReturnsFailureStatusCode()
    {
        var bggId = 12345;
        var response = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            null,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.SearchGame(bggId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.SearchGame(bggId);

        result.Should().BeNull();

        _bggApiMock.Verify(x => x.SearchGame(bggId, 1, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchGame_ShouldReturnNull_WhenApiReturnsNullContent()
    {
        var bggId = 12345;
        var response = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            null,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.SearchGame(bggId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.SearchGame(bggId);

        result.Should().BeNull();

        _bggApiMock.Verify(x => x.SearchGame(bggId, 1, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchGame_ShouldReturnNull_WhenApiReturnsEmptyGamesArray()
    {
        var bggId = 12345;
        var apiGames = new BggApiGames { Games = [] };
        var response = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            apiGames,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.SearchGame(bggId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.SearchGame(bggId);

        result.Should().BeNull();

        _bggApiMock.Verify(x => x.SearchGame(bggId, 1, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchGame_ShouldReturnNull_WhenApiReturnsNullGamesArray()
    {
        var bggId = 12345;
        var apiGames = new BggApiGames { Games = null };
        var response = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            apiGames,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.SearchGame(bggId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.SearchGame(bggId);

        result.Should().BeNull();

        _bggApiMock.Verify(x => x.SearchGame(bggId, 1, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region SearchOnBgg Tests

    [Fact]
    public async Task SearchOnBgg_ShouldReturnGame_AndSaveChanges_WhenCalledWithValidData()
    {
        var bggGame = new BggGame
        {
            Thumbnail = "thumb.jpg",
            Image = "image.jpg",
            Description = "A great game",
            BggId = 42
        };
        var search = new BggSearch
        {
            BggId = 42,
            State = GameState.Owned,
            HasScoring = true,
            Price = 29.99,
            AdditionDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };
        var importData = new GameImportData
        {
            Title = "Test Game",
            BggId = 42,
            Description = "A great game",
            ImageUrl = "image.jpg"
        };
        var createdGame = new Game("Test Game") { Id = 1 };

        _bggGameTranslatorMock
            .Setup(x => x.TranslateFromBggAsync(bggGame))
            .ReturnsAsync(importData);
        _gameFactoryMock
            .Setup(x => x.CreateFromImportDataAsync(
                importData,
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

        var result = await _bggImportService.SearchOnBgg(bggGame, search);

        result.Should().NotBeNull();
        result.Should().Be(createdGame);

        _bggGameTranslatorMock.Verify(x => x.TranslateFromBggAsync(bggGame), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromImportDataAsync(
            importData,
            true,
            GameState.Owned,
            29.99m,
            search.AdditionDate), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchOnBgg_ShouldPassNullPrice_WhenSearchHasNoPrice()
    {
        var bggGame = new BggGame
        {
            Thumbnail = "thumb.jpg",
            Image = "image.jpg",
            Description = "A great game",
            BggId = 99
        };
        var search = new BggSearch
        {
            BggId = 99,
            State = GameState.Wanted,
            HasScoring = false,
            Price = null,
            AdditionDate = null
        };
        var importData = new GameImportData
        {
            Title = "No Price Game",
            BggId = 99,
            Description = "A great game",
            ImageUrl = "image.jpg"
        };
        var createdGame = new Game("No Price Game") { Id = 2 };

        _bggGameTranslatorMock
            .Setup(x => x.TranslateFromBggAsync(bggGame))
            .ReturnsAsync(importData);
        _gameFactoryMock
            .Setup(x => x.CreateFromImportDataAsync(
                importData,
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

        var result = await _bggImportService.SearchOnBgg(bggGame, search);

        result.Should().Be(createdGame);

        _bggGameTranslatorMock.Verify(x => x.TranslateFromBggAsync(bggGame), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromImportDataAsync(
            importData,
            false,
            GameState.Wanted,
            null,
            null), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ImportBggCollection Tests

    [Fact]
    public async Task ImportBggCollection_ShouldReturnNull_WhenApiReturnsFailureStatusCode()
    {
        var userName = "testuser";
        var response = new ApiResponse<BggApiCollection>(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            null,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().BeNull();

        _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnResultWithStatusCode_WhenApiReturnsNonOkSuccess()
    {
        var userName = "testuser";
        var response = new ApiResponse<BggApiCollection>(
            new HttpResponseMessage(HttpStatusCode.Accepted),
            null,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(HttpStatusCode.Accepted);
        result.Games.Should().BeEmpty();

        _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnResultWithEmptyGames_WhenContentIsNull()
    {
        var userName = "testuser";
        var response = new ApiResponse<BggApiCollection>(
            new HttpResponseMessage(HttpStatusCode.OK),
            null,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Games.Should().BeEmpty();

        _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnResultWithEmptyGames_WhenItemListIsNull()
    {
        var userName = "testuser";
        var collection = new BggApiCollection { Item = null! };
        var response = new ApiResponse<BggApiCollection>(
            new HttpResponseMessage(HttpStatusCode.OK),
            collection,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Games.Should().BeEmpty();

        _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnMappedGames_WhenApiReturnsItemsSuccessfully()
    {
        var userName = "testuser";
        var items = new List<Item>
        {
            new()
            {
                Objectid = 101,
                Name = new ImportName { Text = "Catan", Sortindex = 1 },
                Status = new Status
                {
                    Own = 1,
                    Prevowned = 0,
                    Fortrade = 0,
                    Want = 0,
                    LastModified = "2024-03-15 10:30:00"
                },
                Image = new BoardGameTracker.Common.Models.Bgg.Image { Text ="https://example.com/catan.jpg" },
                Subtype = "boardgame"
            }
        };
        var collection = new BggApiCollection { Item = items };
        var response = new ApiResponse<BggApiCollection>(
            new HttpResponseMessage(HttpStatusCode.OK),
            collection,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.ImportBggCollection(userName);

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Games.Should().HaveCount(1);

        var game = result.Games[0];
        game.BggId.Should().Be(101);
        game.Title.Should().Be("Catan");
        game.ImageUrl.Should().Be("https://example.com/catan.jpg");
        game.State.Should().Be(GameState.Owned);
        game.IsExpansion.Should().BeFalse();
        game.LastModified.Should().Be(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc));

        _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldSetIsExpansionTrue_WhenSubtypeIsBoardgameExpansion()
    {
        var userName = "testuser";
        var items = new List<Item>
        {
            new()
            {
                Objectid = 202,
                Name = new ImportName { Text = "Catan Expansion", Sortindex = 1 },
                Status = new Status
                {
                    Own = 1,
                    Prevowned = 0,
                    Fortrade = 0,
                    Want = 0,
                    LastModified = "2024-06-01 12:00:00"
                },
                Image = new BoardGameTracker.Common.Models.Bgg.Image { Text ="https://example.com/expansion.jpg" },
                Subtype = "boardgameexpansion"
            }
        };
        var collection = new BggApiCollection { Item = items };
        var response = new ApiResponse<BggApiCollection>(
            new HttpResponseMessage(HttpStatusCode.OK),
            collection,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.ImportBggCollection(userName);

        result!.Games.Should().HaveCount(1);
        result.Games[0].IsExpansion.Should().BeTrue();

        _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 0, 0, 1, GameState.Wanted)]
    [InlineData(0, 0, 1, 0, GameState.ForTrade)]
    [InlineData(0, 1, 0, 0, GameState.PreviouslyOwned)]
    [InlineData(1, 0, 0, 0, GameState.Owned)]
    public async Task ImportBggCollection_ShouldMapGameState_BasedOnStatusFlags(
        int own, int prevOwned, int forTrade, int want, GameState expectedState)
    {
        var userName = "testuser";
        var items = new List<Item>
        {
            new()
            {
                Objectid = 303,
                Name = new ImportName { Text = "State Game", Sortindex = 1 },
                Status = new Status
                {
                    Own = own,
                    Prevowned = prevOwned,
                    Fortrade = forTrade,
                    Want = want,
                    LastModified = "2024-01-01 00:00:00"
                },
                Image = new BoardGameTracker.Common.Models.Bgg.Image { Text ="https://example.com/img.jpg" },
                Subtype = "boardgame"
            }
        };
        var collection = new BggApiCollection { Item = items };
        var response = new ApiResponse<BggApiCollection>(
            new HttpResponseMessage(HttpStatusCode.OK),
            collection,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.ImportBggCollection(userName);

        result!.Games.Should().HaveCount(1);
        result.Games[0].State.Should().Be(expectedState);

        _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggCollection_ShouldReturnGamesSortedByName()
    {
        var userName = "testuser";
        var items = new List<Item>
        {
            new()
            {
                Objectid = 3,
                Name = new ImportName { Text = "Zombicide", Sortindex = 1 },
                Status = new Status { Own = 1, Prevowned = 0, Fortrade = 0, Want = 0, LastModified = "2024-01-01 00:00:00" },
                Image = new BoardGameTracker.Common.Models.Bgg.Image { Text ="z.jpg" },
                Subtype = "boardgame"
            },
            new()
            {
                Objectid = 1,
                Name = new ImportName { Text = "Agricola", Sortindex = 1 },
                Status = new Status { Own = 1, Prevowned = 0, Fortrade = 0, Want = 0, LastModified = "2024-01-01 00:00:00" },
                Image = new BoardGameTracker.Common.Models.Bgg.Image { Text ="a.jpg" },
                Subtype = "boardgame"
            },
            new()
            {
                Objectid = 2,
                Name = new ImportName { Text = "Catan", Sortindex = 1 },
                Status = new Status { Own = 1, Prevowned = 0, Fortrade = 0, Want = 0, LastModified = "2024-01-01 00:00:00" },
                Image = new BoardGameTracker.Common.Models.Bgg.Image { Text ="c.jpg" },
                Subtype = "boardgame"
            }
        };
        var collection = new BggApiCollection { Item = items };
        var response = new ApiResponse<BggApiCollection>(
            new HttpResponseMessage(HttpStatusCode.OK),
            collection,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _bggImportService.ImportBggCollection(userName);

        result!.Games.Should().HaveCount(3);
        result.Games[0].Title.Should().Be("Agricola");
        result.Games[1].Title.Should().Be("Catan");
        result.Games[2].Title.Should().Be("Zombicide");

        _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion", It.IsAny<CancellationToken>()), Times.Once);
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
        var rawGame = new BggRawGame
        {
            Thumbnail = "thumb.jpg",
            Image = "img1.jpg",
            Description = "Great game",
            Type = "boardgame"
        };
        var bggGame = new BggGame
        {
            Thumbnail = "thumb.jpg",
            Image = "img1.jpg",
            Description = "Great game",
            BggId = 1001
        };
        var apiGames = new BggApiGames { Games = [rawGame] };
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            apiGames,
            new RefitSettings(),
            null);
        var importData = new GameImportData
        {
            Title = "Game One",
            BggId = 1001,
            Description = "Great game",
            ImageUrl = "img1.jpg"
        };
        var createdGame = new Game("Game One") { Id = 10 };

        _bggApiMock
            .Setup(x => x.SearchGame(1001, 1, default))
            .ReturnsAsync(apiResponse);
        _bggGameTranslatorMock
            .Setup(x => x.TranslateRawGame(rawGame))
            .Returns(bggGame);
        _bggGameTranslatorMock
            .Setup(x => x.TranslateFromBggAsync(bggGame))
            .ReturnsAsync(importData);
        _gameFactoryMock
            .Setup(x => x.CreateFromImportDataAsync(
                importData,
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

        _bggApiMock.Verify(x => x.SearchGame(1001, 1, default), Times.Once);
        _bggGameTranslatorMock.Verify(x => x.TranslateRawGame(rawGame), Times.Once);
        _bggGameTranslatorMock.Verify(x => x.TranslateFromBggAsync(bggGame), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromImportDataAsync(
            importData,
            true,
            GameState.Owned,
            34.99m,
            addedDate), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportList_ShouldSkipGame_WhenSearchGameReturnsNull()
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
        var apiGames = new BggApiGames { Games = null };
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            apiGames,
            new RefitSettings(),
            null);

        _bggApiMock
            .Setup(x => x.SearchGame(9999, 1, default))
            .ReturnsAsync(apiResponse);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _bggImportService.ImportList(importGames);

        _bggApiMock.Verify(x => x.SearchGame(9999, 1, default), Times.Once);
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

        var rawGame = new BggRawGame
        {
            Thumbnail = "thumb.jpg",
            Image = "found.jpg",
            Description = "Found",
            Type = "boardgame"
        };
        var bggGame = new BggGame
        {
            Thumbnail = "thumb.jpg",
            Image = "found.jpg",
            Description = "Found",
            BggId = 100
        };
        var foundApiGames = new BggApiGames { Games = [rawGame] };
        var foundResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            foundApiGames,
            new RefitSettings(),
            null);
        var missingApiGames = new BggApiGames { Games = null };
        var missingResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            missingApiGames,
            new RefitSettings(),
            null);
        var importData = new GameImportData
        {
            Title = "Found Game",
            BggId = 100,
            Description = "Found",
            ImageUrl = "found.jpg"
        };
        var createdGame = new Game("Found Game") { Id = 50 };

        _bggApiMock
            .Setup(x => x.SearchGame(100, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foundResponse);
        _bggApiMock
            .Setup(x => x.SearchGame(200, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(missingResponse);
        _bggGameTranslatorMock
            .Setup(x => x.TranslateRawGame(rawGame))
            .Returns(bggGame);
        _bggGameTranslatorMock
            .Setup(x => x.TranslateFromBggAsync(bggGame))
            .ReturnsAsync(importData);
        _gameFactoryMock
            .Setup(x => x.CreateFromImportDataAsync(
                importData,
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

        _bggApiMock.Verify(x => x.SearchGame(100, 1, It.IsAny<CancellationToken>()), Times.Once);
        _bggApiMock.Verify(x => x.SearchGame(200, 1, It.IsAny<CancellationToken>()), Times.Once);
        _bggGameTranslatorMock.Verify(x => x.TranslateRawGame(rawGame), Times.Once);
        _bggGameTranslatorMock.Verify(x => x.TranslateFromBggAsync(bggGame), Times.Once);
        _gameFactoryMock.Verify(x => x.CreateFromImportDataAsync(
            importData,
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
    public async Task ImportList_ShouldMapBggSearchCorrectly_WhenProcessingGame()
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

        var rawGame = new BggRawGame
        {
            Thumbnail = "thumb.jpg",
            Image = "img.jpg",
            Description = "Mapping test",
            Type = "boardgame"
        };
        var bggGame = new BggGame
        {
            Thumbnail = "thumb.jpg",
            Image = "img.jpg",
            Description = "Mapping test",
            BggId = 555
        };
        var apiGames = new BggApiGames { Games = [rawGame] };
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            apiGames,
            new RefitSettings(),
            null);
        var importData = new GameImportData
        {
            Title = "Mapping Test Game",
            BggId = 555,
            Description = "Mapping test",
            ImageUrl = "img.jpg"
        };
        var createdGame = new Game("Mapping Test Game") { Id = 77 };

        _bggApiMock
            .Setup(x => x.SearchGame(555, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);
        _bggGameTranslatorMock
            .Setup(x => x.TranslateRawGame(rawGame))
            .Returns(bggGame);
        _bggGameTranslatorMock
            .Setup(x => x.TranslateFromBggAsync(bggGame))
            .ReturnsAsync(importData);
        _gameFactoryMock
            .Setup(x => x.CreateFromImportDataAsync(
                importData,
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

        _gameFactoryMock.Verify(x => x.CreateFromImportDataAsync(
            importData,
            true,
            GameState.ForTrade,
            19.50m,
            addedDate), Times.Once);

        _bggApiMock.Verify(x => x.SearchGame(555, 1, It.IsAny<CancellationToken>()), Times.Once);
        _bggGameTranslatorMock.Verify(x => x.TranslateRawGame(rawGame), Times.Once);
        _bggGameTranslatorMock.Verify(x => x.TranslateFromBggAsync(bggGame), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(createdGame), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
