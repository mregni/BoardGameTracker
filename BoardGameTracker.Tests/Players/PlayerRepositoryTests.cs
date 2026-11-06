using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Players;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Players;

public class PlayerRepositoryTests : IDisposable
{
    private static readonly DateTime Monday = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);

    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly MainDbContext _context;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly PlayerRepository _repository;

    public PlayerRepositoryTests()
    {
        _context = CreateContext();
        _repository = new PlayerRepository(
            _context,
            new EfReadRepository<Session>(_context),
            new EfReadRepository<PlayerSession>(_context),
            _dateTimeProviderMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _dateTimeProviderMock.VerifyNoOtherCalls();
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

    private async Task<Player[]> SeedPlayersAsync(params string[] names)
    {
        return await SeedAsync(names.Select(name => new Player(name)).ToArray());
    }

    private async Task<Game> SeedGameAsync(string title = "Scored game", bool hasScoring = true, string? image = null)
    {
        var game = new Game(title, hasScoring);
        game.UpdateImage(image);
        await SeedAsync(game);
        return game;
    }

    private static Session Play(Game game, DateTime start, int minutes, params (Player Player, double? Score, bool Won)[] players)
    {
        var session = new Session(game.Id, start, start.AddMinutes(minutes), string.Empty);
        foreach (var (player, score, won) in players)
        {
            session.AddPlayerSession(player.Id, score, false, won);
        }

        return session;
    }

    private static Session Coop(Game game, DateTime start, bool teamWon, params Player[] players)
    {
        var session = Play(game, start, 60, players.Select(player => (player, (double?)null, false)).ToArray());
        session.MarkAsCoop(teamWon, null);
        return session;
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnThePlayerWithItsBadges()
    {
        var players = await SeedPlayersAsync("Alice", "Bob");
        var badge = Badge.CreateWithId(1, "title", "description", BadgeType.Sessions, "badge.png", BadgeLevel.Green);
        await SeedAsync(badge);
        await SeedAsync(new PlayerBadge(players[0].Id, badge.Id, Monday));

        var result = await _repository.GetByIdAsync(players[0].Id);

        result!.Name.Should().Be("Alice");
        result.PlayerBadges.Select(x => x.Badge.Id).Should().Equal(badge.Id);
        _context.ChangeTracker.Entries<Player>().Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenThePlayerDoesNotExist()
    {
        var players = await SeedPlayersAsync("Alice");

        var result = await _repository.GetByIdAsync(players[0].Id + 1);

        result.Should().BeNull();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllAsync_ShouldOrderPlayersByName()
    {
        await SeedPlayersAsync("Cara", "Alice", "Bob");

        var result = await _repository.GetAllAsync();

        result.Select(x => x.Name).Should().Equal("Alice", "Bob", "Cara");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMostPlayedGames_ShouldRankThePlayersGames_AndBreakTiesByTitle()
    {
        var players = await SeedPlayersAsync("Alice", "Bob");
        var (alice, bob) = (players[0], players[1]);
        var brass = await SeedGameAsync("Brass");
        var azul = await SeedGameAsync("Azul", image: "azul.png");
        var catan = await SeedGameAsync("Catan", image: "catan.png");
        await SeedAsync(
            Play(catan, Monday, 60, (alice, null, true), (bob, null, false)),
            Play(catan, Monday.AddDays(1), 60, (alice, null, true), (bob, null, false)),
            Play(catan, Monday.AddDays(2), 60, (alice, null, false), (bob, null, true)),
            Play(brass, Monday.AddDays(3), 60, (alice, null, true)),
            Play(brass, Monday.AddDays(4), 60, (alice, null, false)),
            Play(azul, Monday.AddDays(5), 60, (alice, null, true), (bob, null, true)),
            Play(azul, Monday.AddDays(6), 60, (alice, null, false)),
            Play(azul, Monday.AddDays(7), 60, (bob, null, true)),
            Play(azul, Monday.AddDays(8), 60, (bob, null, true)));

        var result = await _repository.GetMostPlayedGames(alice.Id, 2);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(catan.Id);
        result[0].Title.Should().Be("Catan");
        result[0].Image.Should().Be("catan.png");
        result[0].TotalSessions.Should().Be(3);
        result[0].TotalWins.Should().Be(2);
        result[0].WinningPercentage.Should().BeApproximately(66.667, 0.001);
        result[1].Id.Should().Be(azul.Id);
        result[1].Title.Should().Be("Azul");
        result[1].Image.Should().Be("azul.png");
        result[1].TotalSessions.Should().Be(2);
        result[1].TotalWins.Should().Be(1);
        result[1].WinningPercentage.Should().Be(50);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMostPlayedGames_ShouldReturnAnEmptyImage_WhenTheGameHasNone()
    {
        var alice = (await SeedPlayersAsync("Alice"))[0];
        var game = await SeedGameAsync("Brass");
        await SeedAsync(Play(game, Monday, 60, (alice, null, false)));

        var result = await _repository.GetMostPlayedGames(alice.Id, 5);

        result.Should().ContainSingle();
        result[0].Image.Should().BeEmpty();
        result[0].TotalWins.Should().Be(0);
        result[0].WinningPercentage.Should().Be(0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayLengthInMinutes_ShouldOnlySumThePlayersSessions()
    {
        var players = await SeedPlayersAsync("Alice", "Bob");
        var game = await SeedGameAsync();
        await SeedAsync(
            Play(game, Monday, 30, (players[0], null, false), (players[1], null, false)),
            Play(game, Monday.AddDays(1), 45, (players[0], null, false)),
            Play(game, Monday.AddDays(2), 120, (players[1], null, false)));

        (await _repository.GetPlayLengthInMinutes(players[0].Id)).Should().BeApproximately(75, 0.001);
        (await _repository.GetPlayLengthInMinutes(players[1].Id)).Should().BeApproximately(150, 0.001);
        (await _repository.GetPlayLengthInMinutes(players[1].Id + 1)).Should().Be(0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDistinctGameCount_ShouldCountEachOfThePlayersGamesOnce()
    {
        var players = await SeedPlayersAsync("Alice", "Bob");
        var first = await SeedGameAsync("First");
        var second = await SeedGameAsync("Second");
        var third = await SeedGameAsync("Third");
        await SeedAsync(
            Play(first, Monday, 30, (players[0], null, false)),
            Play(first, Monday.AddDays(1), 30, (players[0], null, false)),
            Play(second, Monday.AddDays(2), 30, (players[0], null, false), (players[1], null, false)),
            Play(third, Monday.AddDays(3), 30, (players[1], null, false)));

        var result = await _repository.GetDistinctGameCount(players[0].Id);

        result.Should().Be(2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTotalPlayCount_AndGetTotalWinCount_ShouldOnlyCountThePlayersSessions()
    {
        var players = await SeedPlayersAsync("Alice", "Bob");
        var game = await SeedGameAsync();
        await SeedAsync(
            Play(game, Monday, 30, (players[0], null, true), (players[1], null, false)),
            Play(game, Monday.AddDays(1), 30, (players[0], null, false), (players[1], null, true)),
            Play(game, Monday.AddDays(2), 30, (players[0], null, true)),
            Play(game, Monday.AddDays(3), 30, (players[0], null, false)),
            Play(game, Monday.AddDays(4), 30, (players[1], null, false)));

        (await _repository.GetTotalPlayCount(players[0].Id)).Should().Be(4);
        (await _repository.GetTotalWinCount(players[0].Id)).Should().Be(2);
        (await _repository.GetTotalPlayCount(players[1].Id)).Should().Be(3);
        (await _repository.GetTotalWinCount(players[1].Id)).Should().Be(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetCoopResults_ShouldOnlyCountThePlayersCoopSessions()
    {
        var players = await SeedPlayersAsync("Alice", "Bob");
        var (alice, bob) = (players[0], players[1]);
        var game = await SeedGameAsync();
        await SeedAsync(
            Coop(game, Monday, true, alice, bob),
            Coop(game, Monday.AddDays(1), true, alice),
            Coop(game, Monday.AddDays(2), false, alice, bob),
            Coop(game, Monday.AddDays(3), false, bob),
            Coop(game, Monday.AddDays(4), false, bob),
            Play(game, Monday.AddDays(5), 30, (alice, null, true)),
            Play(game, Monday.AddDays(6), 30, (alice, null, false)));

        (await _repository.GetCoopResults(alice.Id)).Should().Be((2, 1));
        (await _repository.GetCoopResults(bob.Id)).Should().Be((1, 3));
        (await _repository.GetCoopResults(bob.Id + 1)).Should().Be((0, 0));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRivals_ShouldCountCompetitiveSessionsAgainstEachOpponent()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara");
        var (alice, bob, cara) = (players[0], players[1], players[2]);
        var game = await SeedGameAsync();
        await SeedAsync(
            Play(game, Monday, 30, (alice, null, true), (bob, null, false)),
            Play(game, Monday.AddDays(1), 30, (alice, null, true), (bob, null, false), (cara, null, false)),
            Play(game, Monday.AddDays(2), 30, (alice, null, false), (bob, null, true)),
            Play(game, Monday.AddDays(3), 30, (alice, null, true), (bob, null, true)),
            Play(game, Monday.AddDays(4), 30, (alice, null, false), (bob, null, false)),
            Play(game, Monday.AddDays(5), 30, (bob, null, true), (cara, null, false)),
            Coop(game, Monday.AddDays(6), true, alice, bob));

        var result = await _repository.GetRivals(alice.Id);

        result.Should().BeEquivalentTo(
            new[]
            {
                new PlayerRival { PlayerId = bob.Id, Sessions = 5, Wins = 2, Losses = 1 },
                new PlayerRival { PlayerId = cara.Id, Sessions = 1, Wins = 1, Losses = 0 }
            },
            options => options.WithStrictOrdering());
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRivals_ShouldOrderOpponentsWithEqualSessionsById()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara");
        var game = await SeedGameAsync();
        await SeedAsync(Play(game, Monday, 30, (players[2], null, true), (players[0], null, false), (players[1], null, false)));

        var result = await _repository.GetRivals(players[0].Id);

        result.Select(x => x.PlayerId).Should().Equal(players[1].Id, players[2].Id);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountActivePlayersSince_ShouldCountDistinctPlayers_IncludingTheExactStart()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara");
        var game = await SeedGameAsync();
        await SeedAsync(
            Play(game, Monday, 30, (players[0], null, false), (players[1], null, false)),
            Play(game, Monday.AddDays(1), 30, (players[0], null, false)),
            Play(game, Monday.AddTicks(-1), 30, (players[2], null, false)));

        var result = await _repository.CountActivePlayersSince(Monday);

        result.Should().Be(2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayCountsSince_ShouldOnlyCountThePlayersSessionsFromTheExactStart()
    {
        var players = await SeedPlayersAsync("Alice", "Bob");
        var game = await SeedGameAsync();
        await SeedAsync(
            Play(game, Monday.AddTicks(-1), 30, (players[0], null, true)),
            Play(game, Monday, 30, (players[0], null, true), (players[1], null, false)),
            Play(game, Monday.AddDays(1), 30, (players[0], null, false), (players[1], null, true)),
            Play(game, Monday.AddDays(2), 30, (players[1], null, true)));

        var result = await _repository.GetPlayCountsSince(players[0].Id, Monday);

        result.Should().Be((2, 1));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayCountsSince_ShouldReturnZero_WhenThePlayerHasNoSessionSince()
    {
        var alice = (await SeedPlayersAsync("Alice"))[0];
        var game = await SeedGameAsync();
        await SeedAsync(Play(game, Monday.AddDays(-1), 30, (alice, null, true)));

        var result = await _repository.GetPlayCountsSince(alice.Id, Monday);

        result.Should().Be((0, 0));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTopPlayers_ShouldRankByPlayCount_AndBreakTiesById()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara");
        var (alice, bob, cara) = (players[0], players[1], players[2]);
        var game = await SeedGameAsync();
        await SeedAsync(
            Play(game, Monday, 30, (cara, null, true), (bob, null, false), (alice, null, false)),
            Play(game, Monday.AddDays(1), 30, (cara, null, false), (bob, null, true), (alice, null, true)),
            Play(game, Monday.AddDays(2), 30, (cara, null, true)));

        var result = await _repository.GetTopPlayers(2);

        result.Should().Equal(
            (cara.Id, "Cara", null, 3, 2),
            (alice.Id, "Alice", null, 2, 1));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWinTotals_ShouldReturnEveryPlayerWhoPlayed_OrderedById()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara");
        var (alice, bob) = (players[0], players[1]);
        var game = await SeedGameAsync();
        await SeedAsync(
            Play(game, Monday, 30, (bob, null, true), (alice, null, false)),
            Play(game, Monday.AddDays(1), 30, (bob, null, true)));

        var result = await _repository.GetWinTotals();

        result.Should().Equal(
            (alice.Id, "Alice", null, 1, 0),
            (bob.Id, "Bob", null, 2, 2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldAggregatePlaysWinsMinutesAndPodiumsPerPlayer()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara", "Dave");
        var (alice, bob, cara, dave) = (players[0], players[1], players[2], players[3]);
        var scored = await SeedGameAsync("Scored");
        var unscored = await SeedGameAsync("Unscored", hasScoring: false);
        await SeedAsync(
            Play(scored, Monday, 60, (alice, 50, true), (bob, 40, false), (cara, 30, false), (dave, 20, false)),
            Play(scored, Monday.AddDays(1), 30, (alice, 10, false), (bob, 90, true)),
            Play(unscored, Monday.AddDays(2), 45, (dave, 100, true), (cara, 90, false), (bob, 80, false), (alice, 70, false)));

        var result = await _repository.GetLeaderboardRows(new LeaderboardFilter());

        result.Should().BeEquivalentTo(new[]
        {
            new LeaderboardRow(alice.Id, "Alice", null, 3, 1, 1, 135),
            new LeaderboardRow(bob.Id, "Bob", null, 3, 1, 1, 135),
            new LeaderboardRow(cara.Id, "Cara", null, 2, 0, 1, 105),
            new LeaderboardRow(dave.Id, "Dave", null, 2, 1, 0, 105)
        });
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldGiveNoPodium_WhenFewerThanFourPlayersScored()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara", "Dave");
        var game = await SeedGameAsync();
        await SeedAsync(Play(game, Monday, 60, (players[0], 50, true), (players[1], 40, false), (players[2], 30, false), (players[3], null, false)));

        var result = await _repository.GetLeaderboardRows(new LeaderboardFilter());

        result.Should().HaveCount(4);
        result.Should().OnlyContain(x => x.PodiumCount == 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldRankTheLowestScoreFirst_WhenTheWinnerScoredBelowEveryLoser()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara", "Dave");
        var game = await SeedGameAsync();
        await SeedAsync(Play(game, Monday, 60, (players[0], 10, true), (players[1], 20, false), (players[2], 30, false), (players[3], 40, false)));

        var result = await _repository.GetLeaderboardRows(new LeaderboardFilter());

        result.ToDictionary(x => x.Name, x => x.PodiumCount).Should().Equal(new Dictionary<string, int>
        {
            ["Alice"] = 1,
            ["Bob"] = 1,
            ["Cara"] = 1,
            ["Dave"] = 0
        });
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldRankTheHighestScoreFirst_WhenNobodyWon()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara", "Dave");
        var game = await SeedGameAsync();
        await SeedAsync(Play(game, Monday, 60, (players[0], 10, false), (players[1], 20, false), (players[2], 30, false), (players[3], 40, false)));

        var result = await _repository.GetLeaderboardRows(new LeaderboardFilter());

        result.ToDictionary(x => x.Name, x => x.PodiumCount).Should().Equal(new Dictionary<string, int>
        {
            ["Alice"] = 0,
            ["Bob"] = 1,
            ["Cara"] = 1,
            ["Dave"] = 1
        });
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldLetTiedPlayersShareAPlace_AndPutAWinnerAboveAHigherLoser()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara", "Dave", "Erin");
        var game = await SeedGameAsync();
        await SeedAsync(Play(game, Monday, 60, (players[0], 30, true), (players[1], 40, false), (players[2], 40, false), (players[3], 40, false), (players[4], 20, false)));

        var result = await _repository.GetLeaderboardRows(new LeaderboardFilter());

        result.ToDictionary(x => x.Name, x => x.PodiumCount).Should().Equal(new Dictionary<string, int>
        {
            ["Alice"] = 1,
            ["Bob"] = 1,
            ["Cara"] = 1,
            ["Dave"] = 1,
            ["Erin"] = 0
        });
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldCountCoopWins_AndGiveNoPodiumForCoopSessions()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara", "Dave");
        var game = await SeedGameAsync();
        await SeedAsync(
            Coop(game, Monday, true, players),
            Coop(game, Monday.AddDays(1), false, players));

        var result = await _repository.GetLeaderboardRows(new LeaderboardFilter());

        result.Should().HaveCount(4);
        result.Should().OnlyContain(x => x.PlayCount == 2 && x.WinCount == 1 && x.CoopWinCount == 1 && x.PodiumCount == 0 && x.MinutesPlayed == 120);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldNotCountCompetitiveWinsAsCoopWins()
    {
        var alice = (await SeedPlayersAsync("Alice"))[0];
        var game = await SeedGameAsync();
        await SeedAsync(Play(game, Monday, 60, (alice, null, true)));

        var result = await _repository.GetLeaderboardRows(new LeaderboardFilter());

        result.Should().ContainSingle().Which.Should().Be(new LeaderboardRow(alice.Id, "Alice", null, 1, 1, 0, 60));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldOnlyCountTheChosenGame_ForTotalsAndPodiums()
    {
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara", "Dave");
        var (alice, bob, cara, dave) = (players[0], players[1], players[2], players[3]);
        var chosen = await SeedGameAsync("Chosen");
        var other = await SeedGameAsync("Other");
        await SeedAsync(
            Play(chosen, Monday, 30, (alice, 10, true), (bob, 5, false)),
            Play(other, Monday, 60, (alice, 50, true), (bob, 40, false), (cara, 30, false), (dave, 20, false)));

        var result = await _repository.GetLeaderboardRows(new LeaderboardFilter(GameId: chosen.Id));

        result.Should().BeEquivalentTo(new[]
        {
            new LeaderboardRow(alice.Id, "Alice", null, 1, 1, 0, 30),
            new LeaderboardRow(bob.Id, "Bob", null, 1, 0, 0, 30)
        });
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldUseTheLocalDayBoundaries_ForBothEndsOfThePeriod()
    {
        var localFrom = new DateOnly(2026, 3, 2).ToDateTime(TimeOnly.MinValue);
        var localUntil = new DateOnly(2026, 3, 5).ToDateTime(TimeOnly.MinValue);
        var utcFrom = new DateTime(2026, 3, 1, 23, 0, 0, DateTimeKind.Utc);
        var utcUntil = new DateTime(2026, 3, 4, 23, 0, 0, DateTimeKind.Utc);
        _dateTimeProviderMock.Setup(x => x.ConvertToUtc(localFrom)).Returns(utcFrom);
        _dateTimeProviderMock.Setup(x => x.ConvertToUtc(localUntil)).Returns(utcUntil);

        var alice = (await SeedPlayersAsync("Alice"))[0];
        var game = await SeedGameAsync();
        await SeedAsync(
            Play(game, utcFrom.AddTicks(-1), 1, (alice, null, true)),
            Play(game, utcFrom, 10, (alice, null, false)),
            Play(game, utcUntil.AddTicks(-1), 20, (alice, null, true)),
            Play(game, utcUntil, 100, (alice, null, true)));

        var result = await _repository.GetLeaderboardRows(new LeaderboardFilter(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 4)));

        result.Should().ContainSingle().Which.Should().Be(new LeaderboardRow(alice.Id, "Alice", null, 2, 1, 0, 30));
        _dateTimeProviderMock.Verify(x => x.ConvertToUtc(localFrom), Times.Once);
        _dateTimeProviderMock.Verify(x => x.ConvertToUtc(localUntil), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldOnlyGivePodiumsForSessionsInsideThePeriod()
    {
        var localFrom = new DateOnly(2026, 3, 2).ToDateTime(TimeOnly.MinValue);
        var utcFrom = new DateTime(2026, 3, 1, 23, 0, 0, DateTimeKind.Utc);
        _dateTimeProviderMock.Setup(x => x.ConvertToUtc(localFrom)).Returns(utcFrom);

        var players = await SeedPlayersAsync("Alice", "Bob", "Cara", "Dave");
        var game = await SeedGameAsync();
        await SeedAsync(
            Play(game, utcFrom.AddDays(-1), 60, (players[0], 50, true), (players[1], 40, false), (players[2], 30, false), (players[3], 20, false)),
            Play(game, utcFrom, 60, (players[3], 50, true), (players[2], 40, false), (players[1], 30, false), (players[0], 20, false)));

        var result = await _repository.GetLeaderboardRows(new LeaderboardFilter(From: new DateOnly(2026, 3, 2)));

        result.ToDictionary(x => x.Name, x => x.PodiumCount).Should().Equal(new Dictionary<string, int>
        {
            ["Alice"] = 0,
            ["Bob"] = 1,
            ["Cara"] = 1,
            ["Dave"] = 1
        });
        _dateTimeProviderMock.Verify(x => x.ConvertToUtc(localFrom), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboardRows_ShouldReturnNothing_WhenNoSessionMatches()
    {
        var alice = (await SeedPlayersAsync("Alice"))[0];
        var game = await SeedGameAsync();
        await SeedAsync(Play(game, Monday, 60, (alice, 50, true)));

        var result = await _repository.GetLeaderboardRows(new LeaderboardFilter(GameId: game.Id + 1));

        result.Should().BeEmpty();
        VerifyNoOtherCalls();
    }
}
