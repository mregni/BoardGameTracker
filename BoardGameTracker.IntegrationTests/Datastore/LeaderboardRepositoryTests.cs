using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
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
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetLeaderboardRows_ShouldAggregatePlaysWinsMinutesAndPodiums_PerPlayer()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

        var rows = await repository.GetLeaderboardRows();

        rows.Should().HaveCount(4);
        var alice = rows.Single(x => x.PlayerId == _aliceId);
        alice.Should().BeEquivalentTo(new { PlayCount = 3, WinCount = 2, MinutesPlayed = 180.0, PodiumCount = 2 });
        var bob = rows.Single(x => x.PlayerId == _bobId);
        bob.Should().BeEquivalentTo(new { PlayCount = 2, WinCount = 1, MinutesPlayed = 150.0, PodiumCount = 2 });
        var cara = rows.Single(x => x.PlayerId == _caraId);
        cara.Should().BeEquivalentTo(new { PlayCount = 2, WinCount = 1, MinutesPlayed = 90.0, PodiumCount = 1 });
        rows.Single(x => x.Name == "Dave").PodiumCount.Should().Be(0);
    }
}
