using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Games.Interfaces;

/// <summary>
/// Repository for basic CRUD operations on games
/// </summary>
public interface IGameRepository: IRepository<Game>
{
    Task<List<GameCategory>> GetOrCreateCategoriesAsync(IEnumerable<string> names);
    Task<List<GameMechanic>> GetOrCreateMechanicsAsync(IEnumerable<string> names);
    Task<List<Person>> GetOrCreatePeopleAsync(IEnumerable<PersonKey> people);
    Task<Game?> GetGameByBggId(int bggId);
    Task<List<Game>> GetGamesOverviewList();
    Task<List<Game>> GetTrackedGames();
    Task<GameWatchInfo?> GetWatchInfo(int gameId);
    Task<List<Expansion>> GetExpansions(int gameId, List<int> expansionIds);
    Task<int> GetTotalExpansionCount(CancellationToken cancellationToken = default);
    Task<bool> DeleteExpansion(int gameId, int expansionId);
    Task<List<Game>> GetRecentlyAddedGames(int count, CancellationToken cancellationToken = default);
    Task<int> CountGamesWithNoRecentSessions(DateTime cutoffDate, CancellationToken cancellationToken = default);
    Task<List<ShameGame>> GetShameGames(DateTime cutoffDate);
    Task<List<Game>> GetByIdsAsync(IEnumerable<int> ids);
}