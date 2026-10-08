using System.Linq.Expressions;
using Ardalis.Specification.EntityFrameworkCore;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.ChangeDetection;
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

    public Task<List<GameCategory>> GetOrCreateCategoriesAsync(IEnumerable<string> names)
    {
        var wanted = NormalizeNames(names);
        return GetOrCreateAsync(
            _context.GameCategories,
            wanted,
            x => wanted.Contains(x.Name),
            x => x.Name,
            name => new GameCategory(name));
    }

    public Task<List<GameMechanic>> GetOrCreateMechanicsAsync(IEnumerable<string> names)
    {
        var wanted = NormalizeNames(names);
        return GetOrCreateAsync(
            _context.GameMechanics,
            wanted,
            x => wanted.Contains(x.Name),
            x => x.Name,
            name => new GameMechanic(name));
    }

    public Task<List<Person>> GetOrCreatePeopleAsync(IEnumerable<PersonKey> people)
    {
        var wanted = people
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => x with { Name = x.Name.Trim() })
            .Distinct()
            .ToList();
        var names = wanted.Select(x => x.Name).Distinct().ToList();
        return GetOrCreateAsync(
            _context.People,
            wanted,
            x => names.Contains(x.Name),
            x => new PersonKey(x.Name, x.Type),
            key => new Person(key.Name, key.Type));
    }

    private static List<string> NormalizeNames(IEnumerable<string> names)
    {
        return names
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();
    }

    private static async Task<List<T>> GetOrCreateAsync<T, TKey>(
        DbSet<T> set,
        IReadOnlyCollection<TKey> keys,
        Expression<Func<T, bool>> matches,
        Func<T, TKey> keyOf,
        Func<TKey, T> create)
        where T : class
        where TKey : notnull
    {
        if (keys.Count == 0)
        {
            return [];
        }

        var known = (await set.Where(matches).ToListAsync())
            .Concat(set.Local)
            .DistinctBy(keyOf)
            .ToDictionary(keyOf);

        var result = new List<T>(keys.Count);
        foreach (var key in keys)
        {
            if (!known.TryGetValue(key, out var entity))
            {
                entity = create(key);
                known[key] = entity;
                await set.AddAsync(entity);
            }

            result.Add(entity);
        }

        return result;
    }

    public Task<Game?> GetGameByBggId(int bggId)
    {
        return SingleOrDefaultAsync(new GameByBggIdSpec(bggId));
    }

    public Task<List<Game>> GetGamesOverviewList()
    {
        return ListAsync(new GamesOverviewSpec());
    }

    public Task<List<Game>> GetTrackedGames()
    {
        return ListAsync(new TrackedGamesSpec());
    }

    public Task<GameWatchInfo?> GetWatchInfo(int gameId)
    {
        return _context.Games
            .Where(game => game.Id == gameId)
            .Select(game => new GameWatchInfo(game.Id, game.ChangeDetectionWatchId))
            .FirstOrDefaultAsync();
    }

    public Task<List<Expansion>> GetExpansions(int gameId, List<int> expansionIds)
    {
        return _context.Expansions
            .WithSpecification(new ExpansionsByIdsSpec(gameId, expansionIds))
            .ToListAsync();
    }

    public Task<int> GetTotalExpansionCount(CancellationToken cancellationToken = default)
    {
        return _context.Expansions.CountAsync(cancellationToken);
    }

    public async Task<bool> DeleteExpansion(int gameId, int expansionId)
    {
        var game = await _context.Games
            .Include(x => x.Expansions)
            .SingleOrDefaultAsync(x => x.Id == gameId);

        var expansion = game?.Expansions.FirstOrDefault(e => e.Id == expansionId);
        if (game == null || expansion == null)
        {
            return false;
        }

        game.RemoveExpansion(expansion);
        return true;
    }

    public Task<List<Game>> GetRecentlyAddedGames(int count, CancellationToken cancellationToken = default)
    {
        return ListAsync(new RecentlyAddedGamesSpec(count), cancellationToken);
    }

    public Task<int> CountGamesWithNoRecentSessions(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        return CountAsync(new GamesWithNoRecentSessionsSpec(cutoffDate), cancellationToken);
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
