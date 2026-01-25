using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Bgg;
using BoardGameTracker.Core.Bgg.Interfaces;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class GameServiceTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IGameSessionRepository> _gameSessionRepositoryMock;
    private readonly Mock<IGameStatisticsRepository> _gameStatisticsRepositoryMock;
    private readonly Mock<IBggApi> _bggApiMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<IBggGameTranslator> _bggGameTranslatorMock;
    private readonly Mock<IGameFactory> _gameFactoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IConfigFileProvider> _configFileProviderMock;
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameSessionRepositoryMock = new Mock<IGameSessionRepository>();
        _gameStatisticsRepositoryMock = new Mock<IGameStatisticsRepository>();
        _bggApiMock = new Mock<IBggApi>();
        _imageServiceMock = new Mock<IImageService>();
        _bggGameTranslatorMock = new Mock<IBggGameTranslator>();
        _gameFactoryMock = new Mock<IGameFactory>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _configFileProviderMock = new Mock<IConfigFileProvider>();

        _gameService = new GameService(
            _gameRepositoryMock.Object,
            _gameSessionRepositoryMock.Object,
            _gameStatisticsRepositoryMock.Object,
            _imageServiceMock.Object,
            _bggApiMock.Object,
            _bggGameTranslatorMock.Object,
            _gameFactoryMock.Object,
            _unitOfWorkMock.Object,
            _configFileProviderMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _gameSessionRepositoryMock.VerifyNoOtherCalls();
        _gameStatisticsRepositoryMock.VerifyNoOtherCalls();
        _bggApiMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
        _bggGameTranslatorMock.VerifyNoOtherCalls();
        _gameFactoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _configFileProviderMock.VerifyNoOtherCalls();
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
        var result = await _gameService.GetGameByBggId(bggId);

        // Assert
        result.Should().NotBeNull();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(bggId), Times.Once);
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

        // Act
        await _gameService.Delete(gameId);

        // Assert
        _imageServiceMock.Verify(x => x.DeleteImage("game-image.png"), Times.Once);
        _gameRepositoryMock.Verify(x => x.DeleteAsync(gameId), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldDoNothing_WhenGameDoesNotExist()
    {
        // Arrange
        var gameId = 999;

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync((Game?)null);

        // Act
        await _gameService.Delete(gameId);

        // Assert
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

    #region CreateGame Tests

    [Fact]
    public async Task CreateGame_ShouldCreateGame_AndSaveChanges()
    {
        // Arrange
        var game = new Game("New Game") { Id = 1 };

        _gameRepositoryMock
            .Setup(x => x.CreateAsync(game))
            .ReturnsAsync(game);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _gameService.CreateGame(game);

        // Assert
        result.Should().NotBeNull();

        _gameRepositoryMock.Verify(x => x.CreateAsync(game), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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
        var game = new Game("Updated Game") { Id = 1 };

        _gameRepositoryMock
            .Setup(x => x.UpdateAsync(game))
            .ReturnsAsync(game);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _gameService.UpdateGame(game);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Game");

        _gameRepositoryMock.Verify(x => x.UpdateAsync(game), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
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

    #region GetTotalPlayCount Tests

    [Fact]
    public async Task GetTotalPlayCount_ShouldReturnPlayCount()
    {
        // Arrange
        var gameId = 1;
        var expectedCount = 25;

        _gameSessionRepositoryMock
            .Setup(x => x.GetPlayCount(gameId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _gameService.GetTotalPlayCount(gameId);

        // Assert
        result.Should().Be(25);

        _gameSessionRepositoryMock.Verify(x => x.GetPlayCount(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetPlayFlags Tests

    [Fact]
    public async Task GetPlayFlags_ShouldReturnAllFlags_WhenDifferentValues()
    {
        // Arrange
        var gameId = 1;

        _gameSessionRepositoryMock
            .Setup(x => x.GetShortestPlay(gameId))
            .ReturnsAsync(30);

        _gameSessionRepositoryMock
            .Setup(x => x.GetLongestPlay(gameId))
            .ReturnsAsync(180);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetHighScorePlay(gameId))
            .ReturnsAsync(100);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetLowestScorePlay(gameId))
            .ReturnsAsync(50);

        // Act
        var result = await _gameService.GetPlayFlags(gameId);

        // Assert
        result.Should().ContainKey(SessionFlag.ShortestGame);
        result.Should().ContainKey(SessionFlag.LongestGame);
        result.Should().ContainKey(SessionFlag.HighestScore);
        result.Should().ContainKey(SessionFlag.LowestScore);
        result[SessionFlag.ShortestGame].Should().Be(30);
        result[SessionFlag.LongestGame].Should().Be(180);
    }

    [Fact]
    public async Task GetPlayFlags_ShouldNotIncludeLongestGame_WhenSameAsShortest()
    {
        // Arrange
        var gameId = 1;
        var samePlayTime = 60;

        _gameSessionRepositoryMock
            .Setup(x => x.GetShortestPlay(gameId))
            .ReturnsAsync(samePlayTime);

        _gameSessionRepositoryMock
            .Setup(x => x.GetLongestPlay(gameId))
            .ReturnsAsync(samePlayTime);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetHighScorePlay(gameId))
            .ReturnsAsync(100);

        _gameStatisticsRepositoryMock
            .Setup(x => x.GetLowestScorePlay(gameId))
            .ReturnsAsync(100);

        // Act
        var result = await _gameService.GetPlayFlags(gameId);

        // Assert
        result.Should().ContainKey(SessionFlag.ShortestGame);
        result.Should().NotContainKey(SessionFlag.LongestGame);
        result.Should().ContainKey(SessionFlag.HighestScore);
        result.Should().NotContainKey(SessionFlag.LowestScore);
    }

    #endregion

    #region GetShelfOfShameGames Tests

    [Fact]
    public async Task GetShelfOfShameGames_ShouldReturnGames_WithConfiguredMonths()
    {
        // Arrange
        var configuredMonths = 6;
        var games = new List<Game>
        {
            new Game("Unplayed Game 1") { Id = 1 },
            new Game("Unplayed Game 2") { Id = 2 }
        };

        _configFileProviderMock
            .Setup(x => x.ShelfOfShameMonths)
            .Returns(configuredMonths);

        _gameRepositoryMock
            .Setup(x => x.GetGamesWithNoRecentSessions(It.IsAny<DateTime>()))
            .ReturnsAsync(games);

        // Act
        var result = await _gameService.GetShelfOfShameGames();

        // Assert
        result.Should().HaveCount(2);

        _configFileProviderMock.Verify(x => x.ShelfOfShameMonths, Times.Once);
        _gameRepositoryMock.Verify(x => x.GetGamesWithNoRecentSessions(It.IsAny<DateTime>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetShelfOfShameGames_ShouldReturnEmptyList_WhenNoGamesMatchCriteria()
    {
        // Arrange
        var configuredMonths = 3;

        _configFileProviderMock
            .Setup(x => x.ShelfOfShameMonths)
            .Returns(configuredMonths);

        _gameRepositoryMock
            .Setup(x => x.GetGamesWithNoRecentSessions(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Game>());

        // Act
        var result = await _gameService.GetShelfOfShameGames();

        // Assert
        result.Should().BeEmpty();

        _configFileProviderMock.Verify(x => x.ShelfOfShameMonths, Times.Once);
        _gameRepositoryMock.Verify(x => x.GetGamesWithNoRecentSessions(It.IsAny<DateTime>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetShelfOfShameGames_ShouldUseCutoffDate_BasedOnConfiguredMonths()
    {
        // Arrange
        var configuredMonths = 12;
        DateTime capturedCutoffDate = default;

        _configFileProviderMock
            .Setup(x => x.ShelfOfShameMonths)
            .Returns(configuredMonths);

        _gameRepositoryMock
            .Setup(x => x.GetGamesWithNoRecentSessions(It.IsAny<DateTime>()))
            .Callback<DateTime>(date => capturedCutoffDate = date)
            .ReturnsAsync(new List<Game>());

        // Act
        await _gameService.GetShelfOfShameGames();

        // Assert
        var expectedCutoffDate = DateTime.UtcNow.AddMonths(-configuredMonths);
        capturedCutoffDate.Should().BeCloseTo(expectedCutoffDate, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region CountShelfOfShameGames Tests

    [Fact]
    public async Task CountShelfOfShameGames_ShouldReturnCount_WhenFeatureEnabled()
    {
        // Arrange
        var configuredMonths = 6;
        var expectedCount = 5;

        _configFileProviderMock
            .Setup(x => x.ShelfOfShameEnabled)
            .Returns(true);

        _configFileProviderMock
            .Setup(x => x.ShelfOfShameMonths)
            .Returns(configuredMonths);

        _gameRepositoryMock
            .Setup(x => x.CountGamesWithNoRecentSessions(It.IsAny<DateTime>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _gameService.CountShelfOfShameGames();

        // Assert
        result.Should().Be(expectedCount);

        _configFileProviderMock.Verify(x => x.ShelfOfShameEnabled, Times.Once);
        _configFileProviderMock.Verify(x => x.ShelfOfShameMonths, Times.Once);
        _gameRepositoryMock.Verify(x => x.CountGamesWithNoRecentSessions(It.IsAny<DateTime>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountShelfOfShameGames_ShouldReturnZero_WhenFeatureDisabled()
    {
        // Arrange
        _configFileProviderMock
            .Setup(x => x.ShelfOfShameEnabled)
            .Returns(false);

        // Act
        var result = await _gameService.CountShelfOfShameGames();

        // Assert
        result.Should().Be(0);

        _configFileProviderMock.Verify(x => x.ShelfOfShameEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountShelfOfShameGames_ShouldUseCutoffDate_BasedOnConfiguredMonths()
    {
        // Arrange
        var configuredMonths = 12;
        DateTime capturedCutoffDate = default;

        _configFileProviderMock
            .Setup(x => x.ShelfOfShameEnabled)
            .Returns(true);

        _configFileProviderMock
            .Setup(x => x.ShelfOfShameMonths)
            .Returns(configuredMonths);

        _gameRepositoryMock
            .Setup(x => x.CountGamesWithNoRecentSessions(It.IsAny<DateTime>()))
            .Callback<DateTime>(date => capturedCutoffDate = date)
            .ReturnsAsync(0);

        // Act
        await _gameService.CountShelfOfShameGames();

        // Assert
        var expectedCutoffDate = DateTime.UtcNow.AddMonths(-configuredMonths);
        capturedCutoffDate.Should().BeCloseTo(expectedCutoffDate, TimeSpan.FromSeconds(5));
    }

    #endregion
}
