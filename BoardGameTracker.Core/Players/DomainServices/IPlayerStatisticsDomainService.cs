using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Players.DomainServices;

public interface IPlayerStatisticsDomainService
{
    Task<PlayerStatistics> CalculateStatisticsAsync(int playerId);
}
