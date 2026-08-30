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
    Task AddGameCategoriesIfNotExists(IEnumerable<GameCategory> categories);
    Task AddGameMechanicsIfNotExists(IEnumerable<GameMechanic> mechanics);
    Task AddPeopleIfNotExists(IEnumerable<Person> people);
    Task<Game?> GetGameByBggId(int bggId);
    Task<List<Game>> GetGamesOverviewList();
    Task<List<Game>> GetWantedGamesWithWatchId();
    Task<GameWatchInfo?> GetWatchInfo(int gameId);
    Task<int> CountAsync();
    Task<List<Expansion>> GetExpansions(List<int> expansionIds);
    Task<int> GetTotalExpansionCount();
    Task DeleteExpansion(int gameId, int expansionId);
    Task<List<Game>> GetRecentlyAddedGames(int count);
    Task<List<Game>> GetGamesWithNoRecentSessions(DateTime cutoffDate);
    Task<int> CountGamesWithNoRecentSessions(DateTime cutoffDate);
    Task<List<ShameGame>> GetShameGames(DateTime cutoffDate);
    Task<List<Game>> GetByIdsAsync(IEnumerable<int> ids);
}