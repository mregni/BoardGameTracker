using System;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Sessions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Sessions;

public class SessionRepositorySuggestionsTests
{
    private static MainDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new MainDbContext(options);
    }

    private static Session MakeSession(int gameId, int day, params int[] playerIds)
    {
        var start = new DateTime(2026, 1, day, 20, 0, 0, DateTimeKind.Utc);
        var session = new Session(gameId, start, start.AddHours(1), string.Empty);
        foreach (var playerId in playerIds)
        {
            session.AddPlayerSession(playerId, null, false, false);
        }

        return session;
    }

    private static async Task<string> SeedAsync(params Session[] sessions)
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var seed = CreateContext(databaseName);
        seed.Sessions.AddRange(sessions);
        await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        return databaseName;
    }

    [Fact]
    public async Task GetRecentlyPlayedGameIds_ShouldReturnDistinctGamesByLatestSessionNewestFirst()
    {
        var databaseName = await SeedAsync(
            MakeSession(1, 1, 10),
            MakeSession(2, 2, 10),
            MakeSession(1, 5, 10),
            MakeSession(3, 3, 10),
            MakeSession(4, 4, 10));

        await using var context = CreateContext(databaseName);
        var repository = new SessionRepository(context);

        var result = await repository.GetRecentlyPlayedGameIds(3, TestContext.Current.CancellationToken);

        result.Should().Equal(1, 4, 3);
    }

    [Fact]
    public async Task GetRecentlyPlayedGameIds_ShouldReturnEmpty_WhenNoSessionsExist()
    {
        var databaseName = await SeedAsync();

        await using var context = CreateContext(databaseName);
        var repository = new SessionRepository(context);

        var result = await repository.GetRecentlyPlayedGameIds(5, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }
}
