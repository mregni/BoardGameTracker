using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameChartService
{
    Task<IEnumerable<PlayByDay>> GetPlayByDayChart(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PlayerCount>> GetPlayerCountChart(int id, CancellationToken cancellationToken = default);
    Task<List<PlayerScoringPoint>?> GetPlayerScoringChart(int id, CancellationToken cancellationToken = default);
    Task<List<ScoreRank>> GetScoringRankedChart(int id, double? averageScore, CancellationToken cancellationToken = default);
    Task<List<TopPlayerDto>> GetTopPlayers(int id, CancellationToken cancellationToken = default);
}
