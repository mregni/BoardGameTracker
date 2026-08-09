using System;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Players.Specifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Datastore;

// Regression coverage for bug C2: a name change through a tracked fetch must persist,
// while the previous AsNoTracking read path silently dropped the change.
public class PlayerUpdatePersistenceTests
{
    private static MainDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new MainDbContext(options);
    }

    private static async Task<int> SeedPlayerAsync(string databaseName)
    {
        await using var context = CreateContext(databaseName);
        var player = new Player("Original");
        context.Players.Add(player);
        await context.SaveChangesAsync();
        return player.Id;
    }

    [Fact]
    public async Task Update_ThroughForUpdateSpec_ShouldPersistNameChange()
    {
        var databaseName = Guid.NewGuid().ToString();
        var playerId = await SeedPlayerAsync(databaseName);

        await using (var context = CreateContext(databaseName))
        {
            var repository = new EfRepository<Player>(context);
            var player = await repository.SingleOrDefaultAsync(new PlayerByIdForUpdateSpec(playerId));
            player!.UpdateName("Updated");
            await context.SaveChangesAsync();
        }

        await using var verifyContext = CreateContext(databaseName);
        var persisted = await verifyContext.Players.SingleAsync();
        persisted.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task Update_ThroughNoTrackingReadSpec_ShouldNotPersist()
    {
        var databaseName = Guid.NewGuid().ToString();
        var playerId = await SeedPlayerAsync(databaseName);

        await using (var context = CreateContext(databaseName))
        {
            var repository = new EfRepository<Player>(context);
            var player = await repository.SingleOrDefaultAsync(new PlayerByIdWithBadgesSpec(playerId));
            player!.UpdateName("Updated");
            await context.SaveChangesAsync();
        }

        await using var verifyContext = CreateContext(databaseName);
        var persisted = await verifyContext.Players.SingleAsync();
        persisted.Name.Should().Be("Original");
    }
}
