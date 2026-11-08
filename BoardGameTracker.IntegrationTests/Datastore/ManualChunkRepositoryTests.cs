using System.Reflection;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Rag;
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
public class ManualChunkRepositoryTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;

    public ManualChunkRepositoryTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SearchAsync_ShouldOnlyReturnChunksOfTheRequestedManual()
    {
        var token = TestContext.Current.CancellationToken;
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IManualChunkRepository>();
        var game = new Game("Two manual game");
        db.Games.Add(game);
        await db.SaveChangesAsync(token);
        var rules = new Manual("Rules", "rules.pdf", "application/pdf", 10, game.Id, DateTime.UtcNow);
        var faq = new Manual("FAQ", "faq.pdf", "application/pdf", 10, game.Id, DateTime.UtcNow);
        db.Manuals.AddRange(rules, faq);
        await db.SaveChangesAsync(token);
        db.ManualChunks.AddRange(
            new ManualChunk(faq.Id, game.Id, 0, "faq exact", null, Embedding(1, 1f)),
            new ManualChunk(rules.Id, game.Id, 0, "rules near", null, Embedding(1, 0.9f)),
            new ManualChunk(rules.Id, game.Id, 1, "rules far", null, Embedding(1, 0.2f)));
        await db.SaveChangesAsync(token);

        var matches = await repository.SearchAsync(new NearestManualChunksSpec(game.Id, Embedding(1, 1f), 5, rules.Id), token);

        matches.Select(m => m.Chunk.Content).Should().Equal("rules near", "rules far");
        matches.Select(m => m.ManualTitle).Should().AllBe("Rules");
    }

    [Fact]
    public async Task SearchAsync_ShouldStillFillTheLimit_WhenTheIndexScanStartsWithRowsTheFilterExcludes()
    {
        var token = TestContext.Current.CancellationToken;
        ResetPgvectorDetection();
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IManualChunkRepository>();
        var game = new Game("Sparse game");
        var crowdedGame = new Game("Crowded game");
        db.Games.AddRange(game, crowdedGame);
        await db.SaveChangesAsync(token);
        var manual = new Manual("Sparse rules", "sparse.pdf", "application/pdf", 10, game.Id, DateTime.UtcNow);
        var crowdedManual = new Manual("Crowded rules", "crowded.pdf", "application/pdf", 10, crowdedGame.Id, DateTime.UtcNow);
        db.Manuals.AddRange(manual, crowdedManual);
        await db.SaveChangesAsync(token);
        db.ManualChunks.AddRange(Enumerable.Range(0, 200)
            .Select(i => new ManualChunk(crowdedManual.Id, crowdedGame.Id, i, $"crowded {i}", null, Embedding(1, 1f - i / 1000f))));
        db.ManualChunks.AddRange(Enumerable.Range(0, 3)
            .Select(i => new ManualChunk(manual.Id, game.Id, i, $"sparse {i}", null, Embedding(1, 0.3f - i / 10f))));
        await db.SaveChangesAsync(token);
        await db.Database.OpenConnectionAsync(token);
        await db.Database.ExecuteSqlRawAsync("SET enable_seqscan = off; SET enable_sort = off", token);

        var matches = await repository.SearchAsync(new NearestManualChunksSpec(game.Id, Embedding(1, 1f), 3), token);

        matches.Select(m => m.Chunk.Content).Should().BeEquivalentTo("sparse 0", "sparse 1", "sparse 2");
    }

    [Fact]
    public async Task DeleteByManualAsync_ShouldOnlyRemoveTheChunksOfThatManual()
    {
        var token = TestContext.Current.CancellationToken;
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IManualChunkRepository>();
        var game = new Game("Delete game");
        db.Games.Add(game);
        await db.SaveChangesAsync(token);
        var removed = new Manual("Removed", "removed.pdf", "application/pdf", 10, game.Id, DateTime.UtcNow);
        var kept = new Manual("Kept", "kept.pdf", "application/pdf", 10, game.Id, DateTime.UtcNow);
        db.Manuals.AddRange(removed, kept);
        await db.SaveChangesAsync(token);
        db.ManualChunks.AddRange(
            new ManualChunk(removed.Id, game.Id, 0, "removed 0", null, Embedding(0, 1f)),
            new ManualChunk(removed.Id, game.Id, 1, "removed 1", null, Embedding(0, 1f)),
            new ManualChunk(kept.Id, game.Id, 0, "kept 0", null, Embedding(0, 1f)));
        await db.SaveChangesAsync(token);

        await repository.DeleteByManualAsync(removed.Id);

        (await db.ManualChunks.AsNoTracking().Select(c => c.Content).ToListAsync(token)).Should().Equal("kept 0");
        (await db.Manuals.CountAsync(token)).Should().Be(2);
    }

    private static void ResetPgvectorDetection()
    {
        foreach (var name in new[] { "_versionChecked", "_iterativeScanUnsupported" })
        {
            typeof(ManualChunkRepository).GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, false);
        }
    }

    private static Vector Embedding(int axis, float weight)
    {
        var values = new float[1024];
        values[axis] = weight;
        values[axis == 0 ? 1 : 0] = MathF.Sqrt(1f - weight * weight);
        return new Vector(values);
    }
}
