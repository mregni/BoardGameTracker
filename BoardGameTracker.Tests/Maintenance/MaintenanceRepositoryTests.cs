using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Maintenance;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Maintenance;

public class MaintenanceRepositoryTests
{
    private const string FirstWatch = "3f2504e0-4f89-41d3-9a0c-0305e82c3301";
    private const string SecondWatch = "9a7b1c2d-1111-4222-8333-444455556666";

    [Fact]
    public async Task GetChangeDetectionWatchIdsAsync_ShouldOnlyReturnTheIdsOfWatchedGames()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new MainDbContext(options);
        var first = new Game("First");
        first.UpdateChangeDetectionWatchId(FirstWatch);
        var second = new Game("Second");
        second.UpdateChangeDetectionWatchId(SecondWatch);
        context.Games.AddRange(first, new Game("Unwatched"), second);
        await context.SaveChangesAsync();

        var result = await new MaintenanceRepository(context).GetChangeDetectionWatchIdsAsync();

        result.Should().BeEquivalentTo(FirstWatch, SecondWatch);
    }
}
