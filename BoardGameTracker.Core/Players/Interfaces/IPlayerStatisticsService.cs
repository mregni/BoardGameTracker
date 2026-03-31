using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerStatisticsService
{
    Task<PlayerStatistics> CalculateStatisticsAsync(int playerId);
}
