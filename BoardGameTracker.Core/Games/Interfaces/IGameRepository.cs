using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameRepository
{
    Task AddGameCategoriesIfNotExists(IEnumerable<GameCategory> categories);
    Task AddGameMechanicsIfNotExists(IEnumerable<GameMechanic> mechanics);
    Task AddPeopleIfNotExists(IEnumerable<Person> people);
    Task<Game> InsertGame(Game game);
    Task<Game?> GetGameByBggId(int bggId);
    Task<List<Game>> GetGamesOverviewList();
    Task<Game?> GetGameById(int id, bool includePlays);
    Task DeleteGame(Game game);
}