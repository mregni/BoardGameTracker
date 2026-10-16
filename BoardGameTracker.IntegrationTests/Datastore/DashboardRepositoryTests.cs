using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Auth.Specifications;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class DashboardRepositoryTests : IAsyncLifetime
{
    private static readonly DateTime MonthStart = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Cutoff = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly IntegrationFixture _fixture;
    private int _aliceId;
    private int _bobId;
    private int _caraId;
    private int _azulId;
    private int _brassId;
    private int _nextNightId;

    public DashboardRepositoryTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDataAsync();
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var azul = new Game("Azul", true, GameState.Owned);
        azul.UpdateBuyingPrice(30m);
        azul.UpdateAdditionDate(Cutoff.AddMonths(-2));
        var brass = new Game("Brass", true, GameState.Owned);
        brass.UpdateAdditionDate(Cutoff.AddMonths(-3));
        var catan = new Game("Catan", true, GameState.Owned);
        catan.UpdateBuyingPrice(45m);
        catan.UpdateAdditionDate(Cutoff.AddMonths(-1));
        var everdell = new Game("Everdell", true, GameState.Owned);
        everdell.UpdateBuyingPrice(20m);
        everdell.UpdateAdditionDate(Cutoff.AddMonths(-4));
        var wanted = new Game("Dune", true, GameState.Wanted);
        var alice = new Player("Alice");
        var bob = new Player("Bob");
        var cara = new Player("Cara");
        var home = new Location("Home");
        db.AddRange(azul, brass, catan, everdell, wanted, alice, bob, cara, home);
        await db.SaveChangesAsync();

        var old = new Session(azul.Id, Cutoff.AddMonths(-1), Cutoff.AddMonths(-1).AddHours(1), string.Empty);
        old.AddPlayerSession(alice.Id, null, false, true);
        old.AddPlayerSession(cara.Id, null, false, false);
        var first = new Session(brass.Id, MonthStart.AddDays(2), MonthStart.AddDays(2).AddHours(2), string.Empty);
        first.AddPlayerSession(alice.Id, null, false, true);
        first.AddPlayerSession(bob.Id, null, false, false);
        var second = new Session(brass.Id, MonthStart.AddDays(5), MonthStart.AddDays(5).AddHours(2), string.Empty);
        second.AddPlayerSession(alice.Id, null, false, false);
        second.AddPlayerSession(bob.Id, null, false, true);
        var third = new Session(catan.Id, MonthStart.AddDays(6), MonthStart.AddDays(6).AddHours(1), string.Empty);
        third.AddPlayerSession(alice.Id, null, false, true);
        db.AddRange(old, first, second, third);

        var past = GameNight.Create("Past night", string.Empty, MonthStart.AddDays(-3), alice.Id, home.Id);
        var later = GameNight.Create("Later night", string.Empty, MonthStart.AddDays(30), bob.Id, home.Id);
        var next = GameNight.Create("Next night", string.Empty, MonthStart.AddDays(20), alice.Id, home.Id);
        next.SetSuggestedGames([catan, azul]);
        next.SetInvitedPlayers([
            GameNightRsvp.Create(alice.Id, GameNightRsvpState.Accepted),
            GameNightRsvp.Create(bob.Id, GameNightRsvpState.Pending),
            GameNightRsvp.Create(cara.Id, GameNightRsvpState.Declined)
        ]);
        db.AddRange(past, later, next);
        await db.SaveChangesAsync();

        _aliceId = alice.Id;
        _bobId = bob.Id;
        _caraId = cara.Id;
        _azulId = azul.Id;
        _brassId = brass.Id;
        _nextNightId = next.Id;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SessionsStartedSinceSpec_ShouldCountOnlySessionsFromTheMonthStart()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

        var count = await repository.CountAsync(new SessionsStartedSinceSpec(MonthStart), TestContext.Current.CancellationToken);

        count.Should().Be(3);
    }

    [Fact]
    public async Task CountActivePlayersSince_ShouldCountDistinctPlayersWithASessionSince()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

        var count = await repository.CountActivePlayersSince(MonthStart, TestContext.Current.CancellationToken);

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetPlayCountsSince_ShouldReturnSessionsAndWinsOfOnePlayer()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

        var alice = await repository.GetPlayCountsSince(_aliceId, MonthStart, TestContext.Current.CancellationToken);
        var cara = await repository.GetPlayCountsSince(_caraId, MonthStart, TestContext.Current.CancellationToken);

        alice.Should().Be((3, 2));
        cara.Should().Be((0, 0));
    }

    [Fact]
    public async Task GetTopWinners_ShouldOrderByWinsThenPlays()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

        var top = await repository.GetTopWinners(2, TestContext.Current.CancellationToken);

        top.Should().Equal(
            (_aliceId, "Alice", (string?)null, 4, 3),
            (_bobId, "Bob", (string?)null, 2, 1));
    }

    [Fact]
    public async Task GetTopWinners_ShouldCountCoopTeamWins_AndWonCoopSessionsSpecShouldFindThem()
    {
        await using var scope = _fixture.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        var players = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
        (await sessions.AnyAsync(new WonCoopSessionsSpec(), TestContext.Current.CancellationToken)).Should().BeFalse();

        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var lost = new Session(_azulId, MonthStart.AddDays(8), MonthStart.AddDays(8).AddHours(1), string.Empty);
        lost.AddPlayerSession(_caraId, null, false, false);
        lost.MarkAsCoop(false, null);
        var won = new Session(_azulId, MonthStart.AddDays(9), MonthStart.AddDays(9).AddHours(1), string.Empty);
        won.AddPlayerSession(_caraId, null, false, false);
        won.MarkAsCoop(true, null);
        db.AddRange(lost, won);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await sessions.AnyAsync(new WonCoopSessionsSpec(), TestContext.Current.CancellationToken)).Should().BeTrue();
        var top = await players.GetTopWinners(3, TestContext.Current.CancellationToken);
        top.Should().Contain((_caraId, "Cara", (string?)null, 3, 1));
    }

    [Fact]
    public async Task GetMostPlayedGamesSince_ShouldOnlyCountSessionsSince()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameStatisticsRepository>();

        var games = await repository.GetMostPlayedGamesSince(4, MonthStart, TestContext.Current.CancellationToken);

        games.Select(x => (x.Title, x.PlayCount)).Should().Equal(("Brass", 2), ("Catan", 1));
        games[0].GameId.Should().Be(_brassId);
    }

    [Fact]
    public async Task GetShameSummary_ShouldCountNeverPlayedAndSumPrices()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        var summary = await repository.GetShameSummary(Cutoff, TestContext.Current.CancellationToken);

        summary.Count.Should().Be(2);
        summary.NeverPlayedCount.Should().Be(1);
        summary.TotalValue.Should().Be(50m);
    }

    [Fact]
    public async Task GetShameSummary_ShouldReturnZeros_WhenNoGameQualifies()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        var summary = await repository.GetShameSummary(Cutoff.AddYears(-5), TestContext.Current.CancellationToken);

        summary.Count.Should().Be(0);
        summary.NeverPlayedCount.Should().Be(0);
        summary.TotalValue.Should().BeNull();
    }

    [Fact]
    public async Task NextGameNightSpec_ShouldProjectTheFirstUpcomingNightWithRsvpCountsAndSuggestions()
    {
        await using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReadRepository<GameNight>>();

        var next = await repository.FirstOrDefaultAsync(new NextGameNightSpec(MonthStart), TestContext.Current.CancellationToken);
        var planned = await repository.CountAsync(new FutureGameNightsSpec(MonthStart), TestContext.Current.CancellationToken);

        next.Should().NotBeNull();
        next!.Id.Should().Be(_nextNightId);
        next.Title.Should().Be("Next night");
        next.LocationName.Should().Be("Home");
        next.HostId.Should().Be(_aliceId);
        next.HostName.Should().Be("Alice");
        next.AcceptedCount.Should().Be(1);
        next.PendingCount.Should().Be(1);
        next.DeclinedCount.Should().Be(1);
        next.SuggestedGames.Select(x => x.Title).Should().Equal("Azul", "Catan");
        next.SuggestedGames[0].Id.Should().Be(_azulId);
        planned.Should().Be(2);
    }

    [Fact]
    public async Task LinkedPlayerByUserIdSpec_ShouldReturnTheLinkedPlayer_AndNothingForUnlinkedUsers()
    {
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var linked = new ApplicationUser($"dash-linked-{Guid.NewGuid():N}", null);
        linked.LinkPlayer(_bobId);
        var unlinked = new ApplicationUser($"dash-unlinked-{Guid.NewGuid():N}", null);
        db.Users.AddRange(linked, unlinked);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        try
        {
            var repository = scope.ServiceProvider.GetRequiredService<IReadRepository<ApplicationUser>>();

            var player = await repository.FirstOrDefaultAsync(new LinkedPlayerByUserIdSpec(linked.Id), TestContext.Current.CancellationToken);
            var none = await repository.FirstOrDefaultAsync(new LinkedPlayerByUserIdSpec(unlinked.Id), TestContext.Current.CancellationToken);

            player.Should().NotBeNull();
            player!.Id.Should().Be(_bobId);
            player.Name.Should().Be("Bob");
            none.Should().BeNull();
        }
        finally
        {
            await db.Users.Where(x => x.Id == linked.Id || x.Id == unlinked.Id).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }
    }
}
