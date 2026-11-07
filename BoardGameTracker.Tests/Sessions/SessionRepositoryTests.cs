using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Sessions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Sessions;

public class SessionRepositoryTests : IDisposable
{
    private static readonly DateTime Monday = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);

    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly MainDbContext _context;
    private readonly SessionRepository _repository;

    public SessionRepositoryTests()
    {
        _context = CreateContext();
        _repository = new SessionRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private MainDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;
        return new MainDbContext(options);
    }

    private async Task<T[]> SeedAsync<T>(params T[] entities) where T : class
    {
        await using var context = CreateContext();
        context.Set<T>().AddRange(entities);
        await context.SaveChangesAsync();
        return entities;
    }

    private static Session Play(Game game, DateTime start, int minutes, params Player[] players)
    {
        var session = new Session(game.Id, start, start.AddMinutes(minutes), string.Empty);
        foreach (var player in players)
        {
            session.AddPlayerSession(player.Id, null, false, false);
        }

        return session;
    }

    [Fact]
    public async Task PlayTimes_ShouldUseEverySession()
    {
        var game = (await SeedAsync(new Game("Game")))[0];
        await SeedAsync(Play(game, Monday, 30), Play(game, Monday.AddDays(1), 60), Play(game, Monday.AddDays(2), 120));

        (await _repository.GetTotalPlayTime()).Should().BeApproximately(210, 0.001);
        (await _repository.GetMeanPlayTime()).Should().BeApproximately(70, 0.001);
    }

    [Fact]
    public async Task PlayTimes_ShouldBeZero_WithoutSessions()
    {
        (await _repository.GetTotalPlayTime()).Should().Be(0);
        (await _repository.GetMeanPlayTime()).Should().Be(0);
    }

    [Fact]
    public async Task GetByPlayerBatchAsync_ShouldGroupSessionsPerRequestedPlayer_AndKeepPlayersWithoutSessions()
    {
        var game = (await SeedAsync(new Game("Game")))[0];
        var players = await SeedAsync(new Player("Alice"), new Player("Bob"), new Player("Cara"), new Player("Dave"));
        var (alice, bob, cara, dave) = (players[0], players[1], players[2], players[3]);
        var sessions = await SeedAsync(
            Play(game, Monday, 30, alice, bob, cara),
            Play(game, Monday.AddDays(1), 30, alice),
            Play(game, Monday.AddDays(2), 30, cara));

        var result = await _repository.GetByPlayerBatchAsync([alice.Id, bob.Id, dave.Id]);

        result.Keys.Should().BeEquivalentTo(new[] { alice.Id, bob.Id, dave.Id });
        result[alice.Id].Select(x => x.Id).Should().BeEquivalentTo(new[] { sessions[0].Id, sessions[1].Id });
        result[bob.Id].Select(x => x.Id).Should().Equal(sessions[0].Id);
        result[dave.Id].Should().BeEmpty();
        result[bob.Id][0].PlayerSessions.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTheSessionWithPlayersAndExpansions()
    {
        var game = (await SeedAsync(new Game("Game")))[0];
        var expansion = (await SeedAsync(new Expansion("Expansion", null, game.Id)))[0];
        var alice = (await SeedAsync(new Player("Alice")))[0];
        var sessions = await SeedAsync(Play(game, Monday, 30, alice), Play(game, Monday.AddDays(1), 45));
        await using (var context = CreateContext())
        {
            var session = await context.Sessions.Include(x => x.Expansions).SingleAsync(x => x.Id == sessions[0].Id);
            session.AddExpansion(await context.Expansions.SingleAsync());
            await context.SaveChangesAsync();
        }

        var result = await _repository.GetByIdAsync(sessions[0].Id);

        result!.Id.Should().Be(sessions[0].Id);
        result.PlayerSessions.Select(x => x.PlayerId).Should().Equal(alice.Id);
        result.Expansions.Select(x => x.Id).Should().Equal(expansion.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenTheSessionDoesNotExist()
    {
        var game = (await SeedAsync(new Game("Game")))[0];
        var session = (await SeedAsync(Play(game, Monday, 30)))[0];

        var result = await _repository.GetByIdAsync(session.Id + 1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentSessions_ShouldReturnTheNewestSessionsWithGameAndPlayers()
    {
        var game = (await SeedAsync(new Game("Game")))[0];
        var alice = (await SeedAsync(new Player("Alice")))[0];
        var sessions = await SeedAsync(
            Play(game, Monday, 30, alice),
            Play(game, Monday.AddDays(2), 30, alice),
            Play(game, Monday.AddDays(1), 30, alice));

        var result = await _repository.GetRecentSessions(2);

        result.Select(x => x.Id).Should().Equal(sessions[1].Id, sessions[2].Id);
        result[0].Game.Title.Should().Be("Game");
        result[0].PlayerSessions.Single().Player.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task DeleteByPlayerIdAsync_ShouldOnlyRemoveThePlayersSessions_OnceSaved()
    {
        var game = (await SeedAsync(new Game("Game")))[0];
        var players = await SeedAsync(new Player("Alice"), new Player("Bob"));
        var sessions = await SeedAsync(
            Play(game, Monday, 30, players[0]),
            Play(game, Monday.AddDays(1), 30, players[0], players[1]),
            Play(game, Monday.AddDays(2), 30, players[1]));

        await _repository.DeleteByPlayerIdAsync(players[0].Id);

        _context.ChangeTracker.Entries<Session>().Where(x => x.State == EntityState.Deleted).Select(x => x.Entity.Id)
            .Should().BeEquivalentTo(new[] { sessions[0].Id, sessions[1].Id });
        (await _context.Sessions.CountAsync()).Should().Be(3);

        await _context.SaveChangesAsync();

        await using var verify = CreateContext();
        (await verify.Sessions.Select(x => x.Id).ToListAsync()).Should().Equal(sessions[2].Id);
    }
}
