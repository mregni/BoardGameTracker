using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Games.DomainServices;

public interface IGameStatisticsDomainService
{
    Task<GameStatistics> CalculateStatisticsAsync(int gameId);
}
