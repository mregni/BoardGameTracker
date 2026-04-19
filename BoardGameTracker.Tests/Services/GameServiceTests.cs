using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class GameServiceTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IGameSessionRepository> _gameSessionRepositoryMock;
    private readonly Mock<IBoardGameGeekXmlApi2Client> _bggClientMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<GameService>> _loggerMock;
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameSessionRepositoryMock = new Mock<IGameSessionRepository>();
        _bggClientMock = new Mock<IBoardGameGeekXmlApi2Client>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock.Setup(x => x.GetBggApiKeyAsync()).ReturnsAsync("test-api-key");
        _imageServiceMock = new Mock<IImageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<GameService>>();

        _gameService = new GameService(
            _gameRepositoryMock.Object,
            _gameSessionRepositoryMock.Object,
            _imageServiceMock.Object,
            _bggClientMock.Object,
            _settingsServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _gameSessionRepositoryMock.VerifyNoOtherCalls();
        _bggClientMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

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

        result.Should().HaveCount(2);

        _gameRepositoryMock.Verify(x => x.GetGamesOverviewList(), Times.Once);
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
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        var result = await _gameService.GetGameById(gameId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(gameId);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameById_ShouldReturnNull_WhenGameDoesNotExist()
    {
        var gameId = 999;

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync((Game?)null);

        var result = await _gameService.GetGameById(gameId);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
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

        await _gameService.Delete(gameId);

        _imageServiceMock.Verify(x => x.DeleteImage("game-image.png"), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.DeleteAsync(gameId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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

        await action.Should().ThrowAsync<EntityNotFoundException>();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ShouldReturnGameCount()
    {
        _gameRepositoryMock
            .Setup(x => x.CountAsync())
            .ReturnsAsync(42);

        var result = await _gameService.CountAsync();

        result.Should().Be(42);

        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
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
            State = GameState.Owned,
            YearPublished = 2020,
            Image = "image.png",
            Description = "A great game",
            MinPlayers = 2,
            MaxPlayers = 4,
            MinPlayTime = 30,
            MaxPlayTime = 60,
            MinAge = 10,
            BggId = 12345,
            BuyingPrice = 49.99m,
            AdditionDate = new DateTime(2023, 1, 15)
        };

        _gameRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Game>()))
            .ReturnsAsync((Game g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.CreateGameFromCommand(command);

        result.Should().NotBeNull();
        result.Title.Should().Be("New Game");
        result.HasScoring.Should().BeTrue();
        result.State.Should().Be(GameState.Owned);

        _gameRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Game>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
            .Setup(x => x.CreateAsync(It.IsAny<Game>()))
            .ReturnsAsync((Game g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.CreateGameFromCommand(command);

        result.Should().NotBeNull();
        result.Title.Should().Be("Simple Game");
        result.HasScoring.Should().BeFalse();
        result.State.Should().Be(GameState.Wanted);

        _gameRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Game>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
            State = GameState.Owned,
            Description = "Updated description",
            BuyingPrice = 39.99m,
            SoldPrice = 25.00m,
            Rating = 7.5,
            Weight = 2.8
        };

        var existingGame = new Game("Original Game", false) { Id = 1 };

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
        result.State.Should().Be(GameState.Owned);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
            .Setup(x => x.GetExpansions(expansionIds))
            .ReturnsAsync(expansions);

        var result = await _gameService.GetGameExpansions(expansionIds);

        result.Should().HaveCount(3);

        _gameRepositoryMock.Verify(x => x.GetExpansions(expansionIds), Times.Once);
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
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _gameService.DeleteExpansion(gameId, expansionId);

        _gameRepositoryMock.Verify(x => x.DeleteExpansion(gameId, expansionId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByGameId(gameId, count))
            .ReturnsAsync(sessions);

        var result = await _gameService.GetSessionsForGame(gameId, count);

        result.Should().HaveCount(2);

        _gameSessionRepositoryMock.Verify(x => x.GetSessionsByGameId(gameId, count), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region SearchExpansionsForGame Tests

    [Fact]
    public async Task SearchExpansionsForGame_ShouldThrowBggFeatureDisabledException_WhenApiKeyIsNull()
    {
        _settingsServiceMock
            .Setup(x => x.GetBggApiKeyAsync())
            .ReturnsAsync((string?)null);

        var action = async () => await _gameService.SearchExpansionsForGame(1);

        await action.Should().ThrowAsync<BggFeatureDisabledException>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldThrowBggFeatureDisabledException_WhenApiKeyIsEmpty()
    {
        _settingsServiceMock
            .Setup(x => x.GetBggApiKeyAsync())
            .ReturnsAsync(string.Empty);

        var action = async () => await _gameService.SearchExpansionsForGame(1);

        await action.Should().ThrowAsync<BggFeatureDisabledException>();

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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(CreateFailedThingResponse());

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(CreateSucceededThingResponse([]));

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(thingResponse);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Title == "Expansion One" && e.BggId == 101);
        result.Should().Contain(e => e.Title == "Expansion Two" && e.BggId == 102);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(thingResponse);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().HaveCount(1);
        result.Should().Contain(e => e.Title == "Expansion One" && e.BggId == 101);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
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
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(thingResponse);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateGameExpansions Tests

    [Fact]
    public async Task UpdateGameExpansions_ShouldThrowBggFeatureDisabledException_WhenApiKeyIsNull()
    {
        _settingsServiceMock
            .Setup(x => x.GetBggApiKeyAsync())
            .ReturnsAsync((string?)null);

        var action = async () => await _gameService.UpdateGameExpansions(1, [100]);

        await action.Should().ThrowAsync<BggFeatureDisabledException>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldReturnEmptyList_WhenGameDoesNotExist()
    {
        var gameId = 999;

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync((Game?)null);

        var result = await _gameService.UpdateGameExpansions(gameId, [100]);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
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
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, []);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(thingResponse);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, [expansionBggId]);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("New Expansion");
        result[0].BggId.Should().Be(expansionBggId);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldSkipExpansion_WhenBggApiReturnsEmptyResult()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.IsAny<ThingRequest>()))
            .ReturnsAsync(CreateSucceededThingResponse([]));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, [101]);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, [existingBggId]);

        result.Should().HaveCount(1);
        result[0].BggId.Should().Be(existingBggId);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldCallBggApiForEachNewExpansion_AndSaveOnce()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };

        var item1 = CreateThingItem(101, "Expansion One");
        var item2 = CreateThingItem(102, "Expansion Two");

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => r.Ids.Contains(101))))
            .ReturnsAsync(CreateSucceededThingResponse([item1]));

        _bggClientMock
            .Setup(x => x.GetThingAsync(It.Is<ThingRequest>(r => r.Ids.Contains(102))))
            .ReturnsAsync(CreateSucceededThingResponse([item2]));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameService.UpdateGameExpansions(gameId, [101, 102]);

        result.Should().HaveCount(2);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggClientMock.Verify(x => x.GetThingAsync(It.IsAny<ThingRequest>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
