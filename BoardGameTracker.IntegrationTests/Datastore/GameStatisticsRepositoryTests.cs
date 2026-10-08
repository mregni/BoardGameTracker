using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class GameStatisticsRepositoryTests : IAsyncLifetime
{
    private static readonly DateTime Monday = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);
    private readonly IntegrationFixture _fixture;
    private int _ownedGameId;
    private int _aliceId;
    private int _bobId;

    public GameStatisticsRepositoryTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDataAsync();
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var owned = new Game("Owned game", true, GameState.Owned);
        owned.UpdateBuyingPrice(40m);
        var wanted = new Game("Wanted game", true, GameState.Wanted);
        wanted.UpdateBuyingPrice(100m);
        var sold = new Game("Sold game", true, GameState.PreviouslyOwned);
        sold.UpdateBuyingPrice(60m);
        var alice = new Player("Alice");
        var bob = new Player("Bob");
        db.AddRange(owned, wanted, sold, alice, bob);
        await db.SaveChangesAsync();

        var first = new Session(owned.Id, Monday, Monday.AddHours(1), string.Empty);
        first.AddPlayerSession(alice.Id, 50, true, true);
        first.AddPlayerSession(bob.Id, 30, true, false);
        var second = new Session(owned.Id, Monday.AddDays(2), Monday.AddDays(2).AddHours(2), string.Empty);
        second.AddPlayerSession(alice.Id, 20, false, false);
        second.AddPlayerSession(bob.Id, 70, false, true);
        var third = new Session(owned.Id, Monday.AddDays(7), Monday.AddDays(7).AddHours(1), string.Empty);
        third.AddPlayerSession(alice.Id, 90, false, true);
        db.AddRange(first, second, third);
        await db.SaveChangesAsync();

        _ownedGameId = owned.Id;
        _aliceId = alice.Id;
        _bobId = bob.Id;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task TotalAndMeanPayed_ShouldOnlyCountOwnedGames()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameStatisticsRepository>();

        var total = await repository.GetTotalPayedAsync();
        var mean = await repository.GetMeanPayedAsync();

        total.Should().Be(40m);
        mean.Should().Be(40m);
    }

    [Fact]
    public async Task PricePerPlay_ShouldDivideTheBuyingPriceByTheSessionCount()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameStatisticsRepository>();

        var pricePerPlay = await repository.GetPricePerPlay(_ownedGameId);

        pricePerPlay.Should().Be(13.33m);
    }

    [Fact]
    public async Task PlayerCountChart_ShouldGroupSessionsByNumberOfPlayers()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameStatisticsRepository>();

        var groups = await repository.GetPlayerCountChart(_ownedGameId);

        groups.ToDictionary(g => g.Key, g => g.Count()).Should().BeEquivalentTo(new Dictionary<int, int> { [2] = 2, [1] = 1 });
    }

    [Fact]
    public async Task HighestScoringPlayer_ShouldIgnoreNullScores_AndReturnTheTop()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameStatisticsRepository>();

        var highest = await repository.GetHighestScoringPlayer(_ownedGameId);

        highest!.PlayerId.Should().Be(_aliceId);
        highest.Score.Should().Be(90);
    }

    [Fact]
    public async Task SessionStartTimes_ShouldComeBackAsUtc()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameStatisticsRepository>();

        var starts = await repository.GetSessionStartTimes(_ownedGameId);

        starts.Should().HaveCount(3);
        starts.Should().OnlyContain(s => s.Kind == DateTimeKind.Utc);
        starts.Should().Contain(Monday);
    }

    [Fact]
    public async Task TopPlayers_ShouldAggregatePlaysAndWinsPerPlayer()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

        var top = await repository.GetTopPlayers(5);

        top.Should().HaveCount(2);
        top[0].Should().Be((_aliceId, "Alice", (string?)null, 3, 2));
        top[1].Should().Be((_bobId, "Bob", (string?)null, 2, 1));
    }
}
