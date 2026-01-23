using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameStatisticsRepository
{
    Task<double?> GetPricePerPlay(int gameId);
    Task<double?> GetHighestScore(int gameId);
    Task<Player?> GetMostWins(int gameId);
    Task<Player?> GetMostWins();
    Task<double?> GetAverageScore(int gameId);
    Task<int?> GetExpansionCount(int gameId);
    Task<double> GetAveragePlayTime(int gameId);
    Task<double?> GetMeanPayedAsync();
    Task<double?> GetTotalPayedAsync();
    Task<List<IGrouping<GameState, Game>>> GetGamesGroupedByState();
    Task<int?> GetHighScorePlay(int gameId);
    Task<int?> GetLowestScorePlay(int gameId);
    Task<List<IGrouping<DayOfWeek, Session>>> GetPlayByDayChart(int gameId);
    Task<List<IGrouping<int, int>>> GetPlayerCountChart(int gameId);
    Task<PlayerSession?> GetHighestScoringPlayer(int gameId);
    Task<PlayerSession?> GetHighestLosingPlayer(int gameId);
    Task<PlayerSession?> GetLowestWinning(int gameId);
    Task<PlayerSession?> GetLowestScoringPlayer(int gameId);
    Task<List<(int GameId, string Title, string? Image, int PlayCount)>> GetMostPlayedGames(int count);
}
