using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class CompareTests : IAsyncLifetime
{
    private static readonly DateTime Day = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);
    private readonly IntegrationFixture _fixture;
    private int _aliceId;
    private int _bobId;
    private int _caraId;
    private int _azulId;
    private int _brassId;

    public CompareTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDataAsync();
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var azul = new Game("Azul", true, GameState.Owned);
        var brass = new Game("Brass", false, GameState.Owned);
        var alice = new Player("Alice");
        var bob = new Player("Bob");
        var cara = new Player("Cara");
        db.AddRange(azul, brass, alice, bob, cara);
        await db.SaveChangesAsync();

        Session Add(Game game, int day, int minutes, (Player Player, double? Score, bool Won)[] seats)
        {
            var session = new Session(game.Id, Day.AddDays(day), Day.AddDays(day).AddMinutes(minutes), string.Empty);
            foreach (var seat in seats)
            {
                session.AddPlayerSession(seat.Player.Id, seat.Score, false, seat.Won);
            }

            db.Add(session);
            return session;
        }

        Add(azul, 0, 30, [(alice, 70, true), (bob, 61, false)]);
        Add(azul, 1, 25, [(alice, 71, false), (bob, 72, true)]);
        Add(azul, 2, 40, [(alice, 80, true), (bob, 75, false)]);
        Add(azul, 3, 35, [(alice, 50, true), (bob, 50, true)]);
        Add(brass, 4, 170, [(alice, null, false), (bob, null, false), (cara, null, true)]);
        Add(brass, 5, 60, [(alice, null, true), (cara, null, false)]);
        await db.SaveChangesAsync();

        _aliceId = alice.Id;
        _bobId = bob.Id;
        _caraId = cara.Id;
        _azulId = azul.Id;
        _brassId = brass.Id;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Compare_ShouldReturnTheRecordPerGameBreakdownAndRecentSessions()
    {
        using var reader = await _fixture.CreateClientAsAsync("reader");

        var result = await reader.GetFromJsonAsync<CompareResultDto>($"/api/compare/{_aliceId}/{_bobId}", IntegrationFixture.Json);

        result!.TotalSessionsTogether.Should().Be(5);
        result.DirectWins.PlayerOne.Should().Be(3);
        result.DirectWins.PlayerTwo.Should().Be(2);
        result.WonByOthers.Should().Be(1);
        result.MinutesPlayed.Should().Be(300);
        result.SessionCounts.PlayerOne.Should().Be(6);
        result.WinCount.PlayerOne.Should().Be(4);
        result.Games.Should().HaveCount(2);
        result.Games[0].Should().BeEquivalentTo(new CompareGame
        {
            GameId = _azulId,
            SessionCount = 4,
            PlayerOneWins = 3,
            PlayerTwoWins = 2,
            OtherWins = 0,
            PlayerOneAverageScore = 67.8,
            PlayerTwoAverageScore = 64.5,
            ScoredSessionCount = 4,
            LastPlayed = Day.AddDays(3),
            LastOutcome = CompareOutcome.Both
        });
        result.Games[1].Should().BeEquivalentTo(new { GameId = _brassId, SessionCount = 1, OtherWins = 1, PlayerOneAverageScore = (double?)null, LastOutcome = CompareOutcome.Other });
        result.RecentSessions.Select(s => s.Outcome).Should().Equal(
            CompareOutcome.Other, CompareOutcome.Both, CompareOutcome.PlayerOne, CompareOutcome.PlayerTwo, CompareOutcome.PlayerOne);
        result.RecentSessions[0].OtherWinnerId.Should().Be(_caraId);
        result.RecentSessions[1].Should().BeEquivalentTo(new { PlayerOneScore = 50.0, PlayerTwoScore = 50.0, Minutes = 35 });
        result.BestGames.PlayerOne.Should().Equal(_azulId);
        result.BestGames.PlayerTwo.Should().BeEmpty();
        result.ClosestGame.Should().BeEquivalentTo(new ClosestGame { GameId = _azulId, ScoringDifference = 0, PlayerId = _aliceId });
        result.LongestSessionTogether.Should().BeEquivalentTo(new LongestSession { GameId = _brassId, Minutes = 170 });
        result.FirstGameTogether.Should().BeEquivalentTo(new FirstGameTogether { GameId = _azulId, StartDate = Day });
        result.PreferredGame.Should().BeEquivalentTo(new PreferredGame { GameId = _azulId, SessionCount = 4 });
        result.Streak.Should().BeNull();
    }

    [Fact]
    public async Task Compare_ShouldCountCoopSessionsAsSameTeam()
    {
        await using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var coop = new Session(_brassId, Day.AddDays(6), Day.AddDays(6).AddMinutes(50), string.Empty);
            coop.MarkAsCoop(true, 40);
            coop.AddPlayerSession(_aliceId, null, false, true);
            coop.AddPlayerSession(_bobId, null, false, true);
            db.Add(coop);
            await db.SaveChangesAsync();
        }

        using var reader = await _fixture.CreateClientAsAsync("reader");
        var result = await reader.GetFromJsonAsync<CompareResultDto>($"/api/compare/{_aliceId}/{_bobId}", IntegrationFixture.Json);

        result!.TotalSessionsTogether.Should().Be(6);
        result.DirectWins.PlayerOne.Should().Be(3);
        result.DirectWins.PlayerTwo.Should().Be(2);
        result.WonByOthers.Should().Be(1);
        result.SameTeam.Should().BeEquivalentTo(new CompareTeamRecord { Count = 1, Won = 1, Lost = 0 });
        result.RecentSessions[0].Should().BeEquivalentTo(new { Outcome = CompareOutcome.SameTeam, TeamWon = (bool?)true });
        result.Games.Single(g => g.GameId == _brassId).Should().BeEquivalentTo(new { SameTeamCount = 1, SameTeamWins = 1, LastOutcome = CompareOutcome.SameTeam, LastTeamWon = (bool?)true });
    }

    [Fact]
    public async Task DefaultPair_ShouldBeThePlayersWithTheMostGamesTogether()
    {
        using var reader = await _fixture.CreateClientAsAsync("reader");

        var pair = await reader.GetFromJsonAsync<ComparePairDto>("/api/compare/default-pair", IntegrationFixture.Json);

        pair.Should().BeEquivalentTo(new ComparePairDto { PlayerOneId = _aliceId, PlayerTwoId = _bobId, SessionCount = 5 });
    }

    [Fact]
    public async Task DefaultPair_ShouldBeNoContent_WhenNobodyPlayedTogether()
    {
        await _fixture.ResetDataAsync();
        using var reader = await _fixture.CreateClientAsAsync("reader");

        var response = await reader.GetAsync("/api/compare/default-pair");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Compare_ShouldRejectTheSamePlayerOnBothSides()
    {
        using var reader = await _fixture.CreateClientAsAsync("reader");

        var response = await reader.GetAsync($"/api/compare/{_aliceId}/{_aliceId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ProblemDetails>(IntegrationFixture.Json))!.Title.Should().Be(Constants.Errors.CompareSamePlayer);
    }

    [Fact]
    public async Task Compare_ShouldRequireSignIn()
    {
        using var anonymous = _fixture.CreateClient();

        var response = await anonymous.GetAsync($"/api/compare/{_aliceId}/{_bobId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
