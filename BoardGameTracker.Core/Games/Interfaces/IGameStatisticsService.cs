using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameStatisticsService
{
    Task<GameStatistics> CalculateStatisticsAsync(int gameId);
}
