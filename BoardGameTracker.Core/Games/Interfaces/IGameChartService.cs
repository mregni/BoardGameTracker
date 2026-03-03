using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameChartService
{
    Task<IEnumerable<PlayByDay>> GetPlayByDayChart(int id);
    Task<IEnumerable<PlayerCount>> GetPlayerCountChart(int id);
    Task<Dictionary<DateTime, XValue[]>?> GetPlayerScoringChart(int id);
    Task<List<ScoreRank>> GetScoringRankedChart(int id);
    Task<List<TopPlayerDto>> GetTopPlayers(int id);
}
