using Ardalis.Specification.EntityFrameworkCore;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Games.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Games;

public class GameRepository : EfRepository<Game>, IGameRepository
{
    private readonly MainDbContext _context;

    public GameRepository(MainDbContext context): base(context)
    {
        _context = context;
    }

    public async Task AddGameCategoriesIfNotExists(IEnumerable<GameCategory> categories)
    {
        await _context.GameCategories.AddRangeIfNotExists(categories);
    }

    public async Task AddGameMechanicsIfNotExists(IEnumerable<GameMechanic> mechanics)
    {
        await _context.GameMechanics.AddRangeIfNotExists(mechanics);
    }

    public async Task AddPeopleIfNotExists(IEnumerable<Person> people)
    {
        await _context.People.AddRangeIfNotExists(people);
    }

    public Task<Game?> GetGameByBggId(int bggId)
    {
        return SingleOrDefaultAsync(new GameByBggIdSpec(bggId));
    }

    public Task<List<Game>> GetGamesOverviewList()
    {
        return ListAsync(new GamesOverviewSpec());
    }

    public override Task<Game?> GetByIdAsync(int id)
    {
        return SingleOrDefaultAsync(new GameByIdWithDetailsSpec(id));
    }

    public Task<List<Expansion>> GetExpansions(List<int> expansionIds)
    {
        return _context.Expansions
            .WithSpecification(new ExpansionsByIdsSpec(expansionIds))
            .ToListAsync();
    }

    public Task<int> GetTotalExpansionCount()
    {
        return _context.Expansions.CountAsync();
    }

    public Task<int> CountAsync()
    {
        return _context.Games.CountAsync();
    }

    public async Task DeleteExpansion(int gameId, int expansionId)
    {
        var game = await _context.Games
            .Include(x => x.Expansions)
            .SingleOrDefaultAsync(x => x.Id == gameId);

        if (game == null)
        {
            return;
        }

        var expansion = game.Expansions.FirstOrDefault(e => e.Id == expansionId);
        if (expansion != null)
        {
            game.RemoveExpansion(expansion.BggId);
        }
    }

    public Task<List<Game>> GetRecentlyAddedGames(int count)
    {
        return ListAsync(new RecentlyAddedGamesSpec(count));
    }

    public Task<List<Game>> GetGamesWithNoRecentSessions(DateTime cutoffDate)
    {
        return ListAsync(new GamesWithNoRecentSessionsSpec(cutoffDate));
    }

    public Task<int> CountGamesWithNoRecentSessions(DateTime cutoffDate)
    {
        return CountAsync(new GamesWithNoRecentSessionsSpec(cutoffDate));
    }

    public Task<List<ShameGame>> GetShameGames(DateTime cutoffDate)
    {
        return ListAsync(new ShameGamesSpec(cutoffDate));
    }

    public Task<List<Game>> GetByIdsAsync(IEnumerable<int> ids)
    {
        return ListAsync(new GamesByIdsSpec(ids));
    }
}
