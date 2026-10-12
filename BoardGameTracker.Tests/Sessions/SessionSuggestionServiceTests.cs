using BoardGameTracker.Common;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions;
using BoardGameTracker.Core.Sessions.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Sessions;

public class SessionSuggestionServiceTests
{
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly SessionSuggestionService _service;

    public SessionSuggestionServiceTests()
    {
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _service = new SessionSuggestionService(_sessionRepositoryMock.Object, _playerRepositoryMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSuggestions_ShouldCombineRecentGamesFrequentPlayersAndLastLocation()
    {
        _sessionRepositoryMock
            .Setup(x => x.GetRecentlyPlayedGameIds(Constants.SessionForm.RecentGamesCount, It.IsAny<CancellationToken>()))
            .ReturnsAsync([4, 2, 9]);
        _playerRepositoryMock
            .Setup(x => x.GetTopPlayers(Constants.SessionForm.FrequentPlayersCount, It.IsAny<CancellationToken>()))
            .ReturnsAsync([(7, "Mikhael", null, 12, 4), (3, "Reggi", null, 9, 2)]);
        _sessionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LastUsedLocationSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _service.GetSuggestions(TestContext.Current.CancellationToken);

        result.RecentGameIds.Should().Equal(4, 2, 9);
        result.FrequentPlayerIds.Should().Equal(7, 3);
        result.LastLocationId.Should().Be(5);

        _sessionRepositoryMock.Verify(x => x.GetRecentlyPlayedGameIds(Constants.SessionForm.RecentGamesCount, It.IsAny<CancellationToken>()), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTopPlayers(Constants.SessionForm.FrequentPlayersCount, It.IsAny<CancellationToken>()), Times.Once);
        _sessionRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.IsAny<LastUsedLocationSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSuggestions_ShouldReturnEmptySuggestions_WhenNoSessionsExist()
    {
        _sessionRepositoryMock
            .Setup(x => x.GetRecentlyPlayedGameIds(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _playerRepositoryMock
            .Setup(x => x.GetTopPlayers(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _sessionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LastUsedLocationSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var result = await _service.GetSuggestions(TestContext.Current.CancellationToken);

        result.RecentGameIds.Should().BeEmpty();
        result.FrequentPlayerIds.Should().BeEmpty();
        result.LastLocationId.Should().BeNull();

        _sessionRepositoryMock.Verify(x => x.GetRecentlyPlayedGameIds(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTopPlayers(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _sessionRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.IsAny<LastUsedLocationSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }
}
