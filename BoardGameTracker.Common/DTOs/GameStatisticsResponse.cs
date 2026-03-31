using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Common.DTOs;

public class GameStatisticsResponse
{
    public required GameStatistics GameStats { get; init; }
    public required List<TopPlayerDto> TopPlayers { get; init; }
    public required IEnumerable<PlayByDay> PlayByDayChart { get; init; }
    public required IEnumerable<PlayerCount> PlayerCountChart { get; init; }
    public Dictionary<DateTime, XValue[]>? PlayerScoringChart { get; init; }
    public required List<ScoreRank> ScoreRankChart { get; init; }
}
