using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Storage.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Storage;

public class StorageRepository : EfRepository<StorageLocation>, IStorageRepository
{
    private readonly MainDbContext _dbContext;

    public StorageRepository(MainDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<StorageLocation>> GetAllTrackedAsync()
    {
        return _dbContext.StorageLocations.ToListAsync();
    }

    public Task<List<Game>> GetGamesAsync(IEnumerable<int> ids)
    {
        var list = ids.ToList();
        return _dbContext.Games.Where(x => list.Contains(x.Id)).ToListAsync();
    }

    public Task<List<Expansion>> GetExpansionsAsync(IEnumerable<int> ids)
    {
        var list = ids.ToList();
        return _dbContext.Expansions.Where(x => list.Contains(x.Id)).ToListAsync();
    }

    public Task<List<Game>> GetGamesStoredInAsync(IEnumerable<int> storageLocationIds)
    {
        var list = storageLocationIds.ToList();
        return _dbContext.Games
            .Where(x => x.StorageLocationId != null && list.Contains(x.StorageLocationId.Value))
            .ToListAsync();
    }

    public Task<List<Expansion>> GetExpansionsStoredInAsync(IEnumerable<int> storageLocationIds)
    {
        var list = storageLocationIds.ToList();
        return _dbContext.Expansions
            .Where(x => x.StorageLocationId != null && list.Contains(x.StorageLocationId.Value))
            .ToListAsync();
    }

    public async Task<int> GetNextPositionAsync(int locationId, int? storageLocationId)
    {
        var games = await _dbContext.Games
            .Where(x => x.StoredAtLocationId == locationId && x.StorageLocationId == storageLocationId)
            .MaxAsync(x => (int?)x.StoragePosition);
        var expansions = await _dbContext.Expansions
            .Where(x => x.StoredAtLocationId == locationId && x.StorageLocationId == storageLocationId)
            .MaxAsync(x => (int?)x.StoragePosition);
        return Math.Max(games ?? -1, expansions ?? -1) + 1;
    }
}
