using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.GameNights.Interfaces;

public interface IGameNightStatisticsService
{
    Task<GameNightStatisticsDto> GetStatistics(GameNightStatisticsPeriod period, CancellationToken cancellationToken = default);
}
