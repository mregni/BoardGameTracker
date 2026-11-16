using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Storage.Interfaces;

public interface IStorageRepository : IRepository<StorageLocation>
{
    Task<List<StorageLocation>> GetAllTrackedAsync();
    Task<List<Game>> GetGamesAsync(IEnumerable<int> ids);
    Task<List<Expansion>> GetExpansionsAsync(IEnumerable<int> ids);
    Task<List<Game>> GetGamesStoredInAsync(IEnumerable<int> storageLocationIds);
    Task<List<Expansion>> GetExpansionsStoredInAsync(IEnumerable<int> storageLocationIds);
    Task<int> GetNextPositionAsync(int locationId, int? storageLocationId);
}
