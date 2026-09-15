using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameStatisticsRepository
{
    Task<decimal?> GetPricePerPlay(int gameId, CancellationToken cancellationToken = default);
    Task<double?> GetHighestScore(int gameId, CancellationToken cancellationToken = default);
    Task<(Player? Player, int WinCount)> GetMostWins(int gameId, CancellationToken cancellationToken = default);
    Task<double?> GetAverageScore(int gameId, CancellationToken cancellationToken = default);
    Task<int?> GetExpansionCount(int gameId, CancellationToken cancellationToken = default);
    Task<double> GetAveragePlayTime(int gameId, CancellationToken cancellationToken = default);
    Task<double> GetTotalPlayedTime(int gameId, CancellationToken cancellationToken = default);
    Task<decimal?> GetMeanPayedAsync(CancellationToken cancellationToken = default);
    Task<decimal?> GetTotalPayedAsync(CancellationToken cancellationToken = default);
    Task<List<IGrouping<GameState, Game>>> GetGamesGroupedByState(CancellationToken cancellationToken = default);
    Task<List<DateTime>> GetSessionStartTimes(int gameId, CancellationToken cancellationToken = default);
    Task<List<IGrouping<int, int>>> GetPlayerCountChart(int gameId, CancellationToken cancellationToken = default);
    Task<PlayerSession?> GetHighestScoringPlayer(int gameId, CancellationToken cancellationToken = default);
    Task<PlayerSession?> GetHighestLosingPlayer(int gameId, CancellationToken cancellationToken = default);
    Task<PlayerSession?> GetLowestWinning(int gameId, CancellationToken cancellationToken = default);
    Task<PlayerSession?> GetLowestScoringPlayer(int gameId, CancellationToken cancellationToken = default);
    Task<List<(int GameId, string Title, string? Image, int PlayCount)>> GetMostPlayedGames(int count, CancellationToken cancellationToken = default);
}
