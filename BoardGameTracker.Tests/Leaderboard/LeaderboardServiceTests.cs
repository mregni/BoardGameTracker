using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Leaderboard;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Leaderboard;

public class LeaderboardServiceTests
{
    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();
    private readonly LeaderboardService _service;

    public LeaderboardServiceTests()
    {
        _service = new LeaderboardService(_playerRepositoryMock.Object);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldRankByWins_ThenWinRate_ThenPlays()
    {
        _playerRepositoryMock.Setup(x => x.GetLeaderboardRows(TestContext.Current.CancellationToken)).ReturnsAsync(
        [
            new LeaderboardRow(1, "Alice", null, 10, 4, 6, 600),
            new LeaderboardRow(2, "Bob", "bob.webp", 4, 4, 2, 120),
            new LeaderboardRow(3, "Cara", null, 12, 2, 3, 900),
            new LeaderboardRow(4, "Dan", null, 3, 0, 0, 45),
        ]);

        var result = await _service.GetLeaderboardAsync(TestContext.Current.CancellationToken);

        result.Players.Select(x => (x.Rank, x.Name)).Should().ContainInOrder((1, "Bob"), (2, "Alice"), (3, "Cara"), (4, "Dan"));
        result.Players[0].WinPercentage.Should().Be(100);
        result.Players[1].WinPercentage.Should().Be(40);
        result.Players[2].PodiumCount.Should().Be(3);
        result.MostWins!.Name.Should().Be("Bob");
        result.MostPlays!.Name.Should().Be("Cara");
        result.MostTimePlayed!.Name.Should().Be("Cara");
        result.BestWinRate!.Name.Should().Be("Alice");
        result.MinimumPlaysForWinRate.Should().Be(LeaderboardService.MinimumPlaysForWinRate);
        _playerRepositoryMock.Verify(x => x.GetLeaderboardRows(TestContext.Current.CancellationToken), Times.Once);
        _playerRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldLeaveTheCardsEmpty_WhenNobodyPlayed()
    {
        _playerRepositoryMock.Setup(x => x.GetLeaderboardRows(TestContext.Current.CancellationToken)).ReturnsAsync([]);

        var result = await _service.GetLeaderboardAsync(TestContext.Current.CancellationToken);

        result.Players.Should().BeEmpty();
        result.MostWins.Should().BeNull();
        result.MostPlays.Should().BeNull();
        result.BestWinRate.Should().BeNull();
        result.MostTimePlayed.Should().BeNull();
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldRoundMinutes_AndIgnoreWinlessPlayersForTheWinsCard()
    {
        _playerRepositoryMock.Setup(x => x.GetLeaderboardRows(TestContext.Current.CancellationToken)).ReturnsAsync(
        [
            new LeaderboardRow(1, "Alice", null, 2, 0, 1, 90.6),
        ]);

        var result = await _service.GetLeaderboardAsync(TestContext.Current.CancellationToken);

        result.Players.Single().MinutesPlayed.Should().Be(91);
        result.MostWins.Should().BeNull();
        result.BestWinRate.Should().BeNull();
        result.MostPlays!.Name.Should().Be("Alice");
    }
}
