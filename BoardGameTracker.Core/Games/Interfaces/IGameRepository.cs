using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameRepository: ICrudHelper<Game>
{
    Task AddGameCategoriesIfNotExists(IEnumerable<GameCategory> categories);
    Task AddGameMechanicsIfNotExists(IEnumerable<GameMechanic> mechanics);
    Task AddPeopleIfNotExists(IEnumerable<Person> people);
    Task<Game?> GetGameByBggId(int bggId);
    Task<List<Game>> GetGamesOverviewList();
    Task<List<Session>> GetSessions(int id, int skip, int? take);
    Task<List<Session>> GetSessions(int id, int dayCount);
    Task<int> GetPlayCount(int id);
    Task<TimeSpan> GetTotalPlayedTime(int id);
    Task<double?> GetPricePerPlay(int id);
    Task<DateTime?> GetLastPlayedDateTime(int id);
    Task<double?> GetHighestScore(int id);
    Task<Player?> GetMostWins(int id);
    Task<Player?> GetMostWins();
    Task<double?> GetAverageScore(int id);
    Task<int> CountAsync();
    Task<int?> GetShortestPlay(int id);
    Task<int?> GetLongestPlay(int id);
    Task<int?> GetHighScorePlay(int id);
    Task<int?> GetLowestScorePlay(int id);
    Task<int> GetTotalPlayCount(int id);
    Task<List<IGrouping<DayOfWeek,Session>>> GetPlayByDayChart(int id);
    Task<List<IGrouping<int, int>>> GetPlayerCountChart(int id);
    Task<PlayerSession?> GetHighestScoringPlayer(int id);
    Task<PlayerSession?> GetHighestLosingPlayer(int id);
    Task<PlayerSession?> GetLowestWinning(int id);
    Task<PlayerSession?> GetLowestScoringPlayer(int id);
    Task<double> GetAveragePlayTime(int id);
    Task<double?> GetMeanPayedAsync();
    Task<double?> GetTotalPayedAsync();
    Task<List<IGrouping<GameState, Game>>> GetGamesGroupedByState();
    Task<List<Session>> GetSessionsByGameId(int id);
}