using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class LeaderboardRepositoryTests : IAsyncLifetime
{
    private static readonly DateTime Monday = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);
    private readonly IntegrationFixture _fixture;
    private int _aliceId;
    private int _bobId;
    private int _caraId;
    private int _daveId;
    private int _scoringId;

    public LeaderboardRepositoryTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDataAsync();
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var scoring = new Game("Scoring game", true, GameState.Owned);
        var coop = new Game("Co-op game", false, GameState.Owned);
        var alice = new Player("Alice");
        var bob = new Player("Bob");
        var cara = new Player("Cara");
        var dave = new Player("Dave");
        db.AddRange(scoring, coop, alice, bob, cara, dave);
        await db.SaveChangesAsync();

        var first = new Session(scoring.Id, Monday, Monday.AddMinutes(60), string.Empty);
        first.AddPlayerSession(alice.Id, 50, true, true);
        first.AddPlayerSession(bob.Id, 30, true, false);
        first.AddPlayerSession(cara.Id, 20, true, false);
        first.AddPlayerSession(dave.Id, 10, true, false);
        var second = new Session(scoring.Id, Monday.AddDays(1), Monday.AddDays(1).AddMinutes(90), string.Empty);
        second.AddPlayerSession(alice.Id, 20, false, false);
        second.AddPlayerSession(bob.Id, 70, false, true);
        var third = new Session(coop.Id, Monday.AddDays(2), Monday.AddDays(2).AddMinutes(30), string.Empty);
        third.AddPlayerSession(alice.Id, 99, false, true);
        third.AddPlayerSession(cara.Id, 1, false, true);
        db.AddRange(first, second, third);
        await db.SaveChangesAsync();

        _aliceId = alice.Id;
        _bobId = bob.Id;
        _caraId = cara.Id;
        _daveId = dave.Id;
        _scoringId = scoring.Id;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetLeaderboardRows_ShouldAggregatePlaysWinsMinutesAndPodiums_PerPlayer()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

        var rows = await repository.GetLeaderboardRows(new LeaderboardFilter(), TestContext.Current.CancellationToken);

        rows.Should().HaveCount(4);
        var alice = rows.Single(x => x.PlayerId == _aliceId);
        alice.Should().BeEquivalentTo(new { PlayCount = 3, WinCount = 2, MinutesPlayed = 180.0, PodiumCount = 1 });
        var bob = rows.Single(x => x.PlayerId == _bobId);
        bob.Should().BeEquivalentTo(new { PlayCount = 2, WinCount = 1, MinutesPlayed = 150.0, PodiumCount = 1 });
        var cara = rows.Single(x => x.PlayerId == _caraId);
        cara.Should().BeEquivalentTo(new { PlayCount = 2, WinCount = 1, MinutesPlayed = 90.0, PodiumCount = 1 });
        rows.Single(x => x.Name == "Dave").PodiumCount.Should().Be(0);
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldRankLowestScoreWinsGames_AndLetTiesShareAPlace()
    {
        await using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var golf = new Session(_scoringId, Monday.AddDays(3), Monday.AddDays(3).AddMinutes(45), string.Empty);
            golf.AddPlayerSession(_aliceId, 3, false, true);
            golf.AddPlayerSession(_bobId, 8, false, false);
            golf.AddPlayerSession(_caraId, 12, false, false);
            golf.AddPlayerSession(_daveId, 15, false, false);
            var tie = new Session(_scoringId, Monday.AddDays(4), Monday.AddDays(4).AddMinutes(45), string.Empty);
            tie.AddPlayerSession(_aliceId, 10, false, true);
            tie.AddPlayerSession(_bobId, 10, false, true);
            tie.AddPlayerSession(_caraId, 10, false, true);
            tie.AddPlayerSession(_daveId, 10, false, true);
            db.AddRange(golf, tie);
            await db.SaveChangesAsync();
        }

        await using var readScope = _fixture.CreateScope();
        var rows = await readScope.ServiceProvider.GetRequiredService<IPlayerRepository>().GetLeaderboardRows(new LeaderboardFilter(), TestContext.Current.CancellationToken);

        rows.Single(x => x.PlayerId == _aliceId).PodiumCount.Should().Be(3);
        rows.Single(x => x.PlayerId == _bobId).PodiumCount.Should().Be(3);
        rows.Single(x => x.PlayerId == _caraId).PodiumCount.Should().Be(3);
        rows.Single(x => x.PlayerId == _daveId).PodiumCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldCountCoopWins_AndNeverGivePodiumsForCoopSessions()
    {
        await using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var coop = new Session(_scoringId, Monday.AddDays(5), Monday.AddDays(5).AddMinutes(40), string.Empty);
            coop.AddPlayerSession(_aliceId, 40, false, false);
            coop.AddPlayerSession(_bobId, 30, false, false);
            coop.AddPlayerSession(_caraId, 20, false, false);
            coop.AddPlayerSession(_daveId, 10, false, false);
            coop.MarkAsCoop(true, 100);
            db.Add(coop);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readScope = _fixture.CreateScope();
        var rows = await readScope.ServiceProvider.GetRequiredService<IPlayerRepository>().GetLeaderboardRows(new LeaderboardFilter(), TestContext.Current.CancellationToken);

        rows.Single(x => x.PlayerId == _aliceId).Should().BeEquivalentTo(new { WinCount = 3, CoopWinCount = 1, PodiumCount = 1 });
        rows.Single(x => x.PlayerId == _daveId).Should().BeEquivalentTo(new { WinCount = 1, CoopWinCount = 1, PodiumCount = 0 });
        rows.Sum(x => x.CoopWinCount).Should().Be(4);
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldOnlyCountTheChosenGame()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

        var rows = await repository.GetLeaderboardRows(new LeaderboardFilter(GameId: _scoringId), TestContext.Current.CancellationToken);

        rows.Should().HaveCount(4);
        rows.Single(x => x.PlayerId == _aliceId).Should().BeEquivalentTo(new { PlayCount = 2, WinCount = 1, MinutesPlayed = 150.0, PodiumCount = 1 });
        rows.Single(x => x.PlayerId == _caraId).Should().BeEquivalentTo(new { PlayCount = 1, WinCount = 0, MinutesPlayed = 60.0, PodiumCount = 1 });
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldOnlyCountSessionsInsideThePeriod_IncludingBothEndDays()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
        var day = DateOnly.FromDateTime(Monday.AddDays(1));

        var rows = await repository.GetLeaderboardRows(new LeaderboardFilter(From: day, To: day), TestContext.Current.CancellationToken);

        rows.Should().HaveCount(2);
        rows.Single(x => x.PlayerId == _aliceId).Should().BeEquivalentTo(new { PlayCount = 1, WinCount = 0, MinutesPlayed = 90.0, PodiumCount = 0 });
        rows.Single(x => x.PlayerId == _bobId).Should().BeEquivalentTo(new { PlayCount = 1, WinCount = 1, MinutesPlayed = 90.0, PodiumCount = 0 });
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldCombineThePeriodAndTheGame()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
        var filter = new LeaderboardFilter(From: DateOnly.FromDateTime(Monday.AddDays(1)), GameId: _scoringId);

        var rows = await repository.GetLeaderboardRows(filter, TestContext.Current.CancellationToken);

        rows.Select(x => x.PlayerId).Should().BeEquivalentTo([_aliceId, _bobId]);
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldReturnNothing_WhenNoSessionMatches()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

        var rows = await repository.GetLeaderboardRows(new LeaderboardFilter(To: DateOnly.FromDateTime(Monday.AddDays(-1))), TestContext.Current.CancellationToken);

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task LeaderboardEndpoint_ShouldApplyTheFilters_AndRankAfterTheMinimum()
    {
        using var reader = await _fixture.CreateClientAsAsync("reader");

        var leaderboard = await reader.GetFromJsonAsync<LeaderboardDto>(
            $"/api/leaderboard?from=2026-03-01&to=2026-03-31&gameId={_scoringId}&minSessions=2&rankBy=winRate",
            IntegrationFixture.Json,
            TestContext.Current.CancellationToken);

        leaderboard!.Players.Select(x => (x.Rank, x.PlayerId, x.SessionsToRank)).Should().Equal(
            (1, _aliceId, 0),
            (1, _bobId, 0),
            (null, _caraId, 1),
            (null, _daveId, 1));
        leaderboard.RankBy.Should().Be(LeaderboardRankBy.WinRate);
        leaderboard.MinimumSessions.Should().Be(2);
        leaderboard.RankedCount.Should().Be(2);
        leaderboard.ShowPodium.Should().BeFalse();
    }

    [Theory]
    [InlineData("from=2026-05-01&to=2026-04-01")]
    [InlineData("minSessions=0")]
    [InlineData("gameId=0")]
    public async Task LeaderboardEndpoint_ShouldRejectInvalidFilters(string query)
    {
        using var reader = await _fixture.CreateClientAsAsync("reader");

        var response = await reader.GetAsync($"/api/leaderboard?{query}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
