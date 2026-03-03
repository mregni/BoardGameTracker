using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Bgg.Interfaces;
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
    private readonly Mock<IBggApi> _bggApiMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<IBggGameTranslator> _bggGameTranslatorMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<GameService>> _loggerMock;
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameSessionRepositoryMock = new Mock<IGameSessionRepository>();
        _bggApiMock = new Mock<IBggApi>();
        _imageServiceMock = new Mock<IImageService>();
        _bggGameTranslatorMock = new Mock<IBggGameTranslator>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<GameService>>();

        _gameService = new GameService(
            _gameRepositoryMock.Object,
            _gameSessionRepositoryMock.Object,
            _imageServiceMock.Object,
            _bggApiMock.Object,
            _bggGameTranslatorMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _gameSessionRepositoryMock.VerifyNoOtherCalls();
        _bggApiMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
        _bggGameTranslatorMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    #region GetGames Tests

    [Fact]
    public async Task GetGames_ShouldReturnAllGames()
    {
        // Arrange
        var games = new List<Game>
        {
            new Game("Game 1") { Id = 1 },
            new Game("Game 2") { Id = 2 }
        };

        _gameRepositoryMock
            .Setup(x => x.GetGamesOverviewList())
            .ReturnsAsync(games);

        // Act
        var result = await _gameService.GetGames();

        // Assert
        result.Should().HaveCount(2);

        _gameRepositoryMock.Verify(x => x.GetGamesOverviewList(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGames_ShouldReturnEmptyList_WhenNoGamesExist()
    {
        // Arrange
        _gameRepositoryMock
            .Setup(x => x.GetGamesOverviewList())
            .ReturnsAsync(new List<Game>());

        // Act
        var result = await _gameService.GetGames();

        // Assert
        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetGamesOverviewList(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetGameById Tests

    [Fact]
    public async Task GetGameById_ShouldReturnGame_WhenGameExists()
    {
        // Arrange
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        // Act
        var result = await _gameService.GetGameById(gameId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(gameId);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameById_ShouldReturnNull_WhenGameDoesNotExist()
    {
        // Arrange
        var gameId = 999;

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync((Game?)null);

        // Act
        var result = await _gameService.GetGameById(gameId);

        // Assert
        result.Should().BeNull();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldDeleteGame_WhenGameExists()
    {
        // Arrange
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

        // Act
        await _gameService.Delete(gameId);

        // Assert
        _imageServiceMock.Verify(x => x.DeleteImage("game-image.png"), Times.Once);
        _gameRepositoryMock.Verify(x => x.DeleteAsync(gameId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldThrowEntityNotFoundException_WhenGameDoesNotExist()
    {
        // Arrange
        var gameId = 999;

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync((Game?)null);

        // Act
        var action = async () => await _gameService.Delete(gameId);

        // Assert
        await action.Should().ThrowAsync<EntityNotFoundException>();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ShouldReturnGameCount()
    {
        // Arrange
        _gameRepositoryMock
            .Setup(x => x.CountAsync())
            .ReturnsAsync(42);

        // Act
        var result = await _gameService.CountAsync();

        // Assert
        result.Should().Be(42);

        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CreateGameFromCommand Tests

    [Fact]
    public async Task CreateGameFromCommand_ShouldCreateGame_WithAllProperties()
    {
        // Arrange
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

        // Act
        var result = await _gameService.CreateGameFromCommand(command);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Game");
        result.HasScoring.Should().BeTrue();
        result.State.Should().Be(GameState.Owned);

        _gameRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Game>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateGameFromCommand_ShouldCreateGame_WithMinimalProperties()
    {
        // Arrange
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

        // Act
        var result = await _gameService.CreateGameFromCommand(command);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Simple Game");
        result.HasScoring.Should().BeFalse();
        result.State.Should().Be(GameState.Wanted);
    }

    #endregion

    #region UpdateGame Tests

    [Fact]
    public async Task UpdateGame_ShouldUpdateGame_AndSaveChanges()
    {
        // Arrange
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

        // Act
        var result = await _gameService.UpdateGame(command);

        // Assert
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
        // Arrange
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

        // Act
        var action = async () => await _gameService.UpdateGame(command);

        // Assert
        await action.Should().ThrowAsync<EntityNotFoundException>();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(999), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetGameExpansions Tests

    [Fact]
    public async Task GetGameExpansions_ShouldReturnExpansions()
    {
        // Arrange
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

        // Act
        var result = await _gameService.GetGameExpansions(expansionIds);

        // Assert
        result.Should().HaveCount(3);

        _gameRepositoryMock.Verify(x => x.GetExpansions(expansionIds), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region DeleteExpansion Tests

    [Fact]
    public async Task DeleteExpansion_ShouldDeleteExpansion_AndSaveChanges()
    {
        // Arrange
        var gameId = 1;
        var expansionId = 5;

        _gameRepositoryMock
            .Setup(x => x.DeleteExpansion(gameId, expansionId))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _gameService.DeleteExpansion(gameId, expansionId);

        // Assert
        _gameRepositoryMock.Verify(x => x.DeleteExpansion(gameId, expansionId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetSessionsForGame Tests

    [Fact]
    public async Task GetSessionsForGame_ShouldReturnSessions()
    {
        // Arrange
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

        // Act
        var result = await _gameService.GetSessionsForGame(gameId, count);

        // Assert
        result.Should().HaveCount(2);

        _gameSessionRepositoryMock.Verify(x => x.GetSessionsByGameId(gameId, count), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
