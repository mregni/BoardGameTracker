using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class GameStatisticsRepositoryTests : IDisposable
{
    private static readonly DateTime Monday = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);

    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly MainDbContext _context;
    private readonly GameStatisticsRepository _repository;

    public GameStatisticsRepositoryTests()
    {
        _context = CreateContext();
        _repository = new GameStatisticsRepository(_context);
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

    private async Task<(Game Game, Game Other)> SeedGamesAsync()
    {
        var games = await SeedAsync(new Game("Game", true), new Game("Other", true));
        return (games[0], games[1]);
    }

    private async Task<Player[]> SeedPlayersAsync(params string[] names)
    {
        return await SeedAsync(names.Select(name => new Player(name)).ToArray());
    }

    private static Game Priced(string title, decimal? price, GameState state = GameState.Owned)
    {
        var game = new Game(title, true, state);
        game.UpdateBuyingPrice(price);
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

    private static Session Coop(Game game, DateTime start, bool teamWon)
    {
        var session = Play(game, start, 60);
        session.MarkAsCoop(teamWon, null);
        return session;
    }

    [Theory]
    [InlineData(40, 3, 13.33)]
    [InlineData(20, 3, 6.67)]
    [InlineData(10, 4, 2.5)]
    public async Task GetPricePerPlay_ShouldDivideThePriceBySessions_RoundedToCents(double price, int sessions, double expected)
    {
        var game = Priced("Game", (decimal)price);
        var other = Priced("Other", 1m);
        await SeedAsync(game, other);
        await SeedAsync(Enumerable.Range(0, sessions).Select(i => Play(game, Monday.AddDays(i), 60)).Append(Play(other, Monday, 60)).ToArray());

        var result = await _repository.GetPricePerPlay(game.Id);

        result.Should().Be((decimal)expected);
    }

    [Fact]
    public async Task GetPricePerPlay_ShouldBeNull_WhenTheGameIsMissingUnpricedOrUnplayed()
    {
        var unpriced = Priced("Unpriced", null);
        var unplayed = Priced("Unplayed", 40m);
        await SeedAsync(unpriced, unplayed);
        await SeedAsync(Play(unpriced, Monday, 60));

        (await _repository.GetPricePerPlay(unpriced.Id)).Should().BeNull();
        (await _repository.GetPricePerPlay(unplayed.Id)).Should().BeNull();
        (await _repository.GetPricePerPlay(unplayed.Id + 1)).Should().BeNull();
    }

    [Fact]
    public async Task ScoreAggregates_ShouldOnlyUseScoresOfTheGame_AndIgnoreMissingScores()
    {
        var (game, other) = await SeedGamesAsync();
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara");
        await SeedAsync(
            Play(game, Monday, 60, (players[0], 10, false), (players[1], 20, true), (players[2], null, false)),
            Play(game, Monday.AddDays(1), 60, (players[0], 60, true)),
            Play(other, Monday, 60, (players[0], 999, true), (players[1], 1, false)));

        (await _repository.GetHighestScore(game.Id)).Should().Be(60);
        (await _repository.GetAverageScore(game.Id)).Should().Be(30);
    }

    [Fact]
    public async Task ScoreAggregates_ShouldBeNull_WhenTheGameHasNoScores()
    {
        var (game, other) = await SeedGamesAsync();
        var alice = (await SeedPlayersAsync("Alice"))[0];
        await SeedAsync(Play(other, Monday, 60, (alice, 50, true)));

        (await _repository.GetHighestScore(game.Id)).Should().BeNull();
        (await _repository.GetAverageScore(game.Id)).Should().BeNull();
    }

    [Fact]
    public async Task GetMostWins_ShouldReturnThePlayerWithMostWinsInTheGame()
    {
        var (game, other) = await SeedGamesAsync();
        var players = await SeedPlayersAsync("Alice", "Bob");
        var (alice, bob) = (players[0], players[1]);
        await SeedAsync(
            Play(game, Monday, 60, (alice, null, true), (bob, null, false)),
            Play(game, Monday.AddDays(1), 60, (alice, null, false), (bob, null, true)),
            Play(game, Monday.AddDays(2), 60, (alice, null, true), (bob, null, false)),
            Play(other, Monday, 60, (bob, null, true)),
            Play(other, Monday.AddDays(1), 60, (bob, null, true)),
            Play(other, Monday.AddDays(2), 60, (bob, null, true)));

        var (player, wins) = await _repository.GetMostWins(game.Id);

        player!.Id.Should().Be(alice.Id);
        player.Name.Should().Be("Alice");
        wins.Should().Be(2);
    }

    [Fact]
    public async Task GetMostWins_ShouldBreakATieByTheBetterWinRate()
    {
        var (game, _) = await SeedGamesAsync();
        var players = await SeedPlayersAsync("Alice", "Bob");
        var (alice, bob) = (players[0], players[1]);
        await SeedAsync(
            Play(game, Monday, 60, (alice, null, true)),
            Play(game, Monday.AddDays(1), 60, (alice, null, false)),
            Play(game, Monday.AddDays(2), 60, (bob, null, true)));

        var (player, wins) = await _repository.GetMostWins(game.Id);

        player!.Id.Should().Be(bob.Id);
        wins.Should().Be(1);
    }

    [Fact]
    public async Task GetMostWins_ShouldPickTheLowestId_WhenWinsAndRateAreEqual()
    {
        var (game, _) = await SeedGamesAsync();
        var players = await SeedPlayersAsync("Alice", "Bob");
        await SeedAsync(
            Play(game, Monday, 60, (players[1], null, true)),
            Play(game, Monday.AddDays(1), 60, (players[0], null, true)));

        var (player, _) = await _repository.GetMostWins(game.Id);

        player!.Id.Should().Be(players[0].Id);
    }

    [Fact]
    public async Task GetMostWins_ShouldReturnNobody_WhenNoPlayerWon()
    {
        var (game, _) = await SeedGamesAsync();
        var alice = (await SeedPlayersAsync("Alice"))[0];
        await SeedAsync(Play(game, Monday, 60, (alice, null, false)));

        var (player, wins) = await _repository.GetMostWins(game.Id);

        player.Should().BeNull();
        wins.Should().Be(0);
    }

    [Fact]
    public async Task CountScoredSessions_ShouldCountSessionsOfTheGameWithAtLeastOneScore()
    {
        var (game, other) = await SeedGamesAsync();
        var players = await SeedPlayersAsync("Alice", "Bob");
        await SeedAsync(
            Play(game, Monday, 60, (players[0], 10, true), (players[1], 20, false)),
            Play(game, Monday.AddDays(1), 60, (players[0], null, true), (players[1], 5, false)),
            Play(game, Monday.AddDays(2), 60, (players[0], null, true)),
            Play(other, Monday, 60, (players[0], 10, true)));

        var result = await _repository.CountScoredSessions(game.Id);

        result.Should().Be(2);
    }

    [Fact]
    public async Task GetCoopResults_ShouldOnlyCountCoopSessionsOfTheGame()
    {
        var (game, other) = await SeedGamesAsync();
        await SeedAsync(
            Coop(game, Monday, true),
            Coop(game, Monday.AddDays(1), true),
            Coop(game, Monday.AddDays(2), true),
            Coop(game, Monday.AddDays(3), false),
            Play(game, Monday.AddDays(4), 60),
            Coop(other, Monday, false));

        var result = await _repository.GetCoopResults(game.Id);

        result.Should().Be((3, 1));
    }

    [Fact]
    public async Task GetExpansionCount_ShouldCountTheGamesExpansions_AndBeNullWithoutAny()
    {
        var (game, other) = await SeedGamesAsync();
        await SeedAsync(new Expansion("One", null, game.Id), new Expansion("Two", null, game.Id));

        (await _repository.GetExpansionCount(game.Id)).Should().Be(2);
        (await _repository.GetExpansionCount(other.Id)).Should().BeNull();
    }

    [Fact]
    public async Task PlayTimes_ShouldOnlyUseSessionsOfTheGame()
    {
        var (game, other) = await SeedGamesAsync();
        await SeedAsync(
            Play(game, Monday, 30),
            Play(game, Monday.AddDays(1), 90),
            Play(other, Monday, 600));

        (await _repository.GetTotalPlayedTime(game.Id)).Should().BeApproximately(120, 0.001);
        (await _repository.GetAveragePlayTime(game.Id)).Should().BeApproximately(60, 0.001);
    }

    [Fact]
    public async Task PlayTimes_ShouldBeZero_WhenTheGameHasNoSessions()
    {
        var (game, other) = await SeedGamesAsync();
        await SeedAsync(Play(other, Monday, 600));

        (await _repository.GetTotalPlayedTime(game.Id)).Should().Be(0);
        (await _repository.GetAveragePlayTime(game.Id)).Should().Be(0);
    }

    [Fact]
    public async Task OwnedPriceTotals_ShouldOnlyUseOwnedGamesWithAPrice()
    {
        await SeedAsync(
            Priced("Owned", 40.50m),
            Priced("Also owned", 9.25m),
            Priced("Owned without price", null),
            Priced("Wanted", 100m, GameState.Wanted),
            Priced("Previously owned", 60m, GameState.PreviouslyOwned),
            Priced("For trade", 70m, GameState.ForTrade),
            Priced("Sold", 80m, GameState.Sold));

        (await _repository.GetTotalPayedAsync()).Should().Be(49.75m);
        (await _repository.CountPricedOwnedGamesAsync()).Should().Be(2);
    }

    [Fact]
    public async Task CountPricedOwnedGamesAsync_ShouldBeZero_WhenNoOwnedGameHasAPrice()
    {
        await SeedAsync(Priced("Owned without price", null), Priced("Wanted", 100m, GameState.Wanted));

        (await _repository.CountPricedOwnedGamesAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetGameStateCounts_ShouldCountGamesPerState()
    {
        await SeedAsync(
            new Game("One", state: GameState.Owned),
            new Game("Two", state: GameState.Owned),
            new Game("Three", state: GameState.Wanted),
            new Game("Four", state: GameState.Sold));

        var result = await _repository.GetGameStateCounts();

        result.ToDictionary(x => x.Type, x => x.GameCount).Should().BeEquivalentTo(new Dictionary<GameState, int>
        {
            [GameState.Owned] = 2,
            [GameState.Wanted] = 1,
            [GameState.Sold] = 1
        });
    }

    [Fact]
    public async Task GetSessionStartTimes_ShouldOnlyReturnSessionsOfTheGame()
    {
        var (game, other) = await SeedGamesAsync();
        await SeedAsync(Play(game, Monday, 60), Play(game, Monday.AddDays(1), 60), Play(other, Monday.AddDays(2), 60));

        var result = await _repository.GetSessionStartTimes(game.Id);

        result.Should().BeEquivalentTo(new[] { Monday, Monday.AddDays(1) });
    }

    [Fact]
    public async Task ScoreRecords_ShouldPickTheRightPlayerSession_WithinTheGame()
    {
        var (game, other) = await SeedGamesAsync();
        var players = await SeedPlayersAsync("Alice", "Bob", "Cara", "Dave");
        var (alice, bob, cara, dave) = (players[0], players[1], players[2], players[3]);
        await SeedAsync(
            Play(game, Monday, 60, (alice, 90, true), (bob, 70, false), (cara, 40, true), (dave, 10, false)),
            Play(game, Monday.AddDays(1), 60, (alice, null, true), (bob, null, false)),
            Play(other, Monday, 60, (alice, 1000, true), (bob, 500, false), (cara, 1, true), (dave, 0, false)));

        var highest = await _repository.GetHighestScoringPlayer(game.Id);
        var highestLoser = await _repository.GetHighestLosingPlayer(game.Id);
        var lowestWinner = await _repository.GetLowestWinning(game.Id);
        var lowest = await _repository.GetLowestScoringPlayer(game.Id);

        (highest!.PlayerId, highest.Score).Should().Be((alice.Id, 90));
        (highestLoser!.PlayerId, highestLoser.Score).Should().Be((bob.Id, 70));
        (lowestWinner!.PlayerId, lowestWinner.Score).Should().Be((cara.Id, 40));
        (lowest!.PlayerId, lowest.Score).Should().Be((dave.Id, 10));
    }

    [Fact]
    public async Task ScoreRecords_ShouldBeNull_WhenTheGameHasNoScores()
    {
        var (game, _) = await SeedGamesAsync();
        var alice = (await SeedPlayersAsync("Alice"))[0];
        await SeedAsync(Play(game, Monday, 60, (alice, null, true)));

        (await _repository.GetHighestScoringPlayer(game.Id)).Should().BeNull();
        (await _repository.GetHighestLosingPlayer(game.Id)).Should().BeNull();
        (await _repository.GetLowestWinning(game.Id)).Should().BeNull();
        (await _repository.GetLowestScoringPlayer(game.Id)).Should().BeNull();
    }

    [Fact]
    public async Task GetMostPlayedGames_ShouldRankByPlayCount_ThenTitle_AndHonourTheCount()
    {
        var games = await SeedAsync(new Game("Zoo"), new Game("Brass"), new Game("Azul"), new Game("Catan"));
        games[0].UpdateImage("zoo.png");
        await using (var context = CreateContext())
        {
            context.Update(games[0]);
            await context.SaveChangesAsync();
        }

        await SeedAsync(
            Play(games[0], Monday, 60),
            Play(games[0], Monday.AddDays(1), 60),
            Play(games[0], Monday.AddDays(2), 60),
            Play(games[1], Monday, 60),
            Play(games[1], Monday.AddDays(1), 60),
            Play(games[2], Monday, 60),
            Play(games[2], Monday.AddDays(1), 60),
            Play(games[3], Monday, 60));

        var result = await _repository.GetMostPlayedGames(3);

        result.Should().Equal(
            (games[0].Id, "Zoo", "zoo.png", 3),
            (games[2].Id, "Azul", null, 2),
            (games[1].Id, "Brass", null, 2));
    }

    [Fact]
    public async Task GetMostPlayedGamesSince_ShouldOnlyCountSessionsFromTheExactStart()
    {
        var (game, other) = await SeedGamesAsync();
        await SeedAsync(
            Play(game, Monday.AddTicks(-1), 60),
            Play(game, Monday.AddDays(-3), 60),
            Play(game, Monday.AddDays(-4), 60),
            Play(game, Monday, 60),
            Play(other, Monday.AddDays(1), 60),
            Play(other, Monday.AddDays(2), 60));

        var result = await _repository.GetMostPlayedGamesSince(5, Monday);

        result.Should().Equal(
            (other.Id, "Other", null, 2),
            (game.Id, "Game", null, 1));
    }
}
