using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Datastore;

public class UnitOfWorkTests
{
    private static MainDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MainDbContext(options);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistPendingChanges_AndReturnTheRowCount()
    {
        await using var context = CreateContext();
        context.Players.AddRange(new Player("Alice"), new Player("Bob"));

        var result = await new UnitOfWork(context).SaveChangesAsync();

        result.Should().Be(2);
        (await context.Players.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldForwardTheCancellationToken()
    {
        await using var context = CreateContext();
        context.Players.Add(new Player("Alice"));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = async () => await new UnitOfWork(context).SaveChangesAsync(cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        (await context.Players.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DiscardPendingInserts_ShouldDetachOnlyAddedEntries()
    {
        await using var context = CreateContext();
        var kept = new Player("Kept");
        var changed = new Player("Changed");
        var removed = new Player("Removed");
        context.Players.AddRange(kept, changed, removed);
        await context.SaveChangesAsync();
        changed.UpdateName("Renamed");
        context.Players.Remove(removed);
        var pendingPlayer = new Player("Pending");
        var pendingGame = new Game("Pending game");
        context.AddRange(pendingPlayer, pendingGame);

        new UnitOfWork(context).DiscardPendingInserts();

        context.Entry(pendingPlayer).State.Should().Be(EntityState.Detached);
        context.Entry(pendingGame).State.Should().Be(EntityState.Detached);
        context.Entry(kept).State.Should().Be(EntityState.Unchanged);
        context.Entry(changed).State.Should().Be(EntityState.Modified);
        context.Entry(removed).State.Should().Be(EntityState.Deleted);

        await context.SaveChangesAsync();

        (await context.Players.OrderBy(x => x.Name).Select(x => x.Name).ToListAsync()).Should().Equal("Kept", "Renamed");
        (await context.Games.CountAsync()).Should().Be(0);
    }
}
