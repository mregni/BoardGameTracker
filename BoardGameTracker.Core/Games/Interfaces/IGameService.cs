using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameService
{
    Task<Game> ProcessBggGameData(BggGame rawGame, BggSearch gameState);
    Task<Game?> GetGameByBggId(int bggId);
    Task<List<Game>> GetGames();
    Task<Game?> GetGameById(int id);
    Task Delete(int id);
    Task<GameStatistics> GetStats(int id);
    Task<int> CountAsync();
    Task<BggGame?> SearchAndCreateGame(int searchBggId);
    Task<List<TopPlayer>> GetTopPlayers(int id);
    Task<Dictionary<SessionFlag, int?>> GetPlayFlags(int id);
    Task<int> GetTotalPlayCount(int id);
    Task<IEnumerable<PlayByDay>> GetPlayByDayChart(int id);
    Task<IEnumerable<PlayerCount>> GetPlayerCountChart(int id);
    Task<Dictionary<DateTime, XValue[]>> GetPlayerScoringChart(int id);
    Task<List<ScoreRank>> GetScoringRankedChart(int id);
    Task<Game> CreateGame(Game game);
}