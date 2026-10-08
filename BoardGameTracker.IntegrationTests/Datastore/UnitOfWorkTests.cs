using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pgvector;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class UnitOfWorkTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;

    public UnitOfWorkTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DiscardPendingInserts_ShouldLetAFailedChunkInsertBeFollowedByRecordingTheFailure()
    {
        int manualId;
        await using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var game = new Game("Rulebook game", false, GameState.Owned);
            db.Games.Add(game);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            var manual = new Manual("Rules", "rules.pdf", "application/pdf", 100, game.Id, DateTime.UtcNow);
            db.Manuals.Add(manual);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            manualId = manual.Id;
        }

        await using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var manual = await db.Manuals.SingleAsync(m => m.Id == manualId, TestContext.Current.CancellationToken);
            manual.MarkIndexing();
            await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

            db.Add(new ManualChunk(manualId, manual.GameId, 0, "broken\0text", 1, new Vector(new float[1024])));
            var insert = () => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
            await insert.Should().ThrowAsync<DbUpdateException>();

            unitOfWork.DiscardPendingInserts();
            manual.MarkFailed("could not store the chunks");
            await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var stored = await db.Manuals.AsNoTracking().SingleAsync(m => m.Id == manualId, TestContext.Current.CancellationToken);
            stored.IndexStatus.Should().Be(ManualIndexStatus.Failed);
            (await db.Set<ManualChunk>().CountAsync(c => c.ManualId == manualId, TestContext.Current.CancellationToken)).Should().Be(0);
        }
    }
}
