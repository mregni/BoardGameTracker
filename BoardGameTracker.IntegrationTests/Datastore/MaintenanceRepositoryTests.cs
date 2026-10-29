using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Maintenance.Interfaces;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class MaintenanceRepositoryTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;

    public MaintenanceRepositoryTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetChangeDetectionWatchIdsAsync_ShouldReturnOnlyTheWatchedGames()
    {
        var watchId = Guid.NewGuid().ToString();
        await using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var watched = new Game("Watched", false, GameState.Wanted);
            watched.UpdateChangeDetectionWatchId(watchId);
            db.Games.AddRange(watched, new Game("Unwatched", false, GameState.Owned));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var scope = _fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IMaintenanceRepository>();

            var ids = await repository.GetChangeDetectionWatchIdsAsync(TestContext.Current.CancellationToken);

            ids.Should().Equal(watchId);
        }
    }
}
