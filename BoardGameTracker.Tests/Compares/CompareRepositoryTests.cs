using System;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Compares;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Compares;

public class CompareRepositoryTests
{
    private static readonly DateTime Day = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);

    private static MainDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new MainDbContext(options);
    }

    private sealed record Seed(int Alice, int Bob, int Cara, int Dave, int Scoring, int NoScoring);

    private static async Task<Seed> SeedAsync(string databaseName)
    {
        await using var context = CreateContext(databaseName);
        var scoring = new Game("Scoring", true, GameState.Owned);
        var noScoring = new Game("No scoring", false, GameState.Owned);
        var alice = new Player("Alice");
        var bob = new Player("Bob");
        var cara = new Player("Cara");
        var dave = new Player("Dave");
        context.AddRange(scoring, noScoring, alice, bob, cara, dave);
        await context.SaveChangesAsync();

        var first = new Session(scoring.Id, Day, Day.AddMinutes(60), string.Empty);
        first.AddPlayerSession(alice.Id, 40, false, true);
        first.AddPlayerSession(bob.Id, 30, false, false);
        var second = new Session(noScoring.Id, Day.AddDays(1), Day.AddDays(1).AddMinutes(30), string.Empty);
        second.AddPlayerSession(alice.Id, null, false, false);
        second.AddPlayerSession(bob.Id, null, false, false);
        second.AddPlayerSession(cara.Id, null, false, true);
        second.AddPlayerSession(dave.Id, null, false, true);
        var third = new Session(scoring.Id, Day.AddDays(2), Day.AddDays(2).AddMinutes(90), string.Empty);
        third.AddPlayerSession(alice.Id, 50, false, true);
        third.AddPlayerSession(bob.Id, 50, false, true);
        var withoutBob = new Session(scoring.Id, Day.AddDays(3), Day.AddDays(3).AddMinutes(20), string.Empty);
        withoutBob.AddPlayerSession(alice.Id, 1, false, true);
        withoutBob.AddPlayerSession(cara.Id, 2, false, false);
        var caraAndDave = new Session(scoring.Id, Day.AddDays(4), Day.AddDays(4).AddMinutes(20), string.Empty);
        caraAndDave.AddPlayerSession(cara.Id, 1, false, true);
        caraAndDave.AddPlayerSession(dave.Id, 2, false, false);
        var coop = new Session(noScoring.Id, Day.AddDays(5), Day.AddDays(5).AddMinutes(45), string.Empty);
        coop.MarkAsCoop(false, 12);
        coop.AddPlayerSession(alice.Id, null, false, true);
        coop.AddPlayerSession(bob.Id, null, false, true);
        context.AddRange(first, second, third, withoutBob, caraAndDave, coop);
        await context.SaveChangesAsync();

        return new Seed(alice.Id, bob.Id, cara.Id, dave.Id, scoring.Id, noScoring.Id);
    }

    [Fact]
    public async Task GetSessionsTogether_ShouldReturnOnlySessionsWithBothPlayers_NewestFirst_WithBothScoresAndWins()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seed = await SeedAsync(databaseName);
        await using var context = CreateContext(databaseName);
        var repository = new CompareRepository(context);

        var sessions = await repository.GetSessionsTogether(seed.Alice, seed.Bob);

        sessions.Should().HaveCount(4);
        sessions.Select(s => s.Start).Should().Equal(Day.AddDays(5), Day.AddDays(2), Day.AddDays(1), Day);
        sessions[0].Should().BeEquivalentTo(new { IsCoop = true, TeamWon = (bool?)false, PlayerOneWon = false, PlayerTwoWon = false });
        sessions.Skip(1).Should().OnlyContain(s => !s.IsCoop && s.TeamWon == null);
        sessions[1].Should().BeEquivalentTo(new
        {
            GameId = seed.Scoring,
            HasScoring = true,
            End = Day.AddDays(2).AddMinutes(90),
            PlayerOneScore = 50.0,
            PlayerOneWon = true,
            PlayerTwoScore = 50.0,
            PlayerTwoWon = true,
            OtherWinnerId = (int?)null
        });
        sessions[2].Should().BeEquivalentTo(new
        {
            GameId = seed.NoScoring,
            HasScoring = false,
            PlayerOneScore = (double?)null,
            PlayerOneWon = false,
            PlayerTwoWon = false,
            OtherWinnerId = (int?)Math.Min(seed.Cara, seed.Dave)
        });
        sessions[3].Should().BeEquivalentTo(new { PlayerOneScore = 40.0, PlayerOneWon = true, PlayerTwoScore = 30.0, PlayerTwoWon = false });
    }

    [Fact]
    public async Task GetSessionsTogether_ShouldSwapTheSides_WhenThePlayersAreSwapped()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seed = await SeedAsync(databaseName);
        await using var context = CreateContext(databaseName);

        var sessions = await new CompareRepository(context).GetSessionsTogether(seed.Bob, seed.Alice);

        sessions[3].Should().BeEquivalentTo(new { PlayerOneScore = 30.0, PlayerOneWon = false, PlayerTwoScore = 40.0, PlayerTwoWon = true });
    }

    [Fact]
    public async Task GetSessionsTogether_ShouldBeEmpty_WhenThePlayersNeverMet()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seed = await SeedAsync(databaseName);
        await using var context = CreateContext(databaseName);

        var sessions = await new CompareRepository(context).GetSessionsTogether(seed.Bob, seed.Dave + 100);

        sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPairWithMostSessions_ShouldReturnThePairThatPlayedTogetherMost()
    {
        var databaseName = Guid.NewGuid().ToString();
        var seed = await SeedAsync(databaseName);
        await using var context = CreateContext(databaseName);

        var pair = await new CompareRepository(context).GetPairWithMostSessions();

        pair.Should().BeEquivalentTo(new ComparePairDto { PlayerOneId = seed.Alice, PlayerTwoId = seed.Bob, SessionCount = 4 });
    }

    [Fact]
    public async Task GetPairWithMostSessions_ShouldPreferTheLowestIds_OnATie()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var setup = CreateContext(databaseName))
        {
            var game = new Game("Game", false, GameState.Owned);
            var players = Enumerable.Range(1, 4).Select(i => new Player($"P{i}")).ToArray();
            setup.Add(game);
            setup.AddRange(players.Cast<object>());
            await setup.SaveChangesAsync();

            var late = new Session(game.Id, Day, Day.AddMinutes(10), string.Empty);
            late.AddPlayerSession(players[2].Id, null, false, true);
            late.AddPlayerSession(players[3].Id, null, false, false);
            var early = new Session(game.Id, Day, Day.AddMinutes(10), string.Empty);
            early.AddPlayerSession(players[0].Id, null, false, true);
            early.AddPlayerSession(players[1].Id, null, false, false);
            setup.AddRange(late, early);
            await setup.SaveChangesAsync();
        }

        await using var context = CreateContext(databaseName);
        var pair = await new CompareRepository(context).GetPairWithMostSessions();

        pair!.PlayerOneId.Should().BeLessThan(pair.PlayerTwoId);
        pair.SessionCount.Should().Be(1);
        pair.PlayerOneId.Should().Be(await context.Players.MinAsync(p => p.Id));
    }

    [Fact]
    public async Task GetPairWithMostSessions_ShouldReturnNull_WithoutSessions()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());

        var pair = await new CompareRepository(context).GetPairWithMostSessions();

        pair.Should().BeNull();
    }
}
