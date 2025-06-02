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
                Names = ["Test Game"],
                YearPublished = 2020
            };
            var search = new BggSearch
            {
                State = GameState.Owned,
                Price = 29.99,
                AdditionDate = DateTime.Now,
                HasScoring = true
            };
            var categories = new List<GameCategory> { new() { Name = "Strategy" } };
            var mechanics = new List<GameMechanic> { new() { Name = "Worker Placement" } };
            var people = new List<Person> { new() { Name = "John Designer" } };
            var mappedGame = new Game { Title = "Test Game", BggId = 123 };
            var createdGame = new Game { Id = 1, Title = "Test Game", BggId = 123 };
            var downloadedImage = "/images/123.jpg";

            _mapperMock.Setup(x => x.Map<IList<GameCategory>>(rawGame.Categories)).Returns(categories);
            _mapperMock.Setup(x => x.Map<IList<GameMechanic>>(rawGame.Mechanics)).Returns(mechanics);
            _mapperMock.Setup(x => x.Map<IList<Person>>(rawGame.People)).Returns(people);
            _mapperMock.Setup(x => x.Map<Game>(rawGame)).Returns(mappedGame);
            _imageServiceMock.Setup(x => x.DownloadImage(rawGame.Image, rawGame.BggId.ToString())).ReturnsAsync(downloadedImage);
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
            
            _mapperMock.Verify(x => x.Map<IList<GameCategory>>(null), Times.Once);
            _mapperMock.Verify(x => x.Map<IList<GameMechanic>>(null), Times.Once);
            _mapperMock.Verify(x => x.Map<IList<Person>>(null), Times.Once);
            _mapperMock.Verify(x => x.Map<Game>(rawGame), Times.Once);
            
            _imageServiceMock.Verify(x => x.DownloadImage(rawGame.Image, rawGame.BggId.ToString()), Times.Once);
                
            VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetGameByBggId_ShouldReturnGame_WhenGameExists()
        {
            const int bggId = 123;
            var expectedGame = new Game { BggId = bggId, Title = "Test Game" };

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
                new() { Id = 1, Title = "Game 1" },
                new() { Id = 2, Title = "Game 2" }
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
            var expectedGame = new Game { Id = gameId, Title = "Test Game" };

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
            var game = new Game { Id = gameId, Title = "Test Game", Image = "/images/test.jpg" };

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

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId)).ReturnsAsync((Game?)null);

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
                new TestGrouping<int, int>(2, [1,2,3,4,5]),
                new TestGrouping<int, int>(4, [1,2,3])
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
            var mostWinPlayer = new Player { Id = 5, Name = "Winner", Image = "winner.jpg" };
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
                Games = [new BggRawGame { Id = bggId, Names = [new Name { Value = "Test Game"}] }]
            };
            var expectedBggGame = new BggGame { BggId = bggId, Names = ["Test Game"]};
            var apiResponse = new ApiResponse<BggApiGames>(
                new HttpResponseMessage(HttpStatusCode.OK),
                bggApiGames,
                new RefitSettings()
                );
            
            _bggApiMock.Setup(x => x.SearchGame(bggId, "boardgame", 1)).ReturnsAsync(apiResponse);
            _mapperMock.Setup(x => x.Map<BggGame>(apiResponse.Content.Games.First())).Returns(expectedBggGame);

            var result = await _gameService.SearchAndCreateGame(bggId);

            result.Should().Be(expectedBggGame);
            _bggApiMock.Verify(x => x.SearchGame(bggId, "boardgame", 1), Times.Once);
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
        
            _bggApiMock.Setup(x => x.SearchGame(bggId, "boardgame", 1)).ReturnsAsync(apiResponse);
        
            var result = await _gameService.SearchAndCreateGame(bggId);
        
            result.Should().BeNull();
            _bggApiMock.Verify(x => x.SearchGame(bggId, "boardgame", 1), Times.Once);
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
        
            _bggApiMock.Setup(x => x.SearchGame(bggId, "boardgame", 1)).ReturnsAsync(apiResponse);
        
            var result = await _gameService.SearchAndCreateGame(bggId);
        
            result.Should().BeNull();
            _bggApiMock.Verify(x => x.SearchGame(bggId, "boardgame", 1), Times.Once);
            VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetTopPlayers_ShouldReturnTopFivePlayersWithWins_WhenSessionsExist()
        {
            const int gameId = 1;
            var sessions = CreateSessionsWithPlayerSessions();
            var topPlayers = new List<TopPlayer>
            {
                new() { PlayerId = 1, Wins = 5, PlayCount = 10 },
                new() { PlayerId = 2, Wins = 3, PlayCount = 8 },
                new() { PlayerId = 3, Wins = 2, PlayCount = 5 }
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
            var inputGame = new Game { Title = "New Game" };
            var expectedGame = new Game { Id = 1, Title = "New Game" };

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
                new() { Id = 1, GameId = gameId },
                new() { Id = 2, GameId = gameId }
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
            var inputGame = new Game { Id = 1, Title = "Updated Game" };
            var expectedGame = new Game { Id = 1, Title = "Updated Game" };

            _gameRepositoryMock.Setup(x => x.UpdateAsync(inputGame)).ReturnsAsync(expectedGame);

            var result = await _gameService.UpdateGame(inputGame);

            result.Should().Be(expectedGame);
            _gameRepositoryMock.Verify(x => x.UpdateAsync(inputGame), Times.Once);
            VerifyNoOtherCalls();
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
            _gameRepositoryMock.Verify(x => x.GetLastPlayedDateTime(gameId), Times.Once);
            VerifyNoOtherCalls();
        }

        private static IGrouping<T, Session> CreateGrouping<T>(T key, int count)
        {
            var sessions = Enumerable.Range(1, count).Select(i => new Session { Id = i }).ToList();
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
                        new() {PlayerId = 1, Won = true, Session = new Session(){ Start = DateTime.Now}},
                        new() {PlayerId = 2, Session = new Session(){ Start = DateTime.Now.AddDays(-1)}}
                    }
                }
            ];
        }
    }