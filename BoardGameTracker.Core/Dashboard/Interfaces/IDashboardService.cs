using BoardGameTracker.Common.DTOs;

namespace BoardGameTracker.Core.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatisticsDto> GetStatistics();
    Task<DashboardChartsDto> GetCharts();
}