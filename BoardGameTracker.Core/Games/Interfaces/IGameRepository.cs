using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Games.Interfaces;

/// <summary>
/// Repository for basic CRUD operations on games
/// </summary>
public interface IGameRepository: ICrudHelper<Game>
{
    Task AddGameCategoriesIfNotExists(IEnumerable<GameCategory> categories);
    Task AddGameMechanicsIfNotExists(IEnumerable<GameMechanic> mechanics);
    Task AddPeopleIfNotExists(IEnumerable<Person> people);
    Task<Game?> GetGameByBggId(int bggId);
    Task<List<Game>> GetGamesOverviewList();
    Task<int> CountAsync();
    Task<List<Expansion>> GetExpansions(List<int> expansionIds);
    Task<int> GetTotalExpansionCount();
    Task DeleteExpansion(int gameId, int expansionId);
}