using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class GameControllerTests
{
    private readonly Mock<IGameService> _gameServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GameController _controller;

    public GameControllerTests()
    {
        _gameServiceMock = new Mock<IGameService>();
        _mapperMock = new Mock<IMapper>();

        _controller = new GameController(_gameServiceMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetGames_ShouldReturnOkResultWithMappedGames_WhenServiceReturnsGames()
    {
        var games = new List<Game>
        {
            new() {Title = "Monopoly", Id = 1},
            new() {Title = "Scrabble", Id = 2}
        };

        var mappedGames = new List<GameViewModel>
        {
            new() {Title = "Monopoly", Id = 1},
            new() {Title = "Scrabble", Id = 2}
        };

        _gameServiceMock.Setup(x => x.GetGames()).ReturnsAsync(games);
        _mapperMock.Setup(x => x.Map<IList<GameViewModel>>(games)).Returns(mappedGames);

        var result = await _controller.GetGames();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(mappedGames);

        _gameServiceMock.Verify(x => x.GetGames(), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<GameViewModel>>(games), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGames_ShouldReturnOkResultWithEmptyList_WhenServiceReturnsEmptyList()
    {
        var games = new List<Game>();
        var mappedGames = new List<GameViewModel>();

        _gameServiceMock.Setup(x => x.GetGames()).ReturnsAsync(games);
        _mapperMock.Setup(x => x.Map<IList<GameViewModel>>(games)).Returns(mappedGames);

        var result = await _controller.GetGames();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(mappedGames);

        _gameServiceMock.Verify(x => x.GetGames(), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<GameViewModel>>(games), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateGame_ShouldReturnOkResultWithMappedGame_WhenValidGameViewModelProvided()
    {
        var createGameViewModel = new CreateGameViewModel {Title = "New Game"};
        var game = new Game {Title = "New Game", Id = 1};
        var createdGame = new Game {Title = "New Game", Id = 1};
        var gameViewModel = new GameViewModel {Title = "New Game", Id = 1};

        _mapperMock.Setup(x => x.Map<Game>(createGameViewModel)).Returns(game);
        _gameServiceMock.Setup(x => x.CreateGame(game)).ReturnsAsync(createdGame);
        _mapperMock.Setup(x => x.Map<GameViewModel>(createdGame)).Returns(gameViewModel);

        // Act
        var result = await _controller.CreateGame(createGameViewModel);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(gameViewModel);

        _mapperMock.Verify(x => x.Map<Game>(createGameViewModel), Times.Once);
        _gameServiceMock.Verify(x => x.CreateGame(game), Times.Once);
        _mapperMock.Verify(x => x.Map<GameViewModel>(createdGame), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateGame_ShouldReturnBadRequest_WhenGameViewModelIsNull()
    {
        var result = await _controller.CreateGame(null);

        result.Should().BeOfType<BadRequestResult>();

        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldReturnOkResultWithMappedGame_WhenValidGameViewModelProvided()
    {
        var gameViewModel = new GameViewModel {Title = "Updated Game", Id = 1};
        var game = new Game {Title = "Updated Game", Id = 1};
        var updatedGame = new Game {Title = "Updated Game", Id = 1};
        var resultViewModel = new GameViewModel {Title = "Updated Game", Id = 1};

        _mapperMock.Setup(x => x.Map<Game>(gameViewModel)).Returns(game);
        _gameServiceMock.Setup(x => x.UpdateGame(game)).ReturnsAsync(updatedGame);
        _mapperMock.Setup(x => x.Map<GameViewModel>(updatedGame)).Returns(resultViewModel);

        var result = await _controller.UpdateGame(gameViewModel);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(resultViewModel);

        _mapperMock.Verify(x => x.Map<Game>(gameViewModel), Times.Once);
        _gameServiceMock.Verify(x => x.UpdateGame(game), Times.Once);
        _mapperMock.Verify(x => x.Map<GameViewModel>(updatedGame), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldReturnBadRequest_WhenGameViewModelIsNull()
    {
        var result = await _controller.UpdateGame(null);

        result.Should().BeOfType<BadRequestResult>();

        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldReturnInternalServerError_WhenServiceThrowsException()
    {
        var gameViewModel = new GameViewModel {Title = "Game", Id = 1};
        var game = new Game {Title = "Game", Id = 1};

        _mapperMock.Setup(x => x.Map<Game>(gameViewModel)).Returns(game);
        _gameServiceMock.Setup(x => x.UpdateGame(game)).ThrowsAsync(new InvalidOperationException());

        var result = await _controller.UpdateGame(gameViewModel);

        result.Should().BeOfType<StatusCodeResult>();
        var statusResult = result as StatusCodeResult;
        statusResult!.StatusCode.Should().Be(500);

        _mapperMock.Verify(x => x.Map<Game>(gameViewModel), Times.Once);
        _gameServiceMock.Verify(x => x.UpdateGame(game), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameById_ShouldReturnOkResultWithMappedGame_WhenGameExists()
    {
        const int gameId = 1;
        var game = new Game {Title = "Test Game", Id = gameId};
        var gameViewModel = new GameViewModel {Title = "Test Game", Id = gameId};

        _gameServiceMock.Setup(x => x.GetGameById(gameId)).ReturnsAsync(game);
        _mapperMock.Setup(x => x.Map<GameViewModel>(game)).Returns(gameViewModel);

        var result = await _controller.GetGameById(gameId);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(gameViewModel);

        _gameServiceMock.Verify(x => x.GetGameById(gameId), Times.Once);
        _mapperMock.Verify(x => x.Map<GameViewModel>(game), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameById_ShouldReturnNotFound_WhenGameDoesNotExist()
    {
        const int gameId = 1;

        _gameServiceMock.Setup(x => x.GetGameById(gameId)).ReturnsAsync((Game?) null);

        var result = await _controller.GetGameById(gameId);

        result.Should().BeOfType<NotFoundResult>();

        _gameServiceMock.Verify(x => x.GetGameById(gameId), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameSessionsById_ShouldReturnOkResultWithMappedSessions_WhenSessionsExist()
    {
        const int gameId = 1;
        var sessions = new List<Session> {new() {Id = 1}, new() {Id = 2}};
        var sessionViewModels = new List<SessionViewModel>
        {
            new() {Id = "1"},
            new() {Id = "2"}
        };

        _gameServiceMock.Setup(x => x.GetSessionsForGame(gameId)).ReturnsAsync(sessions);
        _mapperMock.Setup(x => x.Map<IList<SessionViewModel>>(sessions)).Returns(sessionViewModels);

        var result = await _controller.GetGameSessionsById(gameId);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(sessionViewModels);

        _gameServiceMock.Verify(x => x.GetSessionsForGame(gameId), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<SessionViewModel>>(sessions), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteGameById_ShouldReturnOkResultWithSuccessResult_WhenGameDeleted()
    {
        const int gameId = 1;

        _gameServiceMock.Setup(x => x.Delete(gameId)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteGameById(gameId);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<DeletionResultViewModel>();
        var deletionResult = okResult.Value as DeletionResultViewModel;
        deletionResult!.Type.Should().Be((int) ResultState.Success);

        _gameServiceMock.Verify(x => x.Delete(gameId), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameStats_ShouldReturnOkResultWithMappedStats_WhenStatsExist()
    {
        const int gameId = 1;
        var stats = new GameStatistics {PlayCount = 10, TotalPlayedTime = new TimeSpan(0, 2,0,0)};
        var statsViewModel = new GameStatisticsViewModel {PlayCount = 10, TotalPlayedTime = 120};

        _gameServiceMock.Setup(x => x.GetStats(gameId)).ReturnsAsync(stats);
        _mapperMock.Setup(x => x.Map<GameStatisticsViewModel>(stats)).Returns(statsViewModel);

        var result = await _controller.GetGameStats(gameId);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(statsViewModel);

        _gameServiceMock.Verify(x => x.GetStats(gameId), Times.Once);
        _mapperMock.Verify(x => x.Map<GameStatisticsViewModel>(stats), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTopPlayers_ShouldReturnOkResultWithMappedPlayers_WhenPlayersExist()
    {
        const int gameId = 1;
        var topPlayers = new List<TopPlayer> {new() {PlayerId = 1, PlayCount = 5}};
        var topPlayerViewModels = new List<TopPlayerViewModel>
        {
            new() {PlayerId = 1, PlayCount = 5}
        };

        _gameServiceMock.Setup(x => x.GetTopPlayers(gameId)).ReturnsAsync(topPlayers);
        _mapperMock.Setup(x => x.Map<IList<TopPlayerViewModel>>(topPlayers)).Returns(topPlayerViewModels);

        var result = await _controller.GetTopPlayers(gameId);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(topPlayerViewModels);

        _gameServiceMock.Verify(x => x.GetTopPlayers(gameId), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<TopPlayerViewModel>>(topPlayers), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchOnBgg_ShouldReturnExistingGame_WhenGameAlreadyExists()
    {
        var search = new BggSearch {BggId = 123};
        var existingGame = new Game {Title = "Existing Game", BggId = 123};
        var existingGameViewModel = new GameViewModel {Title = "Existing Game", BggId = 123};

        _gameServiceMock.Setup(x => x.GetGameByBggId(search.BggId)).ReturnsAsync(existingGame);
        _mapperMock.Setup(x => x.Map<GameViewModel>(existingGame)).Returns(existingGameViewModel);

        var result = await _controller.SearchOnBgg(search);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(existingGameViewModel);

        _gameServiceMock.Verify(x => x.GetGameByBggId(search.BggId), Times.Once);
        _mapperMock.Verify(x => x.Map<GameViewModel>(existingGame), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchOnBgg_ShouldReturnBadRequest_WhenSearchAndCreateReturnsNull()
    {
        var search = new BggSearch {BggId = 123};

        _gameServiceMock.Setup(x => x.GetGameByBggId(search.BggId)).ReturnsAsync((Game?) null);
        _gameServiceMock.Setup(x => x.SearchAndCreateGame(search.BggId)).ReturnsAsync((BggGame?) null);

        var result = await _controller.SearchOnBgg(search);

        result.Should().BeOfType<BadRequestResult>();

        _gameServiceMock.Verify(x => x.GetGameByBggId(search.BggId), Times.Once);
        _gameServiceMock.Verify(x => x.SearchAndCreateGame(search.BggId), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchOnBgg_ShouldReturnOkResultWithNewGame_WhenGameCreatedSuccessfully()
    {
        var search = new BggSearch {BggId = 123};
        var bggGame = new BggGame {Names = ["New BGG Game"]};
        var dbGame = new Game {Title = "New BGG Game", Id = 1};
        var gameViewModel = new GameViewModel {Title = "New BGG Game", Id = 1};

        _gameServiceMock.Setup(x => x.GetGameByBggId(search.BggId)).ReturnsAsync((Game?) null);
        _gameServiceMock.Setup(x => x.SearchAndCreateGame(search.BggId)).ReturnsAsync(bggGame);
        _gameServiceMock.Setup(x => x.ProcessBggGameData(bggGame, search)).ReturnsAsync(dbGame);
        _mapperMock.Setup(x => x.Map<GameViewModel>(dbGame)).Returns(gameViewModel);

        var result = await _controller.SearchOnBgg(search);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(gameViewModel);

        _gameServiceMock.Verify(x => x.GetGameByBggId(search.BggId), Times.Once);
        _gameServiceMock.Verify(x => x.SearchAndCreateGame(search.BggId), Times.Once);
        _gameServiceMock.Verify(x => x.ProcessBggGameData(bggGame, search), Times.Once);
        _mapperMock.Verify(x => x.Map<GameViewModel>(dbGame), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PlayByDayChart_ShouldReturnOkResultWithMappedData()
    {
        const int gameId = 1;
        var data = new List<PlayByDay> {new() {DayOfWeek = DayOfWeek.Monday, PlayCount = 5}};
        var dataViewModel = new List<PlayByDayChartViewModel>
        {
            new() {DayOfWeek = 1, PlayCount = 5}
        };

        _gameServiceMock.Setup(x => x.GetPlayByDayChart(gameId)).ReturnsAsync(data);
        _mapperMock.Setup(x => x.Map<IList<PlayByDayChartViewModel>>(data)).Returns(dataViewModel);

        var result = await _controller.PlayByDayChart(gameId);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(dataViewModel);

        _gameServiceMock.Verify(x => x.GetPlayByDayChart(gameId), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<PlayByDayChartViewModel>>(data), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PlayerCounts_ShouldReturnOkResultWithMappedData()
    {
        const int gameId = 1;
        var data = new List<PlayerCount> {new() {Players = 4, PlayCount = 10}};
        var dataViewModel = new List<PlayerCountChartViewModel>
        {
            new() {Players = 4, PlayCount = 10}
        };

        _gameServiceMock.Setup(x => x.GetPlayerCountChart(gameId)).ReturnsAsync(data);
        _mapperMock.Setup(x => x.Map<IList<PlayerCountChartViewModel>>(data)).Returns(dataViewModel);

        var result = await _controller.PlayerCounts(gameId);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(dataViewModel);

        _gameServiceMock.Verify(x => x.GetPlayerCountChart(gameId), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<PlayerCountChartViewModel>>(data), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PlayerScoring_ShouldReturnBadRequest_WhenGameHasNoScoring()
    {
        const int gameId = 1;
        var game = new Game {HasScoring = false};

        _gameServiceMock.Setup(x => x.GetGameById(gameId)).ReturnsAsync(game);

        var result = await _controller.PlayerScoring(gameId);

        result.Should().BeOfType<BadRequestResult>();

        _gameServiceMock.Verify(x => x.GetGameById(gameId), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PlayerScoring_ShouldReturnOkResultWithMappedData_WhenGameHasScoring()
    {
        const int gameId = 1;
        var series = new XValue() {Value = 20, Id = 1};
        var dateTime = DateTime.Now;
        var game = new Game {HasScoring = true};
        var data = new Dictionary<DateTime, XValue[]>()
        {
            {dateTime, [new XValue(){ Value = 20, Id = 1}]}
        };
        var dataViewModel = new List<PlayerScoringChartViewModel>
        {
            new() {DateTime = dateTime, Series = [series]}
        };

        _gameServiceMock.Setup(x => x.GetGameById(gameId)).ReturnsAsync(game);
        _gameServiceMock.Setup(x => x.GetPlayerScoringChart(gameId)).ReturnsAsync(data);
        _mapperMock.Setup(x => x.Map<IList<PlayerScoringChartViewModel>>(data)).Returns(dataViewModel);

        var result = await _controller.PlayerScoring(gameId);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(dataViewModel);

        _gameServiceMock.Verify(x => x.GetGameById(gameId), Times.Once);
        _gameServiceMock.Verify(x => x.GetPlayerScoringChart(gameId), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<PlayerScoringChartViewModel>>(data), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ScoringRanked_ShouldReturnOkResultWithMappedData()
    {
        const int gameId = 1;
        var data = new List<ScoreRank>
        {
            new() {Key = "Player1", Score = 100, PlayerId = 1}
        };
        var dataViewModel = new List<ScoreRankChartViewModel>
        {
            new() {Key = "Player1", Score = 100, PlayerId = 1}
        };

        _gameServiceMock.Setup(x => x.GetScoringRankedChart(gameId)).ReturnsAsync(data);
        _mapperMock.Setup(x => x.Map<IList<ScoreRankChartViewModel>>(data)).Returns(dataViewModel);

        var result = await _controller.ScoringRanked(gameId);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(dataViewModel);

        _gameServiceMock.Verify(x => x.GetScoringRankedChart(gameId), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<ScoreRankChartViewModel>>(data), Times.Once);
        _gameServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }
}