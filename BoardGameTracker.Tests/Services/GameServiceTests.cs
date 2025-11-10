using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Bgg;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Moq;
using Refit;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class GameServiceTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly Mock<IBggApi> _bggApiMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _bggApiMock = new Mock<IBggApi>();
        _mapperMock = new Mock<IMapper>();
        _imageServiceMock = new Mock<IImageService>();

        _gameService = new GameService(
            _gameRepositoryMock.Object,
            _mapperMock.Object,
            _imageServiceMock.Object,
            _bggApiMock.Object,
            _playerRepositoryMock.Object);
    }

    [Fact]
    public async Task ProcessBggGameData_ShouldProcessAllDataAndReturnGame_WhenValidInputProvided()
    {
        var rawGame = new BggGame
        {
            BggId = 123,
            Image = "https://example.com/image.jpg",
            Names =
            [
                "Test Game"
            ],
            YearPublished = 2020,
            Thumbnail = string.Empty,
            Description = string.Empty
        };
        var search = new BggSearch
        {
            State = GameState.Owned,
            Price = 29.99,
            AdditionDate = DateTime.Now,
            HasScoring = true
        };
        var categories = new List<GameCategory> {new() {Name = "Strategy"}};
        var mechanics = new List<GameMechanic> {new() {Name = "Worker Placement"}};
        var people = new List<Person> {new() {Name = "John Designer"}};
        var mappedGame = new Game {Title = "Test Game", BggId = 123};
        var createdGame = new Game {Id = 1, Title = "Test Game", BggId = 123};
        var downloadedImage = "/images/123.jpg";

        _mapperMock.Setup(x => x.Map<IList<GameCategory>>(rawGame.Categories)).Returns(categories);
        _mapperMock.Setup(x => x.Map<IList<GameMechanic>>(rawGame.Mechanics)).Returns(mechanics);
        _mapperMock.Setup(x => x.Map<IList<Person>>(rawGame.People)).Returns(people);
        _mapperMock.Setup(x => x.Map<Game>(rawGame)).Returns(mappedGame);
        _imageServiceMock.Setup(x => x.DownloadImage(rawGame.Image, rawGame.BggId.ToString()))
            .ReturnsAsync(downloadedImage);
        _gameRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Game>())).ReturnsAsync(createdGame);

        var result = await _gameService.ProcessBggGameData(rawGame, search);

        result.Should().Be(createdGame);
        _gameRepositoryMock.Verify(x => x.AddGameCategoriesIfNotExists(categories), Times.Once);
        _gameRepositoryMock.Verify(x => x.AddGameMechanicsIfNotExists(mechanics), Times.Once);
        _gameRepositoryMock.Verify(x => x.AddPeopleIfNotExists(people), Times.Once);
        _gameRepositoryMock.Verify(x => x.CreateAsync(It.Is<Game>(g =>
            g.Image == downloadedImage &&
            g.State == search.State &&
            g.BuyingPrice == search.Price &&
            g.AdditionDate == search.AdditionDate &&
            g.HasScoring == search.HasScoring)), Times.Once);

        _mapperMock.Verify(x => x.Map<IList<GameCategory>>(rawGame.Categories), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<GameMechanic>>(rawGame.Mechanics), Times.Once);
        _mapperMock.Verify(x => x.Map<IList<Person>>(rawGame.People), Times.Once);
        _mapperMock.Verify(x => x.Map<Game>(rawGame), Times.Once);

        _imageServiceMock.Verify(x => x.DownloadImage(rawGame.Image, rawGame.BggId.ToString()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameByBggId_ShouldReturnGame_WhenGameExists()
    {
        const int bggId = 123;
        var expectedGame = new Game {BggId = bggId, Title = "Test Game"};

        _gameRepositoryMock.Setup(x => x.GetGameByBggId(bggId)).ReturnsAsync(expectedGame);

        var result = await _gameService.GetGameByBggId(bggId);

        result.Should().Be(expectedGame);
        _gameRepositoryMock.Verify(x => x.GetGameByBggId(bggId), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGames_ShouldReturnGamesList_WhenRepositoryReturnsData()
    {
        var expectedGames = new List<Game>
        {
            new() {Id = 1, Title = "Game 1"},
            new() {Id = 2, Title = "Game 2"}
        };

        _gameRepositoryMock.Setup(x => x.GetGamesOverviewList()).ReturnsAsync(expectedGames);

        var result = await _gameService.GetGames();

        result.Should().Contain(expectedGames);
        _gameRepositoryMock.Verify(x => x.GetGamesOverviewList(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameById_ShouldReturnGame_WhenGameExists()
    {
        const int gameId = 1;
        var expectedGame = new Game {Id = gameId, Title = "Test Game"};

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId)).ReturnsAsync(expectedGame);

        var result = await _gameService.GetGameById(gameId);

        result.Should().Be(expectedGame);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldDeleteGameAndImage_WhenGameExists()
    {
        const int gameId = 1;
        var game = new Game {Id = gameId, Title = "Test Game", Image = "/images/test.jpg"};

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId)).ReturnsAsync(game);

        await _gameService.Delete(gameId);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(game.Image), Times.Once);
        _gameRepositoryMock.Verify(x => x.DeleteAsync(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldDoNothing_WhenGameDoesNotExist()
    {
        const int gameId = 1;

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId)).ReturnsAsync((Game?) null);

        await _gameService.Delete(gameId);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayFlags_ShouldReturnAllFlags_WhenAllValuesAreDifferent()
    {
        const int gameId = 1;
        const int shortestPlay = 10;
        const int longestPlay = 30;
        const int highestScore = 100;
        const int lowestScore = 20;

        _gameRepositoryMock.Setup(x => x.GetShortestPlay(gameId)).ReturnsAsync(shortestPlay);
        _gameRepositoryMock.Setup(x => x.GetLongestPlay(gameId)).ReturnsAsync(longestPlay);
        _gameRepositoryMock.Setup(x => x.GetHighScorePlay(gameId)).ReturnsAsync(highestScore);
        _gameRepositoryMock.Setup(x => x.GetLowestScorePlay(gameId)).ReturnsAsync(lowestScore);

        var result = await _gameService.GetPlayFlags(gameId);

        result.Should().HaveCount(4);
        result[SessionFlag.ShortestGame].Should().Be(shortestPlay);
        result[SessionFlag.LongestGame].Should().Be(longestPlay);
        result[SessionFlag.HighestScore].Should().Be(highestScore);
        result[SessionFlag.LowestScore].Should().Be(lowestScore);
        VerifyPlayFlagsRepositoryCalls(gameId);
    }

    [Fact]
    public async Task GetPlayFlags_ShouldExcludeLongestGame_WhenShortestAndLongestAreEqual()
    {
        const int gameId = 1;
        const int samePlayTime = 25;
        const int highestScore = 100;
        const int lowestScore = 20;

        _gameRepositoryMock.Setup(x => x.GetShortestPlay(gameId)).ReturnsAsync(samePlayTime);
        _gameRepositoryMock.Setup(x => x.GetLongestPlay(gameId)).ReturnsAsync(samePlayTime);
        _gameRepositoryMock.Setup(x => x.GetHighScorePlay(gameId)).ReturnsAsync(highestScore);
        _gameRepositoryMock.Setup(x => x.GetLowestScorePlay(gameId)).ReturnsAsync(lowestScore);

        var result = await _gameService.GetPlayFlags(gameId);

        result.Should().HaveCount(3);
        result[SessionFlag.ShortestGame].Should().Be(samePlayTime);
        result.Should().NotContainKey(SessionFlag.LongestGame);
        result[SessionFlag.HighestScore].Should().Be(highestScore);
        result[SessionFlag.LowestScore].Should().Be(lowestScore);
        VerifyPlayFlagsRepositoryCalls(gameId);
    }

    [Fact]
    public async Task GetPlayFlags_ShouldExcludeLowestScore_WhenHighestAndLowestAreEqual()
    {
        const int gameId = 1;
        const int shortestPlay = 10;
        const int longestPlay = 30;
        const int sameScore = 50;

        _gameRepositoryMock.Setup(x => x.GetShortestPlay(gameId)).ReturnsAsync(shortestPlay);
        _gameRepositoryMock.Setup(x => x.GetLongestPlay(gameId)).ReturnsAsync(longestPlay);
        _gameRepositoryMock.Setup(x => x.GetHighScorePlay(gameId)).ReturnsAsync(sameScore);
        _gameRepositoryMock.Setup(x => x.GetLowestScorePlay(gameId)).ReturnsAsync(sameScore);

        var result = await _gameService.GetPlayFlags(gameId);

        result.Should().HaveCount(3);
        result[SessionFlag.ShortestGame].Should().Be(shortestPlay);
        result[SessionFlag.LongestGame].Should().Be(longestPlay);
        result[SessionFlag.HighestScore].Should().Be(sameScore);
        result.Should().NotContainKey(SessionFlag.LowestScore);
        VerifyPlayFlagsRepositoryCalls(gameId);
    }

    [Fact]
    public async Task GetTotalPlayCount_ShouldReturnPlayCount_WhenRepositoryReturnsCount()
    {
        const int gameId = 1;
        const int expectedCount = 25;

        _gameRepositoryMock.Setup(x => x.GetTotalPlayCount(gameId)).ReturnsAsync(expectedCount);

        var result = await _gameService.GetTotalPlayCount(gameId);

        result.Should().Be(expectedCount);
        _gameRepositoryMock.Verify(x => x.GetTotalPlayCount(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayByDayChart_ShouldReturnAllDaysOfWeek_WhenRepositoryReturnsPartialData()
    {
        const int gameId = 1;
        var repositoryData = new List<IGrouping<DayOfWeek, Session>>
        {
            CreateGrouping(DayOfWeek.Monday, 3),
            CreateGrouping(DayOfWeek.Wednesday, 5)
        };

        _gameRepositoryMock.Setup(x => x.GetPlayByDayChart(gameId)).ReturnsAsync(repositoryData);

        var result = await _gameService.GetPlayByDayChart(gameId);

        var resultList = result.ToList();
        resultList.Should().HaveCount(7);
        resultList.Should().Contain(x => x.DayOfWeek == DayOfWeek.Monday && x.PlayCount == 3);
        resultList.Should().Contain(x => x.DayOfWeek == DayOfWeek.Wednesday && x.PlayCount == 5);
        resultList.Should().Contain(x => x.DayOfWeek == DayOfWeek.Sunday && x.PlayCount == 0);
        resultList.Should().Contain(x => x.DayOfWeek == DayOfWeek.Tuesday && x.PlayCount == 0);
        _gameRepositoryMock.Verify(x => x.GetPlayByDayChart(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerCountChart_ShouldReturnPlayerCounts_WhenRepositoryReturnsData()
    {
        const int gameId = 1;
        var repositoryData = new List<IGrouping<int, int>>
        {
            new TestGrouping<int, int>(2, [1, 2, 3, 4, 5]),
            new TestGrouping<int, int>(4, [1, 2, 3])
        };

        _gameRepositoryMock.Setup(x => x.GetPlayerCountChart(gameId)).ReturnsAsync(repositoryData);

        var result = await _gameService.GetPlayerCountChart(gameId);

        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList.Should().Contain(x => x.Players == 2 && x.PlayCount == 5);
        resultList.Should().Contain(x => x.Players == 4 && x.PlayCount == 3);
        _gameRepositoryMock.Verify(x => x.GetPlayerCountChart(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStats_ShouldReturnCompleteStatistics_WhenAllDataAvailable()
    {
        const int gameId = 1;
        var mostWinPlayer = new Player {Id = 5, Name = "Winner", Image = "winner.jpg"};
        const int playerWins = 10;

        SetupStatsRepositoryCalls(gameId, mostWinPlayer);
        _playerRepositoryMock.Setup(x => x.GetWinCount(mostWinPlayer.Id, gameId)).ReturnsAsync(playerWins);

        var result = await _gameService.GetStats(gameId);

        result.Should().NotBeNull();
        result.PlayCount.Should().Be(15);
        result.TotalPlayedTime.Should().Be(TimeSpan.FromMinutes(300));
        result.PricePerPlay.Should().Be(2.50);
        result.HighScore.Should().Be(100.0);
        result.AveragePlayTime.Should().Be(20.0);
        result.AverageScore.Should().Be(75.0);
        result.LastPlayed.Should().Be(DateTime.Today);
        result.MostWinsPlayer.Should().NotBeNull();
        result.MostWinsPlayer.Id.Should().Be(mostWinPlayer.Id);
        result.MostWinsPlayer.Name.Should().Be(mostWinPlayer.Name);
        result.MostWinsPlayer.Image.Should().Be(mostWinPlayer.Image);
        result.MostWinsPlayer.TotalWins.Should().Be(playerWins);
        VerifyStatsRepositoryCalls(gameId, mostWinPlayer.Id);
    }

    [Fact]
    public async Task GetStats_ShouldReturnStatisticsWithoutMostWinsPlayer_WhenNoMostWinsPlayerFound()
    {
        const int gameId = 1;

        SetupStatsRepositoryCalls(gameId, null);

        var result = await _gameService.GetStats(gameId);

        result.Should().NotBeNull();
        result.MostWinsPlayer.Should().BeNull();
        _gameRepositoryMock.Verify(x => x.GetMostWins(gameId), Times.Once);
        _playerRepositoryMock.VerifyNoOtherCalls();
        VerifyStatsRepositoryCallsWithoutPlayer(gameId);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCount_WhenRepositoryReturnsCount()
    {
        const int expectedCount = 42;

        _gameRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(expectedCount);

        var result = await _gameService.CountAsync();

        result.Should().Be(expectedCount);
        _gameRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchAndCreateGame_ShouldReturnMappedGame_WhenApiReturnsValidData()
    {
        const int bggId = 123;
        var bggApiGames = new BggApiGames
        {
            Games = [new BggRawGame
                {
                    Id = bggId,
                    Names =
                    [
                        new Name
                        {
                            Value = "Test Game",
                            Type = "BaseGame"
                        }
                    ],
                    Thumbnail = string.Empty,
                    Image =  string.Empty,
                    Description =  string.Empty,
                    Type =  string.Empty
                }
            ]
        };
        var expectedBggGame = new BggGame
        {
            BggId = bggId,
            Names =
            [
                "Test Game"
            ],
            Thumbnail = string.Empty,
            Image = string.Empty,
            Description = string.Empty
        };
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            bggApiGames,
            new RefitSettings()
        );

        _bggApiMock.Setup(x => x.SearchGame(bggId, 1)).ReturnsAsync(apiResponse);
        _mapperMock.Setup(x => x.Map<BggGame>(apiResponse.Content.Games.First())).Returns(expectedBggGame);

        var result = await _gameService.SearchGame(bggId);

        result.Should().Be(expectedBggGame);
        _bggApiMock.Verify(x => x.SearchGame(bggId, 1), Times.Once);
        _mapperMock.Verify(x => x.Map<BggGame>(apiResponse.Content.Games.First()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchAndCreateGame_ShouldReturnNull_WhenApiReturnsError()
    {
        const int bggId = 123;
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            null,
            new RefitSettings()
        );

        _bggApiMock.Setup(x => x.SearchGame(bggId, 1)).ReturnsAsync(apiResponse);

        var result = await _gameService.SearchGame(bggId);

        result.Should().BeNull();
        _bggApiMock.Verify(x => x.SearchGame(bggId, 1), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchAndCreateGame_ShouldReturnNull_WhenApiReturnsNoGames()
    {
        const int bggId = 123;
        var bggApiGames = new BggApiGames
        {
            Games = []
        };
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            bggApiGames,
            new RefitSettings()
        );

        _bggApiMock.Setup(x => x.SearchGame(bggId, 1)).ReturnsAsync(apiResponse);

        var result = await _gameService.SearchGame(bggId);

        result.Should().BeNull();
        _bggApiMock.Verify(x => x.SearchGame(bggId, 1), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTopPlayers_ShouldReturnTopFivePlayersWithWins_WhenSessionsExist()
    {
        const int gameId = 1;
        var sessions = CreateSessionsWithPlayerSessions();
        var topPlayers = new List<TopPlayer>
        {
            new() {PlayerId = 1, Wins = 5, PlayCount = 10},
            new() {PlayerId = 2, Wins = 3, PlayCount = 8},
            new() {PlayerId = 3, Wins = 2, PlayCount = 5}
        };

        _gameRepositoryMock.Setup(x => x.GetSessions(gameId, 0, null)).ReturnsAsync(sessions);

        var result = await _gameService.GetTopPlayers(gameId);

        result.Should().HaveCount(1);
        result.Should().AllSatisfy(x => x.Wins.Should().BeGreaterThan(0));
        result.Should().BeInDescendingOrder(x => x.Wins);
        _gameRepositoryMock.Verify(x => x.GetSessions(gameId, 0, null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateGame_ShouldReturnCreatedGame_WhenRepositorySucceeds()
    {
        var inputGame = new Game {Title = "New Game"};
        var expectedGame = new Game {Id = 1, Title = "New Game"};

        _gameRepositoryMock.Setup(x => x.CreateAsync(inputGame)).ReturnsAsync(expectedGame);

        var result = await _gameService.CreateGame(inputGame);

        result.Should().Be(expectedGame);
        _gameRepositoryMock.Verify(x => x.CreateAsync(inputGame), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSessionsForGame_ShouldReturnSessions_WhenRepositoryReturnsData()
    {
        const int gameId = 1;
        var expectedSessions = new List<Session>
        {
            new() {Id = 1, GameId = gameId},
            new() {Id = 2, GameId = gameId}
        };

        _gameRepositoryMock.Setup(x => x.GetSessionsByGameId(gameId)).ReturnsAsync(expectedSessions);

        var result = await _gameService.GetSessionsForGame(gameId);

        result.Should().Contain(expectedSessions);
        _gameRepositoryMock.Verify(x => x.GetSessionsByGameId(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGame_ShouldReturnUpdatedGame_WhenRepositorySucceeds()
    {
        var inputGame = new Game {Id = 1, Title = "Updated Game"};
        var expectedGame = new Game {Id = 1, Title = "Updated Game"};

        _gameRepositoryMock.Setup(x => x.UpdateAsync(inputGame)).ReturnsAsync(expectedGame);

        var result = await _gameService.UpdateGame(inputGame);

        result.Should().Be(expectedGame);
        _gameRepositoryMock.Verify(x => x.UpdateAsync(inputGame), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldReturnExpansions_WhenApiReturnsValidData()
    {
        const int gameId = 123;
        var game = new Game
        {
            BggId = 1,
            Id = gameId
        };

        var bggApiGames = new BggApiGames
        {
            Games = [new BggRawGame
                {
                    Id = gameId,
                    Names =
                    [
                        new Name
                        {
                            Value = "Test Game",
                            Type = string.Empty
                        }
                    ],
                    Thumbnail = string.Empty,
                    Image = string.Empty,
                    Description = string.Empty,
                    Type = string.Empty
                }
            ]
        };
        var expectedBggGame = new BggGame
        {
            BggId = gameId,
            Names =
            [
                "Test Game"
            ],
            Expansions =
            [
                new BggLink
                {
                    Id = 1,
                    Value = "Expansion 1"
                },
                new BggLink
                {
                    Id = 2,
                    Value = "Expansion 2"
                }
            ],
            Thumbnail = string.Empty,
            Image = string.Empty,
            Description = string.Empty
        };
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            bggApiGames,
            new RefitSettings()
        );

        _bggApiMock.Setup(x => x.SearchGame(game.BggId.Value, 0)).ReturnsAsync(apiResponse);
        _mapperMock.Setup(x => x.Map<BggGame>(apiResponse.Content.Games.First())).Returns(expectedBggGame);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId)).ReturnsAsync(game);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEquivalentTo(expectedBggGame.Expansions);

        _bggApiMock.Verify(x => x.SearchGame(game.BggId.Value, 0), Times.Once);
        _mapperMock.Verify(x => x.Map<BggGame>(apiResponse.Content.Games.First()), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(game.Id), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldReturnEmptyArray_WhenApiReturnsError()
    {
        const int gameId = 123;
        var game = new Game
        {
            BggId = 1,
            Id = gameId
        };

        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            null,
            new RefitSettings()
        );

        _bggApiMock.Setup(x => x.SearchGame(game.BggId.Value, 0)).ReturnsAsync(apiResponse);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId)).ReturnsAsync(game);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEmpty();
        _bggApiMock.Verify(x => x.SearchGame(game.BggId.Value, 0), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(game.Id), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchExpansionsForGame_ShouldReturnEmptyArray_WhenApiReturnsNoGames()
    {
        const int gameId = 123;
        var game = new Game
        {
            BggId = 1,
            Id = gameId
        };

        var bggApiGames = new BggApiGames {Games = []};
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            bggApiGames,
            new RefitSettings()
        );

        _bggApiMock.Setup(x => x.SearchGame(game.BggId.Value, 0)).ReturnsAsync(apiResponse);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId)).ReturnsAsync(game);

        var result = await _gameService.SearchExpansionsForGame(gameId);

        result.Should().BeEmpty();
        _bggApiMock.Verify(x => x.SearchGame(game.BggId.Value, 0), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(game.Id), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldReturnEmptyList_WhenGameDoesNotExist()
    {
        const int gameId = 1;
        var expansionIds = new[] {100, 200};

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId)).ReturnsAsync((Game?) null);

        var result = await _gameService.UpdateGameExpansions(gameId, expansionIds);

        result.Should().BeEmpty();
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldRemoveExpansionsNotInList_WhenExpansionsExist()
    {
        const int gameId = 1;
        var expansionIds = new[] {100};
        var game = new Game
        {
            Id = gameId,
            Title = "Test Game",
            Expansions = new List<Expansion>
            {
                new() {Id = 1, BggId = 100, Title = "Keep This"},
                new() {Id = 2, BggId = 200, Title = "Remove This"}
            }
        };
        var updatedGame = new Game
        {
            Id = gameId,
            Title = "Test Game",
            Expansions = new List<Expansion>
            {
                new() {Id = 1, BggId = 100, Title = "Keep This"}
            }
        };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId)).ReturnsAsync(game);
        _gameRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Game>())).ReturnsAsync(updatedGame);

        var result = await _gameService.UpdateGameExpansions(gameId, expansionIds);

        result.Should().HaveCount(1);
        result.Should().Contain(x => x.BggId == 100);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _gameRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<Game>(g => g.Expansions.Count == 1 && g.Expansions.First().BggId == 100)),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldAddNewExpansions_WhenNewExpansionIdsProvided()
    {
        const int gameId = 1;
        var expansionIds = new[] {100, 300};
        var game = new Game
        {
            Id = gameId,
            Title = "Test Game",
            Expansions = new List<Expansion>
            {
                new() {Id = 1, BggId = 100, Title = "Existing Expansion"}
            }
        };
        var bggApiGames = new BggApiGames
        {
            Games = [new BggRawGame
                {
                    Id = 300,
                    Names =
                    [
                        new Name
                        {
                            Value = "New Expansion",
                            Type = string.Empty
                        }
                    ],
                    Thumbnail = string.Empty,
                    Image = string.Empty,
                    Description = string.Empty,
                    Type = string.Empty
                }
            ]
        };
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            bggApiGames,
            new RefitSettings()
        );
        var updatedGame = new Game
        {
            Id = gameId,
            Title = "Test Game",
            Expansions = new List<Expansion>
            {
                new() {Id = 1, BggId = 100, Title = "Existing Expansion"},
                new() {Id = 2, BggId = 300, Title = "New Expansion"}
            }
        };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId)).ReturnsAsync(game);
        _bggApiMock.Setup(x => x.SearchExpansion(300, 0)).ReturnsAsync(apiResponse);
        _gameRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Game>())).ReturnsAsync(updatedGame);

        var result = await _gameService.UpdateGameExpansions(gameId, expansionIds);

        result.Should().HaveCount(2);
        result.Should().Contain(x => x.BggId == 100);
        result.Should().Contain(x => x.BggId == 300 && x.Title == "New Expansion");
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggApiMock.Verify(x => x.SearchExpansion(300, 0), Times.Once);
        _gameRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Game>(g =>
            g.Expansions.Count == 2 &&
            g.Expansions.Any(e => e.BggId == 100) &&
            g.Expansions.Any(e => e.BggId == 300))), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGameExpansions_ShouldSkipExpansionWhenApiCallFails()
    {
        const int gameId = 1;
        var expansionIds = new[] {100, 300};
        var game = new Game
        {
            Id = gameId,
            Title = "Test Game",
            Expansions = new List<Expansion>
            {
                new() {Id = 1, BggId = 100, Title = "Existing Expansion"}
            }
        };
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            null,
            new RefitSettings()
        );
        var updatedGame = new Game
        {
            Id = gameId,
            Title = "Test Game",
            Expansions = new List<Expansion>
            {
                new() {Id = 1, BggId = 100, Title = "Existing Expansion"}
            }
        };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId)).ReturnsAsync(game);
        _bggApiMock.Setup(x => x.SearchExpansion(300, 0)).ReturnsAsync(apiResponse);
        _gameRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Game>())).ReturnsAsync(updatedGame);

        var result = await _gameService.UpdateGameExpansions(gameId, expansionIds);

        result.Should().HaveCount(1);
        result.Should().Contain(x => x.BggId == 100);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _bggApiMock.Verify(x => x.SearchExpansion(300, 0), Times.Once);
        _gameRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Game>(g => g.Expansions.Count == 1)), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerScoringChart_ShouldReturnChartDataWithAllPlayers_WhenSessionsExist()
    {
        const int gameId = 1;
        var sessions = new List<Session>
        {
            new()
            {
                Id = 1,
                Start = new DateTime(2024, 1, 1),
                PlayerSessions = new List<PlayerSession>
                {
                    new() {PlayerId = 1, Score = 85.5},
                    new() {PlayerId = 2, Score = 92.0}
                }
            },
            new()
            {
                Id = 2,
                Start = new DateTime(2024, 1, 2),
                PlayerSessions = new List<PlayerSession>
                {
                    new() {PlayerId = 1, Score = 78.0},
                    new() {PlayerId = 3, Score = 95.5}
                }
            }
        };

        _gameRepositoryMock.Setup(x => x.GetSessions(gameId, -200)).ReturnsAsync(sessions);
        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(new Game {HasScoring = true});

        var result = await _gameService.GetPlayerScoringChart(gameId);

        result.Should().HaveCount(2);

        var firstSessionData = result[new DateTime(2024, 1, 1)];
        firstSessionData.Should().HaveCount(3);
        firstSessionData.Should().Contain(x => x.Id == 1 && x.Value == 85.5);
        firstSessionData.Should().Contain(x => x.Id == 2 && x.Value == 92.0);
        firstSessionData.Should().Contain(x => x.Id == 3 && x.Value == null);

        var secondSessionData = result[new DateTime(2024, 1, 2)];
        secondSessionData.Should().HaveCount(3);
        secondSessionData.Should().Contain(x => x.Id == 1 && x.Value == 78.0);
        secondSessionData.Should().Contain(x => x.Id == 2 && x.Value == null);
        secondSessionData.Should().Contain(x => x.Id == 3 && x.Value == 95.5);

        _gameRepositoryMock.Verify(x => x.GetSessions(gameId, -200), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerScoringChart_ShouldReturnEmptyDictionary_WhenNoSessionsExist()
    {
        const int gameId = 1;
        var sessions = new List<Session>();

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(new Game {HasScoring = true});
        _gameRepositoryMock.Setup(x => x.GetSessions(gameId, -200)).ReturnsAsync(sessions);

        var result = await _gameService.GetPlayerScoringChart(gameId);

        result.Should().BeEmpty();

        _gameRepositoryMock.Verify(x => x.GetSessions(gameId, -200), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerScoringChart_ShouldHandleNullScores_WhenPlayerSessionsHaveNullScores()
    {
        const int gameId = 1;
        var sessions = new List<Session>
        {
            new()
            {
                Id = 1,
                Start = new DateTime(2024, 1, 1),
                PlayerSessions = new List<PlayerSession>
                {
                    new() {PlayerId = 1, Score = null},
                    new() {PlayerId = 2, Score = 85.0}
                }
            }
        };

        _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(new Game {HasScoring = true});
        _gameRepositoryMock.Setup(x => x.GetSessions(gameId, -200)).ReturnsAsync(sessions);

        var result = await _gameService.GetPlayerScoringChart(gameId);

        result.Should().HaveCount(1);
        var sessionData = result[new DateTime(2024, 1, 1)];
        sessionData.Should().HaveCount(2);
        sessionData.Should().Contain(x => x.Id == 1 && x.Value == null);
        sessionData.Should().Contain(x => x.Id == 2 && x.Value == 85.0);

        _gameRepositoryMock.Verify(x => x.GetSessions(gameId, -200), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        VerifyNoOtherCalls();
    }


    [Fact]
    public async Task GetGameExpansions_ShouldReturnExpansions_WhenRepositoryReturnsData()
    {
        var expansionIds = new List<int> {1, 2, 3};
        var expectedExpansions = new List<Expansion>
        {
            new() {Id = 1, Title = "Cities and Knights", BggId = 100},
            new() {Id = 2, Title = "Seafarers", BggId = 200},
            new() {Id = 3, Title = "Traders and Barbarians", BggId = 300}
        };

        _gameRepositoryMock.Setup(x => x.GetExpansions(expansionIds)).ReturnsAsync(expectedExpansions);

        var result = await _gameService.GetGameExpansions(expansionIds);

        result.Should().Contain(expectedExpansions);
        _gameRepositoryMock.Verify(x => x.GetExpansions(expansionIds), Times.Once);
        VerifyNoOtherCalls();
    }
    
    [Fact]
public async Task ImportBggCollection_ShouldReturnMappedResult_WhenApiReturnsSuccessWithGames()
{
    const string userName = "testuser";
    var collectionItems = new List<Item>
    {
        new() { Name = new ImportName() { Text = "Scrabble" } },
        new() { Name = new ImportName { Text = "Monopoly" } }
    };
    var bggApiCollection = new BggApiCollection
    {
        Item = collectionItems
    };
    var mappedGames = new List<BggImportGame>
    {
        new() { Title = "Monopoly" },
        new() { Title = "Scrabble" }
    };
    var apiResponse = new ApiResponse<BggApiCollection>(
        new HttpResponseMessage(HttpStatusCode.OK),
        bggApiCollection,
        new RefitSettings()
    );

    _bggApiMock.Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion"))
              .ReturnsAsync(apiResponse);
    _mapperMock.Setup(x => x.Map<List<BggImportGame>>(It.IsAny<List<Item>>()))
              .Returns(mappedGames);

    var result = await _gameService.ImportBggCollection(userName);

    result.Should().NotBeNull();
    result.StatusCode.Should().Be(HttpStatusCode.OK);
    result.Games.Should().BeEquivalentTo(mappedGames);
    result.Games.Should().BeInAscendingOrder(x => x.Title);

    _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion"), Times.Once);
    _mapperMock.Verify(x => x.Map<List<BggImportGame>>(It.Is<List<Item>>(list => 
        list.Count == 2 && 
        list[0].Name.Text == "Monopoly" && 
        list[1].Name.Text == "Scrabble")), Times.Once);
    VerifyNoOtherCalls();
}

[Fact]
public async Task ImportBggCollection_ShouldReturnResultWithoutGames_WhenApiReturnsAcceptedStatus()
{
    const string userName = "testuser";
    var apiResponse = new ApiResponse<BggApiCollection>(
        new HttpResponseMessage(HttpStatusCode.Accepted),
        null,
        new RefitSettings()
    );

    _bggApiMock.Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion"))
              .ReturnsAsync(apiResponse);

    var result = await _gameService.ImportBggCollection(userName);

    result.Should().NotBeNull();
    result.StatusCode.Should().Be(HttpStatusCode.Accepted);
    result.Games.Should().BeEmpty();

    _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion"), Times.Once);
    VerifyNoOtherCalls();
}

[Fact]
public async Task ImportBggCollection_ShouldReturnNull_WhenApiReturnsErrorStatus()
{
    const string userName = "testuser";
    var apiResponse = new ApiResponse<BggApiCollection>(
        new HttpResponseMessage(HttpStatusCode.BadRequest),
        null,
        new RefitSettings()
    );

    _bggApiMock.Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion"))
              .ReturnsAsync(apiResponse);

    var result = await _gameService.ImportBggCollection(userName);

    result.Should().BeNull();

    _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion"), Times.Once);
    VerifyNoOtherCalls();
}

[Fact]
public async Task ImportBggCollection_ShouldReturnOkResultWithEmptyGames_WhenApiReturnsEmptyCollection()
{
    const string userName = "testuser";
    var bggApiCollection = new BggApiCollection
    {
        Item = new List<Item>()
    };
    var mappedGames = new List<BggImportGame>();
    var apiResponse = new ApiResponse<BggApiCollection>(
        new HttpResponseMessage(HttpStatusCode.OK),
        bggApiCollection,
        new RefitSettings()
    );

    _bggApiMock.Setup(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion"))
              .ReturnsAsync(apiResponse);
    _mapperMock.Setup(x => x.Map<List<BggImportGame>>(It.IsAny<List<Item>>()))
              .Returns(mappedGames);

    var result = await _gameService.ImportBggCollection(userName);

    result.Should().NotBeNull();
    result.StatusCode.Should().Be(HttpStatusCode.OK);
    result.Games.Should().BeEmpty();

    _bggApiMock.Verify(x => x.ImportCollection(userName, "boardgame", "boardgameexpansion"), Times.Once);
    _mapperMock.Verify(x => x.Map<List<BggImportGame>>(It.IsAny<List<Item>>()), Times.Once);
    VerifyNoOtherCalls();
}

[Fact]
public async Task ImportList_ShouldProcessAllValidGames_WhenAllGamesFoundInBgg()
{
    var importGames = new List<ImportGame>
    {
        new()
        {
            BggId = 123,
            HasScoring = true,
            State = GameState.Owned,
            AddedDate = DateTime.Today,
            Price = 29.99,
            Title = string.Empty,
            ImageUrl = string.Empty
        },
        new()
        {
            BggId = 456,
            HasScoring = false,
            State = GameState.Wanted,
            AddedDate = DateTime.Today.AddDays(-1),
            Price = 39.99,
            Title = string.Empty,
            ImageUrl = string.Empty
        }
    };
    var bggGame1 = new BggGame
    {
        BggId = 123,
        Names =
        [
            "Game 1"
        ],
        Thumbnail = string.Empty,
        Image = string.Empty,
        Description = string.Empty
    };
    var bggGame2 = new BggGame
    {
        BggId = 456,
        Names =
        [
            "Game 2"
        ],
        Thumbnail = string.Empty,
        Image = string.Empty,
        Description = string.Empty
    };
    var createdGame1 = new Game { Id = 1, BggId = 123, Title = "Game 1" };
    var createdGame2 = new Game { Id = 2, BggId = 456, Title = "Game 2" };

    SetupSearchGameMock(123, bggGame1);
    SetupSearchGameMock(456, bggGame2);
    SetupProcessBggGameDataMock(bggGame1, createdGame1);
    SetupProcessBggGameDataMock(bggGame2, createdGame2);

    await _gameService.ImportList(importGames);

    VerifyProcessBggGameDataCall(bggGame1, 123, GameState.Owned, 29.99, true);
    VerifyProcessBggGameDataCall(bggGame2, 456, GameState.Wanted, 39.99, false);
    
    _bggApiMock.Verify(x => x.SearchGame(123, 1), Times.Once);
    _bggApiMock.Verify(x => x.SearchGame(456, 1), Times.Once);

    _mapperMock.Verify(x => x.Map<IList<GameCategory>>(It.IsAny<BggLink[]>()), Times.Exactly(2));
    _mapperMock.Verify(x => x.Map<IList<GameMechanic>>(It.IsAny<BggLink[]>()), Times.Exactly(2));
    _mapperMock.Verify(x => x.Map<IList<Person>>(It.IsAny<BggPerson[]>()), Times.Exactly(2));
    _mapperMock.Verify(x => x.Map<Game>(bggGame1), Times.Once);
    _mapperMock.Verify(x => x.Map<Game>(bggGame2), Times.Once);
    _mapperMock.Verify(x => x.Map<BggGame>(It.IsAny<BggRawGame>()), Times.Exactly(2));
    
    VerifyNoOtherCalls();
}

[Fact]
public async Task ImportList_ShouldSkipGamesNotFoundInBgg_WhenSomeGamesNotFound()
{
    var importGames = new List<ImportGame>
    {
        new()
        {
            BggId = 123,
            HasScoring = true,
            State = GameState.Owned,
            Title = string.Empty,
            ImageUrl = string.Empty
        },
        new()
        {
            BggId = 999,
            HasScoring = false,
            State = GameState.Wanted,
            Title = string.Empty,
            ImageUrl = string.Empty
        }
    };
    var bggGame1 = new BggGame
    {
        BggId = 123,
        Names =
        [
            "Game 1"
        ],
        Image = "test.png",
        Thumbnail = string.Empty,
        Description = string.Empty
    };
    var createdGame1 = new Game { Id = 1, BggId = 123, Title = "Game 1", BuyingPrice = 10};

    SetupSearchGameMock(123, bggGame1);
    SetupSearchGameMock(999, null);
    SetupProcessBggGameDataMock(bggGame1, createdGame1);

    await _gameService.ImportList(importGames);

    _bggApiMock.Verify(x => x.SearchGame(123, 1), Times.Once);
    _bggApiMock.Verify(x => x.SearchGame(999, 1), Times.Once);
    
    _gameRepositoryMock.Verify(x => x.AddGameCategoriesIfNotExists(It.IsAny<IEnumerable<GameCategory>>()), Times.Once);
    _gameRepositoryMock.Verify(x => x.AddGameMechanicsIfNotExists(It.IsAny<IEnumerable<GameMechanic>>()), Times.Once);
    _gameRepositoryMock.Verify(x => x.AddPeopleIfNotExists(It.IsAny<IEnumerable<Person>>()), Times.Once);
    _gameRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Game>()), Times.Once);

    _mapperMock.Verify(x => x.Map<IList<GameCategory>>(It.IsAny<BggLink[]>()), Times.Once);
    _mapperMock.Verify(x => x.Map<IList<GameMechanic>>(It.IsAny<BggLink[]>()), Times.Once);
    _mapperMock.Verify(x => x.Map<IList<Person>>(It.IsAny<BggPerson[]>()), Times.Once);
    _mapperMock.Verify(x => x.Map<Game>(bggGame1), Times.Once);
    _mapperMock.Verify(x => x.Map<BggGame>(It.IsAny<BggRawGame>()), Times.Once);
    
    _imageServiceMock.Verify(x => x.DownloadImage("test.png", "123"), Times.Once);
    
    VerifyNoOtherCalls();
}

[Fact]
public async Task ImportList_ShouldDoNothing_WhenEmptyGameListProvided()
{
    var importGames = new List<ImportGame>();

    await _gameService.ImportList(importGames);

    VerifyNoOtherCalls();
}

[Fact]
public async Task ImportList_ShouldDoNothing_WhenAllGamesNotFoundInBgg()
{
    var importGames = new List<ImportGame>
    {
        new()
        {
            BggId = 123,
            Title = string.Empty,
            ImageUrl = string.Empty
        },
        new()
        {
            BggId = 456,
            Title = string.Empty,
            ImageUrl = string.Empty
        }
    };

    SetupSearchGameMock(123, null);
    SetupSearchGameMock(456, null);

    await _gameService.ImportList(importGames);

    _bggApiMock.Verify(x => x.SearchGame(123, 1), Times.Once);
    _bggApiMock.Verify(x => x.SearchGame(456, 1), Times.Once);
    VerifyNoOtherCalls();
}

private void SetupSearchGameMock(int bggId, BggGame? returnValue)
{
    if (returnValue != null)
    {
        var bggApiGames = new BggApiGames
        {
            Games = [new BggRawGame
                {
                    Id = bggId,
                    Names =
                    [
                        new Name
                        {
                            Value = returnValue.Names.First(),
                            Type = string.Empty
                        }
                    ],
                    Thumbnail = string.Empty,
                    Image = string.Empty,
                    Description = string.Empty,
                    Type = string.Empty
                }
            ]
        };
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.OK),
            bggApiGames,
            new RefitSettings()
        );

        _bggApiMock.Setup(x => x.SearchGame(bggId, 1)).ReturnsAsync(apiResponse);
        _mapperMock.Setup(x => x.Map<BggGame>(apiResponse.Content.Games.First())).Returns(returnValue);
    }
    else
    {
        var apiResponse = new ApiResponse<BggApiGames>(
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            null,
            new RefitSettings()
        );

        _bggApiMock.Setup(x => x.SearchGame(bggId, 1)).ReturnsAsync(apiResponse);
    }
}

private void SetupProcessBggGameDataMock(BggGame bggGame, Game returnValue)
{
    var categories = new List<GameCategory>();
    var mechanics = new List<GameMechanic>();
    var people = new List<Person>();
    var mappedGame = new Game { Title = bggGame.Names.First(), BggId = bggGame.BggId };
    var downloadedImage = $"/images/{bggGame.BggId}.jpg";

    _mapperMock.Setup(x => x.Map<IList<GameCategory>>(bggGame.Categories)).Returns(categories);
    _mapperMock.Setup(x => x.Map<IList<GameMechanic>>(bggGame.Mechanics)).Returns(mechanics);
    _mapperMock.Setup(x => x.Map<IList<Person>>(bggGame.People)).Returns(people);
    _mapperMock.Setup(x => x.Map<Game>(bggGame)).Returns(mappedGame);
    _imageServiceMock.Setup(x => x.DownloadImage(bggGame.Image, bggGame.BggId.ToString()))
        .ReturnsAsync(downloadedImage);
    _gameRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Game>())).ReturnsAsync(returnValue);
}

private void VerifyProcessBggGameDataCall(BggGame bggGame, int bggId, GameState state, double? price, bool hasScoring)
{
    _gameRepositoryMock.Verify(x => x.AddGameCategoriesIfNotExists(It.IsAny<IList<GameCategory>>()), Times.AtLeastOnce);
    _gameRepositoryMock.Verify(x => x.AddGameMechanicsIfNotExists(It.IsAny<IList<GameMechanic>>()), Times.AtLeastOnce);
    _gameRepositoryMock.Verify(x => x.AddPeopleIfNotExists(It.IsAny<IList<Person>>()), Times.AtLeastOnce);
    _mapperMock.Verify(x => x.Map<Game>(bggGame), Times.AtLeastOnce);
    _imageServiceMock.Verify(x => x.DownloadImage(bggGame.Image, bggId.ToString()), Times.AtLeastOnce);
    _gameRepositoryMock.Verify(x => x.CreateAsync(It.Is<Game>(g =>
        g.State == state &&
        g.BuyingPrice == price &&
        g.HasScoring == hasScoring)), Times.AtLeastOnce);
}

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _bggApiMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
    }

    private void VerifyPlayFlagsRepositoryCalls(int gameId)
    {
        _gameRepositoryMock.Verify(x => x.GetShortestPlay(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetLongestPlay(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetHighScorePlay(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetLowestScorePlay(gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    private void SetupStatsRepositoryCalls(int gameId, Player? mostWinPlayer)
    {
        _gameRepositoryMock.Setup(x => x.GetPlayCount(gameId)).ReturnsAsync(15);
        _gameRepositoryMock.Setup(x => x.GetTotalPlayedTime(gameId)).ReturnsAsync(TimeSpan.FromMinutes(300));
        _gameRepositoryMock.Setup(x => x.GetPricePerPlay(gameId)).ReturnsAsync(2.50);
        _gameRepositoryMock.Setup(x => x.GetHighestScore(gameId)).ReturnsAsync(100.0);
        _gameRepositoryMock.Setup(x => x.GetAveragePlayTime(gameId)).ReturnsAsync(20.0);
        _gameRepositoryMock.Setup(x => x.GetAverageScore(gameId)).ReturnsAsync(75.0);
        _gameRepositoryMock.Setup(x => x.GetExpansionCount(gameId)).ReturnsAsync(5);
        _gameRepositoryMock.Setup(x => x.GetLastPlayedDateTime(gameId)).ReturnsAsync(DateTime.Today);
        _gameRepositoryMock.Setup(x => x.GetMostWins(gameId)).ReturnsAsync(mostWinPlayer);
    }

    private void VerifyStatsRepositoryCalls(int gameId, int playerId)
    {
        _gameRepositoryMock.Verify(x => x.GetPlayCount(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalPlayedTime(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetPricePerPlay(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetHighestScore(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetAveragePlayTime(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetAverageScore(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetExpansionCount(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetLastPlayedDateTime(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetMostWins(gameId), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetWinCount(playerId, gameId), Times.Once);
        VerifyNoOtherCalls();
    }

    private void VerifyStatsRepositoryCallsWithoutPlayer(int gameId)
    {
        _gameRepositoryMock.Verify(x => x.GetPlayCount(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalPlayedTime(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetPricePerPlay(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetHighestScore(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetAveragePlayTime(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetAverageScore(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetExpansionCount(gameId), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetLastPlayedDateTime(gameId), Times.Once);
        VerifyNoOtherCalls();
    }
    
    private static IGrouping<T, Session> CreateGrouping<T>(T key, int count)
    {
        var sessions = Enumerable.Range(1, count).Select(i => new Session {Id = i}).ToList();
        return new TestGrouping<T, Session>(key, sessions);
    }

    private static List<Session> CreateSessionsWithPlayerSessions()
    {
        return
        [
            new Session
            {
                PlayerSessions = new List<PlayerSession>
                {
                    new() {PlayerId = 1, Won = true, Session = new Session {Start = DateTime.Now}},
                    new() {PlayerId = 2, Session = new Session {Start = DateTime.Now.AddDays(-1)}}
                }
            }
        ];
    }
}