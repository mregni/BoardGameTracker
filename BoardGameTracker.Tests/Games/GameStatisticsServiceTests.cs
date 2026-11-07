using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class GameStatisticsServiceTests
{
    private const int GameId = 7;
    private static readonly Session GameSession = new(GameId, new DateTime(2024, 1, 1), new DateTime(2024, 1, 2), string.Empty);
    private static readonly Session OtherGameSession = new(GameId + 1, new DateTime(2024, 1, 1), new DateTime(2024, 1, 2), string.Empty);

    private readonly CancellationToken _token = new CancellationTokenSource().Token;
    private readonly Mock<IReadRepository<Session>> _sessionRepositoryMock;
    private readonly Mock<IGameStatisticsRepository> _gameStatisticsRepositoryMock;
    private readonly GameStatisticsService _service;

    public GameStatisticsServiceTests()
    {
        _sessionRepositoryMock = new Mock<IReadRepository<Session>>();
        _gameStatisticsRepositoryMock = new Mock<IGameStatisticsRepository>();

        _service = new GameStatisticsService(
            _sessionRepositoryMock.Object,
            _gameStatisticsRepositoryMock.Object,
            NullLogger<GameStatisticsService>.Instance);
    }

    #region CalculateStatisticsAsync Tests

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnNullOptionalValues_WhenNotAvailable()
    {
        var gameId = GameId;
        SetupDefaultRepositoryMocks(gameId);

        var result = await _service.CalculateStatisticsAsync(gameId, _token);

        result.PricePerPlay.Should().BeNull();
        result.HighScore.Should().BeNull();
        result.AverageScore.Should().BeNull();
        result.LastPlayed.Should().BeNull();
        result.ExpansionCount.Should().BeNull();
        result.MostWinsPlayer.Should().BeNull();
        result.ScoredSessionCount.Should().Be(0);
        result.CoopWinCount.Should().Be(0);
        result.CoopLossCount.Should().Be(0);
        VerifyAllStatisticsRead(gameId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldUseEmptyString_WhenPlayerImageIsNull()
    {
        var gameId = GameId;
        var playerId = 5;
        var player = new Player("Jane Doe") { Id = playerId };

        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostWins(gameId, _token)).ReturnsAsync((player, 10));

        var result = await _service.CalculateStatisticsAsync(gameId, _token);

        result.MostWinsPlayer.Should().BeEquivalentTo(new MostWinningPlayer { Id = playerId, Name = "Jane Doe", Image = string.Empty, TotalWins = 10 });
        VerifyAllStatisticsRead(gameId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnCompleteStatistics()
    {
        var gameId = GameId;
        var lastPlayed = new DateTime(2024, 6, 15);
        var player = new Player("Winner", "winner.jpg") { Id = 10 };

        _sessionRepositoryMock.Setup(x => x.CountAsync(It.Is<SessionsByGameSpec>(s => s.IsSatisfiedBy(GameSession) && !s.IsSatisfiedBy(OtherGameSession)), _token)).ReturnsAsync(50);
        _gameStatisticsRepositoryMock.Setup(x => x.GetTotalPlayedTime(gameId, _token)).ReturnsAsync(3000.0);
        _sessionRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.Is<LastPlayedDateSpec>(s => s.IsSatisfiedBy(GameSession) && !s.IsSatisfiedBy(OtherGameSession)), _token)).ReturnsAsync(lastPlayed);
        _gameStatisticsRepositoryMock.Setup(x => x.GetPricePerPlay(gameId, _token)).ReturnsAsync(1.50m);
        _gameStatisticsRepositoryMock.Setup(x => x.GetHighestScore(gameId, _token)).ReturnsAsync(250.0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetAveragePlayTime(gameId, _token)).ReturnsAsync(60.0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetAverageScore(gameId, _token)).ReturnsAsync(150.0);
        _gameStatisticsRepositoryMock.Setup(x => x.CountScoredSessions(gameId, _token)).ReturnsAsync(4);
        _gameStatisticsRepositoryMock.Setup(x => x.GetExpansionCount(gameId, _token)).ReturnsAsync(5);
        _gameStatisticsRepositoryMock.Setup(x => x.GetCoopResults(gameId, _token)).ReturnsAsync((2, 3));
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostWins(gameId, _token)).ReturnsAsync((player, 20));

        var result = await _service.CalculateStatisticsAsync(gameId, _token);

        result.PlayCount.Should().Be(50);
        result.TotalPlayedTime.Should().Be(3000.0);
        result.LastPlayed.Should().Be(lastPlayed);
        result.PricePerPlay.Should().Be(1.50m);
        result.HighScore.Should().Be(250.0);
        result.AveragePlayTime.Should().Be(60.0);
        result.AverageScore.Should().Be(150.0);
        result.ScoredSessionCount.Should().Be(4);
        result.ExpansionCount.Should().Be(5);
        result.MostWinsPlayer.Should().NotBeNull();
        result.MostWinsPlayer!.Id.Should().Be(10);
        result.MostWinsPlayer.Name.Should().Be("Winner");
        result.MostWinsPlayer.Image.Should().Be("winner.jpg");
        result.MostWinsPlayer.TotalWins.Should().Be(20);
        result.CoopWinCount.Should().Be(2);
        result.CoopLossCount.Should().Be(3);
        VerifyAllStatisticsRead(gameId);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CalculateStatisticsAsync_ShouldReturnTheCoopResults()
    {
        var gameId = GameId;
        SetupDefaultRepositoryMocks(gameId);
        _gameStatisticsRepositoryMock.Setup(x => x.GetCoopResults(gameId, _token)).ReturnsAsync((3, 1));

        var result = await _service.CalculateStatisticsAsync(gameId, _token);

        result.CoopWinCount.Should().Be(3);
        result.CoopLossCount.Should().Be(1);
        VerifyAllStatisticsRead(gameId);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Helper Methods

    private void VerifyNoOtherCalls()
    {
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _gameStatisticsRepositoryMock.VerifyNoOtherCalls();
    }

    private void VerifyAllStatisticsRead(int gameId)
    {
        _sessionRepositoryMock.Verify(x => x.CountAsync(It.Is<SessionsByGameSpec>(s => s.IsSatisfiedBy(GameSession) && !s.IsSatisfiedBy(OtherGameSession)), _token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.Is<LastPlayedDateSpec>(s => s.IsSatisfiedBy(GameSession) && !s.IsSatisfiedBy(OtherGameSession)), _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetTotalPlayedTime(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetPricePerPlay(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetHighestScore(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetAveragePlayTime(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetAverageScore(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.CountScoredSessions(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetExpansionCount(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetCoopResults(gameId, _token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMostWins(gameId, _token), Times.Once);
    }

    private void SetupDefaultRepositoryMocks(int gameId)
    {
        _sessionRepositoryMock.Setup(x => x.CountAsync(It.Is<SessionsByGameSpec>(s => s.IsSatisfiedBy(GameSession) && !s.IsSatisfiedBy(OtherGameSession)), _token)).ReturnsAsync(0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetTotalPlayedTime(gameId, _token)).ReturnsAsync(0);
        _sessionRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.Is<LastPlayedDateSpec>(s => s.IsSatisfiedBy(GameSession) && !s.IsSatisfiedBy(OtherGameSession)), _token)).ReturnsAsync((DateTime?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetPricePerPlay(gameId, _token)).ReturnsAsync((decimal?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetHighestScore(gameId, _token)).ReturnsAsync((double?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetAveragePlayTime(gameId, _token)).ReturnsAsync(0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetAverageScore(gameId, _token)).ReturnsAsync((double?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetExpansionCount(gameId, _token)).ReturnsAsync((int?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostWins(gameId, _token)).ReturnsAsync(((Player?)null, 0));
        _gameStatisticsRepositoryMock.Setup(x => x.GetCoopResults(gameId, _token)).ReturnsAsync((0, 0));
        _gameStatisticsRepositoryMock.Setup(x => x.CountScoredSessions(gameId, _token)).ReturnsAsync(0);
    }

    #endregion
}
