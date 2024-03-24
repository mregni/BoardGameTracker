using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameRepository
{
    Task AddGameCategoriesIfNotExists(IEnumerable<GameCategory> categories);
    Task AddGameMechanicsIfNotExists(IEnumerable<GameMechanic> mechanics);
    Task AddPeopleIfNotExists(IEnumerable<Person> people);
    Task<Game> InsertGame(Game game);
    Task<Game?> GetGameByBggId(int bggId);
    Task<List<Game>> GetGamesOverviewList();
    Task<Game?> GetGameById(int id);
    Task DeleteGame(Game game);
    Task<List<Play>> GetPlays(int id);
    Task<int> GetPlayCount(int id);
    Task<TimeSpan> GetTotalPlayedTime(int id);
    Task<double?> GetPricePerPlay(int id);
    Task<int> GetUniquePlayerCount(int id);
    Task<double?> GetHighestScore(int id);
    Task<Player?> GetMostWins(int id);
    Task<double?> GetAverageScore(int id);
    Task<int> CountAsync();
}