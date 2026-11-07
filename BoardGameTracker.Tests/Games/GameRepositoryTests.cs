using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class GameRepositoryTests : IDisposable
{
    private const string WatchId = "3f2504e0-4f89-41d3-9a0c-0305e82c3301";
    private static readonly DateTime Cutoff = new(2026, 3, 2, 12, 0, 0, DateTimeKind.Utc);

    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly MainDbContext _context;
    private readonly GameRepository _repository;

    public GameRepositoryTests()
    {
        _context = CreateContext();
        _repository = new GameRepository(_context);
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

    private static Game GameAddedAt(string title, DateTime? additionDate, GameState state = GameState.Owned)
    {
        var game = new Game(title, true, state);
        game.UpdateAdditionDate(additionDate);
        return game;
    }

    private static Game Priced(string title, decimal? price, GameState state = GameState.Owned)
    {
        var game = new Game(title, true, state);
        game.UpdateBuyingPrice(price);
        return game;
    }

    private static Session SessionAt(Game game, DateTime start)
    {
        return new Session(game.Id, start, start.AddHours(1), string.Empty);
    }

    [Fact]
    public async Task GetOrCreateCategoriesAsync_ShouldTrimDeduplicateAndSkipBlankNames_AndLeaveOtherRowsOut()
    {
        var existing = (await SeedAsync(new GameCategory("Strategy"), new GameCategory("Party")))[0];

        var result = await _repository.GetOrCreateCategoriesAsync([" Strategy ", "Family", "Family ", "  ", string.Empty]);

        result.Select(x => x.Name).Should().Equal("Strategy", "Family");
        result[0].Id.Should().Be(existing.Id);
        _context.ChangeTracker.Entries<GameCategory>().Where(x => x.State == EntityState.Added).Select(x => x.Entity.Name).Should().Equal("Family");
        (await _context.GameCategories.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task GetOrCreateCategoriesAsync_ShouldReturnNothing_WhenEveryNameIsBlank()
    {
        var result = await _repository.GetOrCreateCategoriesAsync(["  ", string.Empty]);

        result.Should().BeEmpty();
        _context.ChangeTracker.Entries<GameCategory>().Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrCreateMechanicsAsync_ShouldReuseExistingRows_AndAddOnlyTheMissingName()
    {
        var existing = (await SeedAsync(new GameMechanic("Drafting"), new GameMechanic("Auction")))[0];

        var result = await _repository.GetOrCreateMechanicsAsync([" Drafting", "Worker placement", "Worker placement", " "]);

        result.Select(x => x.Name).Should().Equal("Drafting", "Worker placement");
        result[0].Id.Should().Be(existing.Id);
        _context.ChangeTracker.Entries<GameMechanic>().Where(x => x.State == EntityState.Added).Select(x => x.Entity.Name).Should().Equal("Worker placement");
    }

    [Fact]
    public async Task GetOrCreatePeopleAsync_ShouldTrimDeduplicateAndSkipBlankNames()
    {
        var existing = (await SeedAsync(new Person("Reiner", PersonType.Designer)))[0];

        var result = await _repository.GetOrCreatePeopleAsync(
        [
            new PersonKey(" Reiner ", PersonType.Designer),
            new PersonKey("Reiner", PersonType.Designer),
            new PersonKey("Reiner", PersonType.Artist),
            new PersonKey("  ", PersonType.Publisher)
        ]);

        result.Select(x => (x.Name, x.Type)).Should().Equal(("Reiner", PersonType.Designer), ("Reiner", PersonType.Artist));
        result[0].Id.Should().Be(existing.Id);
        _context.ChangeTracker.Entries<Person>().Where(x => x.State == EntityState.Added).Select(x => x.Entity.Type).Should().Equal(PersonType.Artist);
    }

    [Fact]
    public async Task GetGameByBggId_ShouldReturnTheMatchingGame()
    {
        var wanted = new Game("Wanted");
        wanted.UpdateBggId(174430);
        var other = new Game("Other");
        other.UpdateBggId(13);
        await SeedAsync(other, wanted);

        (await _repository.GetGameByBggId(174430))!.Id.Should().Be(wanted.Id);
        (await _repository.GetGameByBggId(1)).Should().BeNull();
    }

    [Fact]
    public async Task GetGamesOverviewList_ShouldOrderByTitle_AndLoadExpansionsCategoriesAndLoans()
    {
        var games = await SeedAsync(new Game("Zoo"), new Game("Azul"));
        var player = (await SeedAsync(new Player("Alice")))[0];
        await SeedAsync(new Expansion("Crystal", null, games[1].Id));
        await SeedAsync(new Loan(games[1].Id, player.Id, Cutoff));
        await using (var context = CreateContext())
        {
            var azul = await context.Games.Include(x => x.Categories).SingleAsync(x => x.Id == games[1].Id);
            azul.AddCategory(new GameCategory("Abstract"));
            await context.SaveChangesAsync();
        }

        var result = await _repository.GetGamesOverviewList();

        result.Select(x => x.Title).Should().Equal("Azul", "Zoo");
        result[0].Expansions.Select(x => x.Title).Should().Equal("Crystal");
        result[0].Categories.Select(x => x.Name).Should().Equal("Abstract");
        result[0].Loans.Select(x => x.PlayerId).Should().Equal(player.Id);
        result[1].Expansions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlayStatsPerGame_ShouldCountSessionsAndTakeTheLatestStart_OnlyForPlayedGames()
    {
        var games = await SeedAsync(new Game("Played"), new Game("Also played"), new Game("Never played"));
        await SeedAsync(
            SessionAt(games[0], Cutoff),
            SessionAt(games[0], Cutoff.AddDays(3)),
            SessionAt(games[0], Cutoff.AddDays(1)),
            SessionAt(games[1], Cutoff.AddDays(-1)));

        var result = await _repository.GetPlayStatsPerGame();

        result.Should().BeEquivalentTo(new Dictionary<int, GamePlayStats>
        {
            [games[0].Id] = new(3, Cutoff.AddDays(3)),
            [games[1].Id] = new(1, Cutoff.AddDays(-1))
        });
    }

    [Fact]
    public async Task GetTrackedGames_ShouldOnlyReturnGamesWithAWatch()
    {
        var tracked = new Game("Tracked");
        tracked.UpdateChangeDetectionWatchId(WatchId);
        await SeedAsync(new Game("Untracked"), tracked);

        var result = await _repository.GetTrackedGames();

        result.Select(x => x.Id).Should().Equal(tracked.Id);
    }

    [Fact]
    public async Task GetWatchInfo_ShouldReturnTheWatchOfTheRequestedGame()
    {
        var tracked = new Game("Tracked");
        tracked.UpdateChangeDetectionWatchId(WatchId);
        var untracked = new Game("Untracked");
        await SeedAsync(tracked, untracked);

        (await _repository.GetWatchInfo(tracked.Id)).Should().Be(new GameWatchInfo(tracked.Id, WatchId));
        (await _repository.GetWatchInfo(untracked.Id)).Should().Be(new GameWatchInfo(untracked.Id, null));
        (await _repository.GetWatchInfo(untracked.Id + 1)).Should().BeNull();
    }

    [Fact]
    public async Task GetExpansions_ShouldOnlyReturnRequestedExpansionsOfTheGame()
    {
        var games = await SeedAsync(new Game("First"), new Game("Second"));
        var expansions = await SeedAsync(
            new Expansion("Wanted", null, games[0].Id),
            new Expansion("Not requested", null, games[0].Id),
            new Expansion("Other game", null, games[1].Id));

        var result = await _repository.GetExpansions(games[0].Id, [expansions[0].Id, expansions[2].Id]);

        result.Select(x => x.Title).Should().Equal("Wanted");
    }

    [Fact]
    public async Task GetTotalExpansionCount_ShouldCountTheExpansionsOfEveryGame()
    {
        var games = await SeedAsync(new Game("First"), new Game("Second"));
        await SeedAsync(new Expansion("One", null, games[0].Id), new Expansion("Two", null, games[0].Id), new Expansion("Three", null, games[1].Id));

        var result = await _repository.GetTotalExpansionCount();

        result.Should().Be(3);
    }

    [Fact]
    public async Task DeleteExpansion_ShouldRemoveOnlyThatExpansion_OnceSaved()
    {
        var game = (await SeedAsync(new Game("Game")))[0];
        var expansions = await SeedAsync(new Expansion("Removed", null, game.Id), new Expansion("Kept", null, game.Id));

        var result = await _repository.DeleteExpansion(game.Id, expansions[0].Id);
        await _context.SaveChangesAsync();

        result.Should().BeTrue();
        await using var verify = CreateContext();
        (await verify.Expansions.Select(x => x.Title).ToListAsync()).Should().Equal("Kept");
    }

    [Fact]
    public async Task DeleteExpansion_ShouldReturnFalse_WhenTheExpansionBelongsToAnotherGame()
    {
        var games = await SeedAsync(new Game("First"), new Game("Second"));
        var expansion = (await SeedAsync(new Expansion("Other game", null, games[1].Id)))[0];

        var result = await _repository.DeleteExpansion(games[0].Id, expansion.Id);
        await _context.SaveChangesAsync();

        result.Should().BeFalse();
        await using var verify = CreateContext();
        (await verify.Expansions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteExpansion_ShouldReturnFalse_WhenTheGameDoesNotExist()
    {
        var game = (await SeedAsync(new Game("Game")))[0];
        var expansion = (await SeedAsync(new Expansion("Expansion", null, game.Id)))[0];

        var result = await _repository.DeleteExpansion(game.Id + 1, expansion.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetRecentlyAddedGames_ShouldReturnTheNewestDatedGames()
    {
        await SeedAsync(
            GameAddedAt("Oldest", Cutoff.AddDays(-3)),
            GameAddedAt("Undated", null),
            GameAddedAt("Newest", Cutoff),
            GameAddedAt("Middle", Cutoff.AddDays(-1)));

        var result = await _repository.GetRecentlyAddedGames(2);

        result.Select(x => x.Title).Should().Equal("Newest", "Middle");
    }

    [Fact]
    public async Task GetShameGames_ShouldReturnOwnedGamesWithoutARecentSession_OrderedByTitle()
    {
        var games = await SeedAsync(
            GameAddedAt("Added before", Cutoff.AddDays(-1)),
            GameAddedAt("Added at cutoff", Cutoff),
            GameAddedAt("Never dated", null),
            GameAddedAt("Added after", Cutoff.AddTicks(1)),
            GameAddedAt("Played at cutoff", Cutoff.AddDays(-1)),
            GameAddedAt("Wanted", Cutoff.AddDays(-1), GameState.Wanted),
            GameAddedAt("Sold", Cutoff.AddDays(-1), GameState.Sold));
        await SeedAsync(
            SessionAt(games[2], Cutoff.AddTicks(-1)),
            SessionAt(games[2], Cutoff.AddDays(-5)),
            SessionAt(games[4], Cutoff));

        var result = await _repository.GetShameGames(Cutoff);

        result.Select(x => x.Title).Should().Equal("Added at cutoff", "Added before", "Never dated");
        result[2].PlayCount.Should().Be(2);
        result[2].LastSessionDate.Should().Be(Cutoff.AddTicks(-1));
        result[0].PlayCount.Should().Be(0);
        result[0].LastSessionDate.Should().BeNull();
        (await _repository.CountGamesWithNoRecentSessions(Cutoff)).Should().Be(3);
    }

    [Fact]
    public async Task GetShameGames_ShouldProjectThePriceAndRanges_AndHideAZeroPrice()
    {
        var priced = GameAddedAt("Priced", Cutoff.AddDays(-1));
        priced.UpdateBuyingPrice(39.95m);
        priced.UpdatePlayerCount(2, 4);
        priced.UpdatePlayTime(30, 60);
        var free = GameAddedAt("Free", Cutoff.AddDays(-1));
        free.UpdateBuyingPrice(0m);
        await SeedAsync(priced, free);

        var result = await _repository.GetShameGames(Cutoff);

        result.Select(x => x.Title).Should().Equal("Free", "Priced");
        result[0].Price.Should().BeNull();
        result[0].MinPlayers.Should().BeNull();
        result[0].MaxPlayTime.Should().BeNull();
        result[1].Price.Should().Be(39.95m);
        result[1].MinPlayers.Should().Be(2);
        result[1].MaxPlayers.Should().Be(4);
        result[1].MinPlayTime.Should().Be(30);
        result[1].MaxPlayTime.Should().Be(60);
    }

    [Fact]
    public async Task GetOwnedCollectionSummary_ShouldCountAllOwnedGames_AndOnlySumOwnedPrices()
    {
        await SeedAsync(
            Priced("Owned", 40.50m),
            Priced("Also owned", 19.25m),
            Priced("Owned without price", null),
            Priced("Wanted", 100m, GameState.Wanted),
            Priced("Previously owned", 60m, GameState.PreviouslyOwned),
            Priced("For trade", 70m, GameState.ForTrade),
            Priced("Sold", 80m, GameState.Sold));

        var result = await _repository.GetOwnedCollectionSummary();

        result.Should().Be(new OwnedCollectionSummary(3, 59.75m));
    }

    [Fact]
    public async Task GetOwnedCollectionSummary_ShouldBeZero_WhenNoOwnedGameHasAPrice()
    {
        await SeedAsync(Priced("Owned without price", null), Priced("Wanted", 100m, GameState.Wanted));

        var result = await _repository.GetOwnedCollectionSummary();

        result.Should().Be(new OwnedCollectionSummary(1, 0m));
    }

    [Fact]
    public async Task GetByIdsAsync_ShouldOnlyReturnTheRequestedGames()
    {
        var games = await SeedAsync(new Game("First"), new Game("Second"), new Game("Third"));

        var result = await _repository.GetByIdsAsync([games[0].Id, games[2].Id]);

        result.Select(x => x.Title).Should().BeEquivalentTo("First", "Third");
    }
}
