using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Locations;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Locations;

public class LocationRepositoryTests
{
    private static MainDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new MainDbContext(options);
    }

    private static async Task<int> SeedAsync(string databaseName)
    {
        await using var context = CreateContext(databaseName);
        var home = new Location("Home");
        var office = new Location("Office");
        context.Locations.AddRange(home, office);
        await context.SaveChangesAsync();

        context.Sessions.AddRange(
            new SessionBuilder().ForGame(1).AtLocation(home).LastingMinutes(30).WithWinner(1).WithPlayer(2).Build(),
            new SessionBuilder().ForGame(1).AtLocation(home).LastingMinutes(45).WithWinner(2).WithPlayer(3).Build(),
            new SessionBuilder().ForGame(2).AtLocation(home).LastingMinutes(60).WithWinner(1).Build(),
            new SessionBuilder().ForGame(3).AtLocation(office).LastingMinutes(90).WithWinner(4).Build());
        await context.SaveChangesAsync();

        return home.Id;
    }

    private static LocationRepository CreateRepository(MainDbContext context)
    {
        return new LocationRepository(context);
    }

    [Fact]
    public async Task Aggregates_ShouldOnlyCountSessionsAtTheLocation()
    {
        var databaseName = Guid.NewGuid().ToString();
        var homeId = await SeedAsync(databaseName);
        await using var context = CreateContext(databaseName);
        var repository = CreateRepository(context);

        (await repository.GetDistinctGameCount(homeId)).Should().Be(2);
        (await repository.GetDistinctPlayerCount(homeId)).Should().Be(3);
        (await repository.GetPlayLengthInMinutes(homeId)).Should().BeApproximately(135, 0.01);
    }

    [Fact]
    public async Task Aggregates_ShouldBeZero_WhenLocationHasNoSessions()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedAsync(databaseName);
        await using var context = CreateContext(databaseName);
        var repository = CreateRepository(context);

        (await repository.GetDistinctGameCount(999)).Should().Be(0);
        (await repository.GetDistinctPlayerCount(999)).Should().Be(0);
        (await repository.GetPlayLengthInMinutes(999)).Should().Be(0);
    }
}
