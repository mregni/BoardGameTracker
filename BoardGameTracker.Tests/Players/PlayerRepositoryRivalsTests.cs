using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Players;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Players;

public class PlayerRepositoryRivalsTests : IDisposable
{
    private readonly MainDbContext _context;
    private readonly PlayerRepository _repository;

    public PlayerRepositoryRivalsTests()
    {
        _context = new MainDbContext(new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        _repository = new PlayerRepository(
            _context,
            Mock.Of<IReadRepository<Session>>(),
            Mock.Of<IReadRepository<PlayerSession>>(),
            Mock.Of<IDateTimeProvider>());
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetRivals_ShouldCountHeadToHeadResults_AndSkipCoopSessions()
    {
        var game = new Game("Catan", true, GameState.Owned);
        var alice = new Player("Alice");
        var bob = new Player("Bob");
        var carol = new Player("Carol");
        _context.AddRange(game, alice, bob, carol);
        await _context.SaveChangesAsync();

        var start = new DateTime(2026, 3, 2, 20, 0, 0, DateTimeKind.Utc);
        var aliceWins = new Session(game.Id, start, start.AddMinutes(30), string.Empty);
        aliceWins.AddPlayerSession(alice.Id, 10, false, true);
        aliceWins.AddPlayerSession(bob.Id, 5, false, false);
        aliceWins.AddPlayerSession(carol.Id, 3, false, false);
        var bobWins = new Session(game.Id, start.AddDays(1), start.AddDays(1).AddMinutes(30), string.Empty);
        bobWins.AddPlayerSession(alice.Id, 4, false, false);
        bobWins.AddPlayerSession(bob.Id, 9, false, true);
        var carolWins = new Session(game.Id, start.AddDays(2), start.AddDays(2).AddMinutes(30), string.Empty);
        carolWins.AddPlayerSession(alice.Id, 2, false, false);
        carolWins.AddPlayerSession(bob.Id, 1, false, false);
        carolWins.AddPlayerSession(carol.Id, 8, false, true);
        var coop = new Session(game.Id, start.AddDays(3), start.AddDays(3).AddMinutes(30), string.Empty);
        coop.MarkAsCoop(false, null);
        coop.AddPlayerSession(alice.Id, null, false, false);
        coop.AddPlayerSession(bob.Id, null, false, false);
        _context.AddRange(aliceWins, bobWins, carolWins, coop);
        await _context.SaveChangesAsync();

        var rivals = await _repository.GetRivals(alice.Id);

        rivals.Should().BeEquivalentTo(
            [
                new { PlayerId = bob.Id, Sessions = 3, Wins = 1, Losses = 1 },
                new { PlayerId = carol.Id, Sessions = 2, Wins = 1, Losses = 1 }
            ],
            options => options.WithStrictOrdering());
    }
}
