using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Rag.Interfaces;
using BoardGameTracker.Core.Rag.Specifications;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pgvector;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class PostgresSemanticsTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;

    public PostgresSemanticsTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync() => new(_fixture.ResetDataAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SetConfigValue_ShouldUpsert_WithoutDuplicatingTheKey()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        const string key = "integration_test_key";

        await repository.SetConfigValueAsync(key, "first");
        await repository.SetConfigValueAsync(key, "second");
        var rows = await db.Config.AsNoTracking().Where(c => c.Key == key).ToListAsync();

        rows.Should().ContainSingle().Which.Value.Should().Be("second");
        (await repository.GetConfigValueAsync<string>(key)).Should().Be("second");
    }

    [Fact]
    public async Task GetOrCreateCategories_ShouldReuseExistingRows_AndDeduplicateTheBatch()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var first = await repository.GetOrCreateCategoriesAsync(["Strategy", "Party"]);
        await db.SaveChangesAsync();
        var second = await repository.GetOrCreateCategoriesAsync(["Party", "Party", "Family"]);
        await db.SaveChangesAsync();
        var names = await db.GameCategories.AsNoTracking().Select(c => c.Name).ToListAsync();

        first.Should().HaveCount(2);
        second.Select(c => c.Name).Should().BeEquivalentTo(["Party", "Family"]);
        names.Should().BeEquivalentTo(["Strategy", "Party", "Family"]);
    }

    [Fact]
    public async Task CategoryName_ShouldBeUnique_AtTheDatabaseLevel()
    {
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        db.GameCategories.Add(new GameCategory("Unique category"));
        await db.SaveChangesAsync();
        db.GameCategories.Add(new GameCategory("Unique category"));

        var act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task AwardBadgeToPlayer_ShouldInsertTheJoinRowOnce()
    {
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IBadgeRepository>();
        var player = new Player("Badge player");
        db.Players.Add(player);
        await db.SaveChangesAsync();
        var badgeId = await db.Badges.Select(b => b.Id).FirstAsync();

        var firstAward = await repository.AwardBadgeToPlayer(player.Id, badgeId);
        await db.SaveChangesAsync();
        var secondAward = await repository.AwardBadgeToPlayer(player.Id, badgeId);
        var awarded = await db.Badges.AsNoTracking().Where(b => b.Id == badgeId).SelectMany(b => b.Players).CountAsync(p => p.Id == player.Id);

        firstAward.Should().BeTrue();
        secondAward.Should().BeFalse();
        awarded.Should().Be(1);
    }

    [Fact]
    public async Task NearestManualChunks_ShouldOrderByCosineDistance_AndRespectTheFilters()
    {
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IManualChunkRepository>();
        var game = new Game("Rag game");
        var otherGame = new Game("Other rag game");
        db.AddRange(game, otherGame);
        await db.SaveChangesAsync();
        var manual = new Manual("Rules", "rules.pdf", "application/pdf", 10, game.Id, DateTime.UtcNow);
        var otherManual = new Manual("Other rules", "other.pdf", "application/pdf", 10, otherGame.Id, DateTime.UtcNow);
        db.AddRange(manual, otherManual);
        await db.SaveChangesAsync();
        db.AddRange(
            new ManualChunk(manual.Id, game.Id, 0, "far", null, UnitVector(0)),
            new ManualChunk(manual.Id, game.Id, 1, "near", null, UnitVector(1, 0.9f)),
            new ManualChunk(manual.Id, game.Id, 2, "exact", null, UnitVector(1)),
            new ManualChunk(otherManual.Id, otherGame.Id, 0, "other game exact", null, UnitVector(1)));
        await db.SaveChangesAsync();

        var matches = await repository.SearchAsync(new NearestManualChunksSpec(game.Id, UnitVector(1), 2));

        matches.Select(m => m.Chunk.Content).Should().ContainInOrder("exact", "near");
        matches.Should().HaveCount(2);
        matches[0].Distance.Should().BeApproximately(0, 0.0001);
        matches[0].ManualTitle.Should().Be("Rules");
    }

    [Fact]
    public async Task ClearUserData_ShouldDeleteAFullObjectGraph_InForeignKeyOrder()
    {
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var game = new Game("Graph game", true, GameState.Owned);
        var host = new Player("Host");
        var guest = new Player("Guest");
        var location = new Location("Kitchen table");
        db.AddRange(game, host, guest, location);
        await db.SaveChangesAsync();
        game.AddExpansion(new Expansion("Graph expansion", 4242, game.Id));
        var session = new Session(game.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1).AddHours(1), string.Empty);
        session.AddPlayerSession(host.Id, 10, true, true);
        session.AddPlayerSession(guest.Id, 5, true, false);
        var loan = new Loan(game.Id, guest.Id, DateTime.UtcNow.AddDays(-3));
        var gameNight = GameNight.Create("Graph night", string.Empty, DateTime.UtcNow.AddDays(3), host.Id, location.Id);
        gameNight.AddInvitedPlayers([guest.Id]);
        var manual = new Manual("Graph rules", "graph.pdf", "application/pdf", 10, game.Id, DateTime.UtcNow);
        db.AddRange(session, loan, gameNight, manual);
        await db.SaveChangesAsync();
        db.ManualChunks.Add(new ManualChunk(manual.Id, game.Id, 0, "chunk", 1, UnitVector(0)));
        await db.SaveChangesAsync();
        var badgeId = await db.Badges.Select(b => b.Id).FirstAsync();
        await scope.ServiceProvider.GetRequiredService<IBadgeRepository>().AwardBadgeToPlayer(host.Id, badgeId);
        await db.SaveChangesAsync();

        await _fixture.ResetDataAsync();

        (await db.Games.CountAsync()).Should().Be(0);
        (await db.Players.CountAsync()).Should().Be(0);
        (await db.Sessions.CountAsync()).Should().Be(0);
        (await db.Loans.CountAsync()).Should().Be(0);
        (await db.GameNights.CountAsync()).Should().Be(0);
        (await db.ManualChunks.CountAsync()).Should().Be(0);
        (await db.Locations.CountAsync()).Should().Be(0);
        (await db.Badges.CountAsync()).Should().BeGreaterThan(0);
        (await db.Users.CountAsync()).Should().BeGreaterThan(0);
    }

    private static Vector UnitVector(int axis, float weight = 1f)
    {
        var values = new float[1024];
        values[axis] = weight;
        if (weight < 1f)
        {
            values[axis == 0 ? 1 : 0] = MathF.Sqrt(1f - weight * weight);
        }

        return new Vector(values);
    }
}
