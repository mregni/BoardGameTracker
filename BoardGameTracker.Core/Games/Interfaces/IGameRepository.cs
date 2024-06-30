using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Models.Charts;

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
    Task<List<Play>> GetPlays(int id, int skip, int? take);
    Task<List<Play>> GetPlays(int id, int dayCount);
    Task<int> GetPlayCount(int id);
    Task<TimeSpan> GetTotalPlayedTime(int id);
    Task<double?> GetPricePerPlay(int id);
    Task<DateTime?> GetLastPlayedDateTime(int id);
    Task<double?> GetHighestScore(int id);
    Task<Player?> GetMostWins(int id);
    Task<double?> GetAverageScore(int id);
    Task<int> CountAsync();
    Task<int?> GetShortestPlay(int id);
    Task<int?> GetLongestPlay(int id);
    Task<int?> GetHighScorePlay(int id);
    Task<int?> GetLowestScorePlay(int id);
    Task<int> GetTotalPlayCount(int id);
    Task<List<IGrouping<DayOfWeek,Play>>> GetPlayByDayChart(int id);
    Task<List<IGrouping<int, int>>> GetPlayerCountChart(int id);
    Task<PlayerPlay?> GetHighestScoringPlayer(int id);
    Task<PlayerPlay?> GetHighestLosingPlayer(int id);
    Task<PlayerPlay?> GetLowestWinning(int id);
    Task<PlayerPlay?> GetLowestScoringPlayer(int id);
    Task<double> GetAveragePlayTime(int id);
}