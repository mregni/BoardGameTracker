using System.Threading.Tasks;
using BoardGameTracker.Common.Enums;
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

    private void VerifyNoOtherCalls()
    {
        _playerRepositoryMock.VerifyNoOtherCalls();
    }

    private void SetupRows(LeaderboardFilter filter, List<LeaderboardRow> rows)
    {
        _playerRepositoryMock
            .Setup(x => x.GetLeaderboardRows(filter, TestContext.Current.CancellationToken))
            .ReturnsAsync(rows);
    }

    private void VerifyRowsLoaded(LeaderboardFilter filter)
    {
        _playerRepositoryMock.Verify(x => x.GetLeaderboardRows(filter, TestContext.Current.CancellationToken), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldRankByWins_ThenWinRate_ThenPlays()
    {
        var filter = new LeaderboardFilter(MinSessions: 3);
        SetupRows(filter,
        [
            new LeaderboardRow(1, "Alice", null, 10, 4, 6, 600),
            new LeaderboardRow(2, "Bob", "bob.webp", 4, 4, 2, 120),
            new LeaderboardRow(3, "Cara", null, 12, 2, 3, 900),
            new LeaderboardRow(4, "Dan", null, 3, 0, 0, 45),
        ]);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.Players.Select(x => (x.Rank, x.Name)).Should().Equal((1, "Bob"), (2, "Alice"), (3, "Cara"), (4, "Dan"));
        result.Players.Should().OnlyContain(x => x.IsRanked && x.SessionsToRank == 0);
        result.Players[0].WinPercentage.Should().Be(100);
        result.Players[1].WinPercentage.Should().Be(40);
        result.Players[2].PodiumCount.Should().Be(3);
        result.MostWins!.Name.Should().Be("Bob");
        result.MostPlays!.Name.Should().Be("Cara");
        result.MostTimePlayed!.Name.Should().Be("Cara");
        result.BestWinRate!.Name.Should().Be("Bob");
        result.MinimumSessions.Should().Be(3);
        result.MinimumPlayersForPodium.Should().Be(LeaderboardService.MinimumPlayersForPodium);
        result.RankBy.Should().Be(LeaderboardRankBy.Wins);
        result.RankedCount.Should().Be(4);
        VerifyRowsLoaded(filter);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldRankByWinRate_WhenAsked()
    {
        var filter = new LeaderboardFilter(MinSessions: 1, RankBy: LeaderboardRankBy.WinRate);
        SetupRows(filter,
        [
            new LeaderboardRow(1, "Alice", null, 10, 6, 0, 600),
            new LeaderboardRow(2, "Bob", null, 4, 3, 0, 120),
            new LeaderboardRow(3, "Cara", null, 8, 6, 0, 300),
            new LeaderboardRow(4, "Dan", null, 2, 0, 0, 45),
        ]);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.Players.Select(x => (x.Rank, x.Name)).Should().Equal((1, "Cara"), (2, "Bob"), (3, "Alice"), (4, "Dan"));
        result.RankBy.Should().Be(LeaderboardRankBy.WinRate);
        VerifyRowsLoaded(filter);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldRankBySessions_WhenAsked()
    {
        var filter = new LeaderboardFilter(MinSessions: 1, RankBy: LeaderboardRankBy.Sessions);
        SetupRows(filter,
        [
            new LeaderboardRow(1, "Alice", null, 10, 6, 0, 600),
            new LeaderboardRow(2, "Bob", null, 14, 3, 0, 120),
            new LeaderboardRow(3, "Cara", null, 10, 2, 0, 300),
        ]);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.Players.Select(x => (x.Rank, x.Name)).Should().Equal((1, "Bob"), (2, "Alice"), (3, "Cara"));
        VerifyRowsLoaded(filter);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldGiveTiedPlayersTheSameRank()
    {
        var filter = new LeaderboardFilter();
        SetupRows(filter,
        [
            new LeaderboardRow(1, "Zoe", null, 6, 3, 0, 300),
            new LeaderboardRow(2, "Adam", null, 6, 3, 0, 200),
            new LeaderboardRow(3, "Mia", null, 5, 1, 0, 100),
        ]);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.Players.Select(x => (x.Rank, x.Name)).Should().Equal((1, "Adam"), (1, "Zoe"), (3, "Mia"));
        VerifyRowsLoaded(filter);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldShareTheRank_WhenWinRateAndWinsAreEqual()
    {
        var filter = new LeaderboardFilter(MinSessions: 1, RankBy: LeaderboardRankBy.WinRate);
        SetupRows(filter,
        [
            new LeaderboardRow(1, "Zoe", null, 3, 1, 0, 300),
            new LeaderboardRow(2, "Adam", null, 3, 1, 0, 200),
            new LeaderboardRow(3, "Mia", null, 6, 2, 0, 100),
            new LeaderboardRow(4, "Lou", null, 4, 0, 0, 100),
        ]);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.Players.Select(x => (x.Rank, x.Name)).Should().Equal((1, "Mia"), (2, "Adam"), (2, "Zoe"), (4, "Lou"));
        VerifyRowsLoaded(filter);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldBreakTiesOnWinsAndWinRate_ByPlaysThenName()
    {
        var filter = new LeaderboardFilter(MinSessions: 1);
        SetupRows(filter,
        [
            new LeaderboardRow(1, "finn", null, 2, 0, 0, 60),
            new LeaderboardRow(2, "Eve", null, 5, 0, 0, 150),
            new LeaderboardRow(3, "Aaron", null, 2, 0, 0, 60),
        ]);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.Players.Select(x => (x.Rank, x.Name)).Should().Equal((1, "Eve"), (2, "Aaron"), (2, "finn"));
        VerifyRowsLoaded(filter);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldListPlayersBelowTheMinimum_AfterTheRankedOnes_WithoutARank()
    {
        var filter = new LeaderboardFilter(MinSessions: 5, RankBy: LeaderboardRankBy.WinRate);
        SetupRows(filter,
        [
            new LeaderboardRow(1, "Alice", null, 10, 3, 0, 600),
            new LeaderboardRow(2, "Bob", null, 4, 4, 0, 120),
            new LeaderboardRow(3, "Cara", null, 1, 1, 0, 30),
            new LeaderboardRow(4, "Dan", null, 5, 1, 0, 45),
        ]);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.Players.Select(x => (x.Rank, x.Name, x.IsRanked, x.SessionsToRank)).Should().Equal(
            (1, "Alice", true, 0),
            (2, "Dan", true, 0),
            (null, "Bob", false, 1),
            (null, "Cara", false, 4));
        result.RankedCount.Should().Be(2);
        result.BestWinRate!.Name.Should().Be("Alice");
        result.MostWins!.Name.Should().Be("Bob");
        VerifyRowsLoaded(filter);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldTreatAMinimumBelowOneAsOne()
    {
        var filter = new LeaderboardFilter(MinSessions: 0);
        SetupRows(filter, [new LeaderboardRow(1, "Alice", null, 1, 0, 0, 60)]);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.MinimumSessions.Should().Be(1);
        result.Players.Single().IsRanked.Should().BeTrue();
        VerifyRowsLoaded(filter);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(6, true)]
    public async Task GetLeaderboardAsync_ShouldShowThePodium_OnlyWithAtLeastFourRankedPlayers(int rankedPlayers, bool expected)
    {
        var filter = new LeaderboardFilter(GameId: 7, MinSessions: 3);
        var rows = Enumerable.Range(1, rankedPlayers)
            .Select(i => new LeaderboardRow(i, $"Player {i}", null, 3 + i, i, 0, 60))
            .Append(new LeaderboardRow(99, "Newcomer", null, 1, 1, 0, 30))
            .ToList();
        SetupRows(filter, rows);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.ShowPodium.Should().Be(expected);
        result.RankedCount.Should().Be(rankedPlayers);
        VerifyRowsLoaded(filter);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldPassTheFiltersToTheRepository()
    {
        var filter = new LeaderboardFilter(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), 12, 10, LeaderboardRankBy.Sessions);
        SetupRows(filter, []);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.MinimumSessions.Should().Be(10);
        result.RankBy.Should().Be(LeaderboardRankBy.Sessions);
        VerifyRowsLoaded(filter);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldLeaveTheCardsEmpty_WhenNobodyPlayed()
    {
        var filter = new LeaderboardFilter();
        SetupRows(filter, []);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.Players.Should().BeEmpty();
        result.MostWins.Should().BeNull();
        result.MostPlays.Should().BeNull();
        result.BestWinRate.Should().BeNull();
        result.MostTimePlayed.Should().BeNull();
        result.ShowPodium.Should().BeFalse();
        result.RankedCount.Should().Be(0);
        VerifyRowsLoaded(filter);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldRoundMinutes_AndIgnoreWinlessPlayersForTheWinCards()
    {
        var filter = new LeaderboardFilter(MinSessions: 1);
        SetupRows(filter, [new LeaderboardRow(1, "Alice", null, 2, 0, 1, 90.6)]);

        var result = await _service.GetLeaderboardAsync(filter, TestContext.Current.CancellationToken);

        result.Players.Single().MinutesPlayed.Should().Be(91);
        result.MostWins.Should().BeNull();
        result.BestWinRate.Should().BeNull();
        result.MostPlays!.Name.Should().Be("Alice");
        result.MostTimePlayed!.Name.Should().Be("Alice");
        VerifyRowsLoaded(filter);
    }
}
