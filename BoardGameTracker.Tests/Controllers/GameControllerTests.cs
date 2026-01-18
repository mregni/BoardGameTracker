using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class GameControllerTests
{
    private readonly Mock<IGameService> _gameServiceMock;
    private readonly Mock<IGameStatisticsService> _gameStatisticsDomainServiceMock;
    private readonly GameController _controller;

    public GameControllerTests()
    {
        _gameServiceMock = new Mock<IGameService>();
        _gameStatisticsDomainServiceMock = new Mock<IGameStatisticsService>();
        _controller = new GameController(_gameServiceMock.Object, _gameStatisticsDomainServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameServiceMock.VerifyNoOtherCalls();
        _gameStatisticsDomainServiceMock.VerifyNoOtherCalls();
    }

    #region GetGames Tests

    [Fact]
    public async Task GetGames_ShouldReturnOkWithGames_WhenGamesExist()
    {
        // Arrange
        var games = new List<Game>
        {
            new Game("Catan", true) { Id = 1 },
            new Game("Ticket to Ride", false) { Id = 2 }
        };

        _gameServiceMock
            .Setup(x => x.GetGames())
            .ReturnsAsync(games);

        // Act
        var result = await _controller.GetGames();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGames = okResult.Value.Should().BeAssignableTo<List<GameDto>>().Subject;

        returnedGames.Should().HaveCount(2);
        returnedGames[0].Title.Should().Be("Catan");
        returnedGames[0].HasScoring.Should().BeTrue();
        returnedGames[1].Title.Should().Be("Ticket to Ride");
        returnedGames[1].HasScoring.Should().BeFalse();

        _gameServiceMock.Verify(x => x.GetGames(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGames_ShouldReturnOkWithEmptyList_WhenNoGamesExist()
    {
        // Arrange
        _gameServiceMock
            .Setup(x => x.GetGames())
            .ReturnsAsync(new List<Game>());

        // Act
        var result = await _controller.GetGames();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGames = okResult.Value.Should().BeAssignableTo<List<GameDto>>().Subject;

        returnedGames.Should().BeEmpty();

        _gameServiceMock.Verify(x => x.GetGames(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CreateGame Tests

    [Fact]
    public async Task CreateGame_ShouldReturnOkWithCreatedGame_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateGameCommand
        {
            Title = "New Game",
            HasScoring = true,
            State = GameState.Owned
        };

        var createdGame = new Game(command.Title, command.HasScoring, command.State) { Id = 1 };

        _gameServiceMock
            .Setup(x => x.CreateGameFromCommand(command))
            .ReturnsAsync(createdGame);

        // Act
        var result = await _controller.CreateGame(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gameDto = okResult.Value.Should().BeAssignableTo<GameDto>().Subject;

        gameDto.Id.Should().Be(1);
        gameDto.Title.Should().Be("New Game");
        gameDto.HasScoring.Should().BeTrue();

        _gameServiceMock.Verify(x => x.CreateGameFromCommand(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateGame_ShouldReturnBadRequest_WhenCommandIsNull()
    {
        // Act
        var result = await _controller.CreateGame(null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateGame Tests

    [Fact]
    public async Task UpdateGame_ShouldReturnOkWithUpdatedGame_WhenDtoIsValid()
    {
        // Arrange
        var dto = new GameDto
        {
            Id = 1,
            Title = "Updated Game",
            HasScoring = true,
            State = GameState.Owned
        };

        var updatedGame = new Game(dto.Title, dto.HasScoring, dto.State) { Id = dto.Id };

        _gameServiceMock
            .Setup(x => x.UpdateGame(It.IsAny<Game>()))
            .ReturnsAsync(updatedGame);

        // Act
        var result = await _controller.UpdateGame(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gameDto = okResult.Value.Should().BeAssignableTo<GameDto>().Subject;

        gameDto.Id.Should().Be(1);
        gameDto.Title.Should().Be("Updated Game");

        _gameServiceMock.Verify(x => x.UpdateGame(It.IsAny<Game>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldReturnBadRequest_WhenDtoIsNull()
    {
        // Act
        var result = await _controller.UpdateGame(null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldReturnInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var dto = new GameDto
        {
            Id = 1,
            Title = "Test Game",
            State = GameState.Owned
        };

        _gameServiceMock
            .Setup(x => x.UpdateGame(It.IsAny<Game>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _controller.UpdateGame(dto);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        _gameServiceMock.Verify(x => x.UpdateGame(It.IsAny<Game>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region DeleteGameById Tests

    [Fact]
    public async Task DeleteGameById_ShouldReturnOkWithSuccess_WhenGameIsDeleted()
    {
        // Arrange
        var gameId = 1;

        _gameServiceMock
            .Setup(x => x.Delete(gameId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteGameById(gameId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        _gameServiceMock.Verify(x => x.Delete(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetGameById Tests

    [Fact]
    public async Task GetGameById_ShouldReturnOkWithGame_WhenGameExists()
    {
        // Arrange
        var gameId = 1;
        var game = new Game("Catan", true) { Id = gameId };

        _gameServiceMock
            .Setup(x => x.GetGameById(gameId))
            .ReturnsAsync(game);

        // Act
        var result = await _controller.GetGameById(gameId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gameDto = okResult.Value.Should().BeAssignableTo<GameDto>().Subject;

        gameDto.Id.Should().Be(gameId);
        gameDto.Title.Should().Be("Catan");

        _gameServiceMock.Verify(x => x.GetGameById(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameById_ShouldReturnNotFound_WhenGameDoesNotExist()
    {
        // Arrange
        var gameId = 999;

        _gameServiceMock
            .Setup(x => x.GetGameById(gameId))
            .ReturnsAsync((Game?)null);

        // Act
        var result = await _controller.GetGameById(gameId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _gameServiceMock.Verify(x => x.GetGameById(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetGameSessionsById Tests

    [Fact]
    public async Task GetGameSessionsById_ShouldReturnOkWithSessions_WhenSessionsExist()
    {
        // Arrange
        var gameId = 1;
        int? count = 10;
        var sessions = new List<Session>
        {
            new Session(gameId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1).AddHours(2), "Session 1") { Id = 1 },
            new Session(gameId, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Session 2") { Id = 2 }
        };

        _gameServiceMock
            .Setup(x => x.GetSessionsForGame(gameId, count))
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetGameSessionsById(gameId, count);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;

        returnedSessions.Should().HaveCount(2);

        _gameServiceMock.Verify(x => x.GetSessionsForGame(gameId, count), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameSessionsById_ShouldReturnOkWithEmptyList_WhenNoSessionsExist()
    {
        // Arrange
        var gameId = 1;
        int? count = null;

        _gameServiceMock
            .Setup(x => x.GetSessionsForGame(gameId, count))
            .ReturnsAsync(new List<Session>());

        // Act
        var result = await _controller.GetGameSessionsById(gameId, count);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;

        returnedSessions.Should().BeEmpty();

        _gameServiceMock.Verify(x => x.GetSessionsForGame(gameId, count), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetGameExpansions Tests

    [Fact]
    public async Task GetGameExpansions_ShouldReturnOkWithExpansions_WhenExpansionsExist()
    {
        // Arrange
        var gameId = 1;
        var expansions = new[]
        {
            new BggLink { Id = 100, Value = "Expansion 1" },
            new BggLink { Id = 101, Value = "Expansion 2" }
        };

        _gameServiceMock
            .Setup(x => x.SearchExpansionsForGame(gameId))
            .ReturnsAsync(expansions);

        // Act
        var result = await _controller.GetGameExpansions(gameId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedExpansions = okResult.Value.Should().BeAssignableTo<BggLink[]>().Subject;

        returnedExpansions.Should().HaveCount(2);

        _gameServiceMock.Verify(x => x.SearchExpansionsForGame(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateGameExpansions Tests

    [Fact]
    public async Task UpdateGameExpansions_ShouldReturnOkWithUpdatedExpansions()
    {
        // Arrange
        var gameId = 1;
        var command = new UpdateGameExpansionsCommand { ExpansionBggIds = new[] { 100, 101, 102 } };
        var expansions = new List<Expansion>
        {
            new Expansion("Expansion 1", 100, gameId) { Id = 1 },
            new Expansion("Expansion 2", 101, gameId) { Id = 2 }
        };

        _gameServiceMock
            .Setup(x => x.UpdateGameExpansions(gameId, command.ExpansionBggIds))
            .ReturnsAsync(expansions);

        // Act
        var result = await _controller.UpdateGameExpansions(gameId, command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedExpansions = okResult.Value.Should().BeAssignableTo<List<ExpansionDto>>().Subject;

        returnedExpansions.Should().HaveCount(2);

        _gameServiceMock.Verify(x => x.UpdateGameExpansions(gameId, command.ExpansionBggIds), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region DeleteGameExpansions Tests

    [Fact]
    public async Task DeleteGameExpansions_ShouldReturnOkWithSuccess()
    {
        // Arrange
        var gameId = 1;
        var expansionId = 100;

        _gameServiceMock
            .Setup(x => x.DeleteExpansion(gameId, expansionId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteGameExpansions(gameId, expansionId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        _gameServiceMock.Verify(x => x.DeleteExpansion(gameId, expansionId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetGameStatistics Tests

    [Fact]
    public async Task GetGameStatistics_ShouldReturnOkWithAllStatisticsData()
    {
        // Arrange
        var gameId = 1;
        var stats = new GameStatistics
        {
            PlayCount = 50,
            TotalPlayedTime = 3000
        };
        var topPlayers = new List<TopPlayerDto>();
        var playByDayChart = new List<PlayByDay>();
        var playerCountChart = new List<PlayerCount>();
        var playerScoringChart = new Dictionary<DateTime, XValue[]>();
        var scoringRankChart = new List<ScoreRank>();

        _gameStatisticsDomainServiceMock
            .Setup(x => x.CalculateStatisticsAsync(gameId))
            .ReturnsAsync(stats);

        _gameServiceMock
            .Setup(x => x.GetTopPlayers(gameId))
            .ReturnsAsync(topPlayers);

        _gameServiceMock
            .Setup(x => x.GetPlayByDayChart(gameId))
            .ReturnsAsync(playByDayChart);

        _gameServiceMock
            .Setup(x => x.GetPlayerCountChart(gameId))
            .ReturnsAsync(playerCountChart);

        _gameServiceMock
            .Setup(x => x.GetPlayerScoringChart(gameId))
            .ReturnsAsync(playerScoringChart);

        _gameServiceMock
            .Setup(x => x.GetScoringRankedChart(gameId))
            .ReturnsAsync(scoringRankChart);

        // Act
        var result = await _controller.GetGameStatistics(gameId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        _gameStatisticsDomainServiceMock.Verify(x => x.CalculateStatisticsAsync(gameId), Times.Once);
        _gameServiceMock.Verify(x => x.GetTopPlayers(gameId), Times.Once);
        _gameServiceMock.Verify(x => x.GetPlayByDayChart(gameId), Times.Once);
        _gameServiceMock.Verify(x => x.GetPlayerCountChart(gameId), Times.Once);
        _gameServiceMock.Verify(x => x.GetPlayerScoringChart(gameId), Times.Once);
        _gameServiceMock.Verify(x => x.GetScoringRankedChart(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ImportBgg Tests

    [Fact]
    public async Task ImportBgg_ShouldReturnOkWithResult_WhenUsernameIsValid()
    {
        // Arrange
        var username = "testuser";
        var importResult = new BggImportResult();

        _gameServiceMock
            .Setup(x => x.ImportBggCollection(username))
            .ReturnsAsync(importResult);

        // Act
        var result = await _controller.ImportBgg(username);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<BggImportResult>();

        _gameServiceMock.Verify(x => x.ImportBggCollection(username), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggGames_ShouldReturnOkWithSuccess_WhenCommandIsValid()
    {
        // Arrange
        var command = new ImportBggGamesCommand
        {
            Games = new List<ImportGame>
            {
                new ImportGame { BggId = 1, Title = "Game 1", ImageUrl = "https://example.com/img1.jpg" },
                new ImportGame { BggId = 2, Title = "Game 2", ImageUrl = "https://example.com/img2.jpg" }
            }
        };

        _gameServiceMock
            .Setup(x => x.ImportList(command.Games))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ImportBggGames(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        _gameServiceMock.Verify(x => x.ImportList(command.Games), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggGames_ShouldReturnInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var command = new ImportBggGamesCommand
        {
            Games = new List<ImportGame>()
        };

        _gameServiceMock
            .Setup(x => x.ImportList(command.Games))
            .ThrowsAsync(new Exception("Import error"));

        // Act
        var result = await _controller.ImportBggGames(command);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        _gameServiceMock.Verify(x => x.ImportList(command.Games), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region SearchOnBgg Tests

    [Fact]
    public async Task SearchOnBgg_ShouldReturnExistingGame_WhenGameAlreadyExists()
    {
        // Arrange
        var search = new BggSearch { BggId = 123 };
        var existingGame = new Game("Existing Game", true) { Id = 1 };

        _gameServiceMock
            .Setup(x => x.GetGameByBggId(search.BggId))
            .ReturnsAsync(existingGame);

        // Act
        var result = await _controller.SearchOnBgg(search);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gameDto = okResult.Value.Should().BeAssignableTo<GameDto>().Subject;

        gameDto.Title.Should().Be("Existing Game");

        _gameServiceMock.Verify(x => x.GetGameByBggId(search.BggId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchOnBgg_ShouldReturnBadRequest_WhenGameNotFoundOnBgg()
    {
        // Arrange
        var search = new BggSearch { BggId = 999 };

        _gameServiceMock
            .Setup(x => x.GetGameByBggId(search.BggId))
            .ReturnsAsync((Game?)null);

        _gameServiceMock
            .Setup(x => x.SearchGame(search.BggId))
            .ReturnsAsync((BggGame?)null);

        // Act
        var result = await _controller.SearchOnBgg(search);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        _gameServiceMock.Verify(x => x.GetGameByBggId(search.BggId), Times.Once);
        _gameServiceMock.Verify(x => x.SearchGame(search.BggId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchOnBgg_ShouldReturnNewGame_WhenGameFoundOnBgg()
    {
        // Arrange
        var search = new BggSearch { BggId = 123 };
        var bggGame = new BggGame
        {
            BggId = 123,
            Names = new[] { "BGG Game" },
            Thumbnail = "https://example.com/thumb.jpg",
            Image = "https://example.com/img.jpg",
            Description = "A great game"
        };
        var newGame = new Game("BGG Game", true) { Id = 1 };

        _gameServiceMock
            .Setup(x => x.GetGameByBggId(search.BggId))
            .ReturnsAsync((Game?)null);

        _gameServiceMock
            .Setup(x => x.SearchGame(search.BggId))
            .ReturnsAsync(bggGame);

        _gameServiceMock
            .Setup(x => x.SearchOnBgg(bggGame, search))
            .ReturnsAsync(newGame);

        // Act
        var result = await _controller.SearchOnBgg(search);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gameDto = okResult.Value.Should().BeAssignableTo<GameDto>().Subject;

        gameDto.Title.Should().Be("BGG Game");

        _gameServiceMock.Verify(x => x.GetGameByBggId(search.BggId), Times.Once);
        _gameServiceMock.Verify(x => x.SearchGame(search.BggId), Times.Once);
        _gameServiceMock.Verify(x => x.SearchOnBgg(bggGame, search), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
