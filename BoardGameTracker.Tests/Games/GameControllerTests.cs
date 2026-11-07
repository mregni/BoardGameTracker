using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class GameControllerTests
{
    private readonly CancellationToken _token = new CancellationTokenSource().Token;
    private readonly Mock<IGameService> _gameServiceMock;
    private readonly Mock<IGameStatisticsService> _gameStatisticsDomainServiceMock;
    private readonly Mock<IBggImportService> _bggImportServiceMock;
    private readonly Mock<IGameChartService> _gameChartServiceMock;
    private readonly Mock<IShameService> _shameServiceMock;
    private readonly GameController _controller;

    public GameControllerTests()
    {
        _gameServiceMock = new Mock<IGameService>();
        _gameStatisticsDomainServiceMock = new Mock<IGameStatisticsService>();
        _bggImportServiceMock = new Mock<IBggImportService>();
        _gameChartServiceMock = new Mock<IGameChartService>();
        _shameServiceMock = new Mock<IShameService>();
        _controller = new GameController(
            _gameServiceMock.Object,
            _gameStatisticsDomainServiceMock.Object,
            _bggImportServiceMock.Object,
            _gameChartServiceMock.Object,
            _shameServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameServiceMock.VerifyNoOtherCalls();
        _gameStatisticsDomainServiceMock.VerifyNoOtherCalls();
        _bggImportServiceMock.VerifyNoOtherCalls();
        _gameChartServiceMock.VerifyNoOtherCalls();
        _shameServiceMock.VerifyNoOtherCalls();
    }

    #region GetGames Tests

    [Fact]
    public async Task GetGames_ShouldReturnOkWithGames_WhenGamesExist()
    {
        var games = new List<Game>
        {
            new Game("Catan", true) { Id = 1 },
            new Game("Ticket to Ride", false) { Id = 2 }
        };

        var lastPlayed = new DateTime(2026, 10, 1, 20, 0, 0, DateTimeKind.Utc);

        _gameServiceMock
            .Setup(x => x.GetGames())
            .ReturnsAsync(games);
        _gameServiceMock
            .Setup(x => x.GetPlayStatsPerGame())
            .ReturnsAsync(new Dictionary<int, GamePlayStats> { [1] = new(3, lastPlayed) });

        var result = await _controller.GetGames();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGames = okResult.Value.Should().BeAssignableTo<List<GameDto>>().Subject;

        returnedGames.Should().HaveCount(2);
        returnedGames[0].Title.Should().Be("Catan");
        returnedGames[0].HasScoring.Should().BeTrue();
        returnedGames[0].PlayCount.Should().Be(3);
        returnedGames[0].LastPlayed.Should().Be(lastPlayed);
        returnedGames[1].Title.Should().Be("Ticket to Ride");
        returnedGames[1].HasScoring.Should().BeFalse();
        returnedGames[1].PlayCount.Should().Be(0);
        returnedGames[1].LastPlayed.Should().BeNull();

        _gameServiceMock.Verify(x => x.GetGames(), Times.Once);
        _gameServiceMock.Verify(x => x.GetPlayStatsPerGame(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGames_ShouldReturnOkWithEmptyList_WhenNoGamesExist()
    {
        _gameServiceMock
            .Setup(x => x.GetGames())
            .ReturnsAsync([]);
        _gameServiceMock
            .Setup(x => x.GetPlayStatsPerGame())
            .ReturnsAsync([]);

        var result = await _controller.GetGames();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGames = okResult.Value.Should().BeAssignableTo<List<GameDto>>().Subject;

        returnedGames.Should().BeEmpty();

        _gameServiceMock.Verify(x => x.GetGames(), Times.Once);
        _gameServiceMock.Verify(x => x.GetPlayStatsPerGame(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CreateGame Tests

    [Fact]
    public async Task CreateGame_ShouldReturnCreatedWithGame_WhenCommandIsValid()
    {
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

        var result = await _controller.CreateGame(command);

        var okResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var gameDto = okResult.Value.Should().BeAssignableTo<GameDto>().Subject;

        okResult.ActionName.Should().Be(nameof(GameController.GetGameById));
        okResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);
        gameDto.Id.Should().Be(1);
        gameDto.Title.Should().Be("New Game");
        gameDto.HasScoring.Should().BeTrue();

        _gameServiceMock.Verify(x => x.CreateGameFromCommand(command), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateGame Tests

    [Fact]
    public async Task UpdateGame_ShouldReturnOkWithUpdatedGame_WhenCommandIsValid()
    {
        var command = new UpdateGameCommand
        {
            Id = 1,
            Title = "Updated Game",
            HasScoring = true,
            State = GameState.Owned
        };

        var updatedGame = new Game(command.Title, command.HasScoring, command.State) { Id = command.Id };

        _gameServiceMock
            .Setup(x => x.UpdateGame(command))
            .ReturnsAsync(updatedGame);

        var result = await _controller.UpdateGame(command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gameDto = okResult.Value.Should().BeAssignableTo<GameDto>().Subject;

        gameDto.Id.Should().Be(1);
        gameDto.Title.Should().Be("Updated Game");

        _gameServiceMock.Verify(x => x.UpdateGame(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldReturnTheCoopMode()
    {
        var command = new UpdateGameCommand
        {
            Id = 4,
            Title = "Spirit Island",
            HasCoopMode = true,
            CoopByDefault = true,
        };

        var updatedGame = new Game(command.Title) { Id = command.Id };
        updatedGame.UpdateCoopMode(true, true);

        _gameServiceMock
            .Setup(x => x.UpdateGame(command))
            .ReturnsAsync(updatedGame);

        var result = await _controller.UpdateGame(command);

        var gameDto = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeAssignableTo<GameDto>().Subject;
        gameDto.HasCoopMode.Should().BeTrue();
        gameDto.CoopByDefault.Should().BeTrue();

        _gameServiceMock.Verify(x => x.UpdateGame(command), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region DeleteGameById Tests

    [Fact]
    public async Task DeleteGameById_ShouldReturnNoContent_WhenGameIsDeleted()
    {
        var gameId = 1;

        _gameServiceMock
            .Setup(x => x.Delete(gameId))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteGameById(gameId);

        result.Should().BeOfType<NoContentResult>();

        _gameServiceMock.Verify(x => x.Delete(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetGameById Tests

    [Fact]
    public async Task GetGameById_ShouldReturnOkWithGame_WhenGameExists()
    {
        var gameId = 1;
        var game = new Game("Catan", true) { Id = gameId };

        _gameServiceMock
            .Setup(x => x.GetGameById(gameId))
            .ReturnsAsync(game);

        var result = await _controller.GetGameById(gameId);

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
        var gameId = 999;

        _gameServiceMock
            .Setup(x => x.GetGameById(gameId))
            .ReturnsAsync((Game?)null);

        var result = await _controller.GetGameById(gameId);

        result.Should().BeOfType<NotFoundResult>();

        _gameServiceMock.Verify(x => x.GetGameById(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetGameSessionsById Tests

    [Fact]
    public async Task GetGameSessionsById_ShouldReturnOkWithSessions_WhenSessionsExist()
    {
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

        var result = await _controller.GetGameSessionsById(gameId, count);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;

        returnedSessions.Select(x => x.Id).Should().Equal(1, 2);

        _gameServiceMock.Verify(x => x.GetSessionsForGame(gameId, count), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameSessionsById_ShouldReturnOkWithEmptyList_WhenNoSessionsExist()
    {
        var gameId = 1;
        int? count = null;

        _gameServiceMock
            .Setup(x => x.GetSessionsForGame(gameId, count))
            .ReturnsAsync([]);

        var result = await _controller.GetGameSessionsById(gameId, count);

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
        var gameId = 1;
        var expansions = new[]
        {
            new ExpansionData { BggId = 100, Title = "Expansion 1" },
            new ExpansionData { BggId = 101, Title = "Expansion 2" }
        };

        _gameServiceMock
            .Setup(x => x.SearchExpansionsForGame(gameId))
            .ReturnsAsync(expansions);

        var result = await _controller.GetGameExpansions(gameId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedExpansions = okResult.Value.Should().BeAssignableTo<ExpansionData[]>().Subject;

        returnedExpansions.Should().BeSameAs(expansions);

        _gameServiceMock.Verify(x => x.SearchExpansionsForGame(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateGameExpansions Tests

    [Fact]
    public async Task UpdateGameExpansions_ShouldReturnOkWithUpdatedExpansions()
    {
        var gameId = 1;
        var command = new UpdateGameExpansionsCommand { ExpansionBggIds = [100, 101, 102]};
        var expansions = new List<Expansion>
        {
            new Expansion("Expansion 1", 100, gameId) { Id = 1 },
            new Expansion("Expansion 2", 101, gameId) { Id = 2 }
        };

        _gameServiceMock
            .Setup(x => x.UpdateGameExpansions(gameId, command.ExpansionBggIds))
            .ReturnsAsync(expansions);

        var result = await _controller.UpdateGameExpansions(gameId, command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedExpansions = okResult.Value.Should().BeAssignableTo<List<ExpansionDto>>().Subject;

        returnedExpansions.Select(x => (x.Id, x.Title, x.BggId)).Should().Equal((1, "Expansion 1", 100), (2, "Expansion 2", 101));

        _gameServiceMock.Verify(x => x.UpdateGameExpansions(gameId, command.ExpansionBggIds), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region AddManualExpansion Tests

    [Fact]
    public async Task AddManualExpansion_ShouldReturnCreatedWithTheExpansion()
    {
        var expansion = new Expansion("Promo pack", null, 3) { Id = 8 };
        _gameServiceMock.Setup(x => x.AddManualExpansion(3, "Promo pack")).ReturnsAsync(expansion);

        var result = await _controller.AddManualExpansion(3, new CreateExpansionCommand { Title = "Promo pack" });

        var dto = result.Should().BeOfType<CreatedResult>().Which.Value.Should().BeOfType<ExpansionDto>().Subject;
        dto.Id.Should().Be(8);
        dto.Title.Should().Be("Promo pack");
        dto.BggId.Should().BeNull();
        _gameServiceMock.Verify(x => x.AddManualExpansion(3, "Promo pack"), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region DeleteGameExpansions Tests

    [Fact]
    public async Task DeleteGameExpansions_ShouldReturnNoContent()
    {
        var gameId = 1;
        var expansionId = 100;

        _gameServiceMock
            .Setup(x => x.DeleteExpansion(gameId, expansionId))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteGameExpansions(gameId, expansionId);

        result.Should().BeOfType<NoContentResult>();

        _gameServiceMock.Verify(x => x.DeleteExpansion(gameId, expansionId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetGameStatistics Tests

    [Fact]
    public async Task GetGameStatistics_ShouldReturnOkWithAllStatisticsData()
    {
        var gameId = 1;
        var stats = new GameStatistics
        {
            PlayCount = 50,
            TotalPlayedTime = 3000,
            AverageScore = 42.5
        };
        var topPlayers = new List<TopPlayerDto>();
        var playByDayChart = new List<PlayByDay>();
        var playerCountChart = new List<PlayerCount>();
        var playerScoringChart = new List<PlayerScoringPoint>();
        var scoringRankChart = new List<ScoreRank>();

        _gameServiceMock
            .Setup(x => x.ExistsAsync(gameId))
            .ReturnsAsync(true);
        _gameStatisticsDomainServiceMock
            .Setup(x => x.CalculateStatisticsAsync(gameId, _token))
            .ReturnsAsync(stats);

        _gameChartServiceMock
            .Setup(x => x.GetTopPlayers(gameId, _token))
            .ReturnsAsync(topPlayers);

        _gameChartServiceMock
            .Setup(x => x.GetPlayByDayChart(gameId, _token))
            .ReturnsAsync(playByDayChart);

        _gameChartServiceMock
            .Setup(x => x.GetPlayerCountChart(gameId, _token))
            .ReturnsAsync(playerCountChart);

        _gameChartServiceMock
            .Setup(x => x.GetPlayerScoringChart(gameId, _token))
            .ReturnsAsync(playerScoringChart);

        _gameChartServiceMock
            .Setup(x => x.GetScoringRankedChart(gameId, 42.5, _token))
            .ReturnsAsync(scoringRankChart);

        var result = await _controller.GetGameStatistics(gameId, _token);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GameStatisticsResponse>().Subject;
        response.GameStats.Should().Be(stats);
        response.TopPlayers.Should().BeSameAs(topPlayers);
        response.PlayByDayChart.Should().BeSameAs(playByDayChart);
        response.PlayerCountChart.Should().BeSameAs(playerCountChart);
        response.PlayerScoringChart.Should().BeSameAs(playerScoringChart);
        response.ScoreRankChart.Should().BeSameAs(scoringRankChart);

        _gameServiceMock.Verify(x => x.ExistsAsync(gameId), Times.Once);
        _gameStatisticsDomainServiceMock.Verify(x => x.CalculateStatisticsAsync(gameId, _token), Times.Once);
        _gameChartServiceMock.Verify(x => x.GetTopPlayers(gameId, _token), Times.Once);
        _gameChartServiceMock.Verify(x => x.GetPlayByDayChart(gameId, _token), Times.Once);
        _gameChartServiceMock.Verify(x => x.GetPlayerCountChart(gameId, _token), Times.Once);
        _gameChartServiceMock.Verify(x => x.GetPlayerScoringChart(gameId, _token), Times.Once);
        _gameChartServiceMock.Verify(x => x.GetScoringRankedChart(gameId, 42.5, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameStatistics_ShouldReturnNotFound_WhenGameDoesNotExist()
    {
        _gameServiceMock
            .Setup(x => x.ExistsAsync(42))
            .ReturnsAsync(false);

        var result = await _controller.GetGameStatistics(42, _token);

        result.Should().BeOfType<NotFoundResult>();

        _gameServiceMock.Verify(x => x.ExistsAsync(42), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region ImportBgg Tests

    [Fact]
    public async Task ImportBgg_ShouldReturnOkWithResult_WhenUsernameIsValid()
    {
        var username = "testuser";
        var importResult = new List<BggImportGame>();

        _bggImportServiceMock
            .Setup(x => x.ImportBggCollection(username, _token))
            .ReturnsAsync(importResult);

        var result = await _controller.ImportBgg(username, _token);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(importResult);

        _bggImportServiceMock.Verify(x => x.ImportBggCollection(username, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBgg_ShouldTrimUsername_BeforeImport()
    {
        var importResult = new List<BggImportGame>();

        _bggImportServiceMock
            .Setup(x => x.ImportBggCollection("testuser", _token))
            .ReturnsAsync(importResult);

        var result = await _controller.ImportBgg(" testuser ", _token);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(importResult);

        _bggImportServiceMock.Verify(x => x.ImportBggCollection("testuser", _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ImportBgg_ShouldReturnBadRequest_WhenUsernameIsNullOrWhitespace(string? username)
    {
        var result = await _controller.ImportBgg(username!, _token);

        result.Should().BeOfType<BadRequestResult>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportBggGames_ShouldReturnNoContent_WhenCommandIsValid()
    {
        var command = new ImportBggGamesCommand
        {
            Games =
            [
                new ImportGame {BggId = 1, Title = "Game 1", ImageUrl = "https://example.com/img1.jpg"},
                new ImportGame {BggId = 2, Title = "Game 2", ImageUrl = "https://example.com/img2.jpg"}
            ]
        };

        _bggImportServiceMock
            .Setup(x => x.ImportList(command.Games))
            .Returns(Task.CompletedTask);

        var result = await _controller.ImportBggGames(command);

        result.Should().BeOfType<NoContentResult>();

        _bggImportServiceMock.Verify(x => x.ImportList(command.Games), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region SearchOnBgg Tests

    [Fact]
    public async Task SearchOnBgg_ShouldReturnGame_WhenImportSucceeds()
    {
        var search = new BggSearch { BggId = 123 };
        var game = new Game("BGG Game", true) { Id = 1 };

        _bggImportServiceMock
            .Setup(x => x.ImportGameFromBgg(search))
            .ReturnsAsync(game);

        var result = await _controller.SearchOnBgg(search);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gameDto = okResult.Value.Should().BeAssignableTo<GameDto>().Subject;

        gameDto.Title.Should().Be("BGG Game");

        _bggImportServiceMock.Verify(x => x.ImportGameFromBgg(search), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchOnBgg_ShouldReturnNotFound_WhenGameNotFoundOnBgg()
    {
        var search = new BggSearch { BggId = 999 };

        _bggImportServiceMock
            .Setup(x => x.ImportGameFromBgg(search))
            .ReturnsAsync((Game?)null);

        var result = await _controller.SearchOnBgg(search);

        result.Should().BeOfType<NotFoundResult>();

        _bggImportServiceMock.Verify(x => x.ImportGameFromBgg(search), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Price Tests

    [Fact]
    public async Task GetGamePrice_ShouldReturnNotFound_WhenTheGameDoesNotExist()
    {
        _gameServiceMock
            .Setup(x => x.GetGamePriceAsync(9, false, _token))
            .ReturnsAsync((GamePriceDto?)null);

        var result = await _controller.GetGamePrice(9, false, _token);

        result.Should().BeOfType<NotFoundResult>();
        _gameServiceMock.Verify(x => x.GetGamePriceAsync(9, false, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGamePrice_ShouldForwardRefresh_AndReturnThePrice()
    {
        var price = new GamePriceDto { GameId = 3, Available = true, Status = ChangeDetectionStatus.Ok, Price = 19.99m };
        _gameServiceMock
            .Setup(x => x.GetGamePriceAsync(3, true, _token))
            .ReturnsAsync(price);

        var result = await _controller.GetGamePrice(3, true, _token);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(price);
        _gameServiceMock.Verify(x => x.GetGamePriceAsync(3, true, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTrackedPrices_ShouldForwardRefresh_AndReturnEveryPrice()
    {
        var prices = new List<GamePriceDto>
        {
            new() { GameId = 1, Available = true, Status = ChangeDetectionStatus.Ok },
            new() { GameId = 2, Available = false, Status = ChangeDetectionStatus.Unreachable }
        };
        _gameServiceMock
            .Setup(x => x.GetTrackedPricesAsync(true, _token))
            .ReturnsAsync(prices);

        var result = await _controller.GetTrackedPrices(true, _token);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(prices);
        _gameServiceMock.Verify(x => x.GetTrackedPricesAsync(true, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatch_ShouldReturnTheGameWithItsNewWatch()
    {
        const string watchId = "e0808154-28da-4b85-9a71-24a409e694f1";
        var game = new Game("Brass") { Id = 4 };
        game.UpdateChangeDetectionWatchId(watchId);
        _gameServiceMock
            .Setup(x => x.CreateWatchForGame(4, "https://shop.example.com/brass", _token))
            .ReturnsAsync(game);

        var result = await _controller.CreateWatch(4, new CreateWatchCommand { Url = "https://shop.example.com/brass" }, _token);

        var dto = result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<GameDto>().Subject;
        dto.Id.Should().Be(4);
        dto.ChangeDetectionWatchId.Should().Be(watchId);
        _gameServiceMock.Verify(x => x.CreateWatchForGame(4, "https://shop.example.com/brass", _token), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Shame Tests

    [Fact]
    public async Task GetShameGames_ShouldReturnOkWithShameGames()
    {
        var plannedFor = new DateTime(2026, 10, 23, 19, 0, 0, DateTimeKind.Utc);
        var shameGames = new List<ShameGame>
        {
            new ShameGame
            {
                Id = 1,
                Title = "Dusty Game",
                Price = 40m,
                PlayCount = 3,
                MinPlayers = 2,
                MaxPlayers = 4,
                MinPlayTime = 60,
                MaxPlayTime = 90,
                PlannedGameNightId = 7,
                PlannedFor = plannedFor
            },
            new ShameGame { Id = 2, Title = "Forgotten Game" }
        };

        _shameServiceMock
            .Setup(x => x.GetShameGames(null))
            .ReturnsAsync(shameGames);

        var result = await _controller.GetShameGames();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGames = okResult.Value.Should().BeAssignableTo<List<ShameDto>>().Subject;

        returnedGames.Should().HaveCount(2);
        returnedGames[0].Id.Should().Be(1);
        returnedGames[0].Title.Should().Be("Dusty Game");
        returnedGames[0].Price.Should().Be(40m);
        returnedGames[0].PlayCount.Should().Be(3);
        returnedGames[0].MinPlayers.Should().Be(2);
        returnedGames[0].MaxPlayers.Should().Be(4);
        returnedGames[0].MinPlayTime.Should().Be(60);
        returnedGames[0].MaxPlayTime.Should().Be(90);
        returnedGames[0].PlannedGameNightId.Should().Be(7);
        returnedGames[0].PlannedFor.Should().Be(plannedFor);
        returnedGames[1].Id.Should().Be(2);
        returnedGames[1].Title.Should().Be("Forgotten Game");
        returnedGames[1].PlannedGameNightId.Should().BeNull();

        _shameServiceMock.Verify(x => x.GetShameGames(null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetShameGames_ShouldPassTheRequestedMonths()
    {
        _shameServiceMock
            .Setup(x => x.GetShameGames(12))
            .ReturnsAsync([]);

        var result = await _controller.GetShameGames(12);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<List<ShameDto>>().Which.Should().BeEmpty();

        _shameServiceMock.Verify(x => x.GetShameGames(12), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetShameStatistics_ShouldReturnOkWithStatistics()
    {
        var nightStart = new DateTime(2026, 10, 23, 19, 0, 0, DateTimeKind.Utc);
        var statistics = new ShameStatistics
        {
            Count = 3,
            TotalValue = 120m,
            AverageValue = 40m,
            PricedGameCount = 3,
            Months = 12,
            OwnedCount = 41,
            CollectionValue = 1240m,
            NeverPlayedCount = 2,
            NeverPlayedValue = 80m,
            PlannedCount = 1,
            NextGameNight = new UpcomingGameNight { Id = 5, Title = "Friday", StartDate = nightStart, SuggestedGameIds = [1] }
        };

        _shameServiceMock
            .Setup(x => x.GetShameStatistics(12))
            .ReturnsAsync(statistics);

        var result = await _controller.GetShameStatistics(12);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statisticsDto = okResult.Value.Should().BeOfType<ShameStatisticsDto>().Subject;

        statisticsDto.Count.Should().Be(3);
        statisticsDto.TotalValue.Should().Be(120m);
        statisticsDto.AverageValue.Should().Be(40m);
        statisticsDto.PricedGameCount.Should().Be(3);
        statisticsDto.Months.Should().Be(12);
        statisticsDto.OwnedCount.Should().Be(41);
        statisticsDto.CollectionValue.Should().Be(1240m);
        statisticsDto.NeverPlayedCount.Should().Be(2);
        statisticsDto.NeverPlayedValue.Should().Be(80m);
        statisticsDto.PlannedCount.Should().Be(1);
        statisticsDto.NextGameNight.Should().BeEquivalentTo(new ShameGameNightDto { Id = 5, Title = "Friday", StartDate = nightStart });

        _shameServiceMock.Verify(x => x.GetShameStatistics(12), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetShameStatistics_ShouldReturnNoNextGameNight_WhenNoneIsPlanned()
    {
        _shameServiceMock
            .Setup(x => x.GetShameStatistics(null))
            .ReturnsAsync(new ShameStatistics { Count = 0, Months = 6 });

        var result = await _controller.GetShameStatistics();

        var statisticsDto = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ShameStatisticsDto>().Subject;
        statisticsDto.NextGameNight.Should().BeNull();
        statisticsDto.Months.Should().Be(6);

        _shameServiceMock.Verify(x => x.GetShameStatistics(null), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Authorization Tests

    [Theory]
    [InlineData(nameof(GameController.CreateGame))]
    [InlineData(nameof(GameController.UpdateGame))]
    [InlineData(nameof(GameController.DeleteGameById))]
    [InlineData(nameof(GameController.SearchOnBgg))]
    [InlineData(nameof(GameController.ImportBgg))]
    [InlineData(nameof(GameController.ImportBggGames))]
    [InlineData(nameof(GameController.CreateWatch))]
    [InlineData(nameof(GameController.UpdateGameExpansions))]
    [InlineData(nameof(GameController.AddManualExpansion))]
    [InlineData(nameof(GameController.DeleteGameExpansions))]
    public void WriteEndpoints_ShouldRequireTheUserOrAdminRole(string action)
    {
        var attribute = typeof(GameController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();

        attribute!.Roles.Should().Be(Constants.AuthRoles.UserOrAdmin);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Controller_ShouldRequireASignedInUser()
    {
        var attribute = typeof(GameController).GetCustomAttribute<AuthorizeAttribute>();

        attribute!.Roles.Should().BeNull();
        VerifyNoOtherCalls();
    }

    #endregion
}
