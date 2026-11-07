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
    private const int RecentGamesCount = 8;
    private const int FrequentPlayersCount = 3;

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
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _sessionRepositoryMock
            .Setup(x => x.GetRecentlyPlayedGameIds(RecentGamesCount, token))
            .ReturnsAsync([4, 2, 9]);
        _playerRepositoryMock
            .Setup(x => x.GetTopPlayers(FrequentPlayersCount, token))
            .ReturnsAsync([(7, "Mikhael", null, 12, 4), (3, "Reggi", null, 9, 2)]);
        _sessionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LastUsedLocationSpec>(), token))
            .ReturnsAsync(5);

        var result = await _service.GetSuggestions(token);

        result.RecentGameIds.Should().Equal(4, 2, 9);
        result.FrequentPlayerIds.Should().Equal(7, 3);
        result.LastLocationId.Should().Be(5);

        VerifyLookups(token);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSuggestions_ShouldReturnEmptySuggestions_WhenNoSessionsExist()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _sessionRepositoryMock
            .Setup(x => x.GetRecentlyPlayedGameIds(RecentGamesCount, token))
            .ReturnsAsync([]);
        _playerRepositoryMock
            .Setup(x => x.GetTopPlayers(FrequentPlayersCount, token))
            .ReturnsAsync([]);
        _sessionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LastUsedLocationSpec>(), token))
            .ReturnsAsync((int?)null);

        var result = await _service.GetSuggestions(token);

        result.RecentGameIds.Should().BeEmpty();
        result.FrequentPlayerIds.Should().BeEmpty();
        result.LastLocationId.Should().BeNull();

        VerifyLookups(token);
        VerifyNoOtherCalls();
    }

    private void VerifyLookups(CancellationToken token)
    {
        _sessionRepositoryMock.Verify(x => x.GetRecentlyPlayedGameIds(RecentGamesCount, token), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetTopPlayers(FrequentPlayersCount, token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.IsAny<LastUsedLocationSpec>(), token), Times.Once);
    }
}
