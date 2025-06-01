using BoardGameTracker.Common.Models.Dashboard;

namespace BoardGameTracker.Core.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatistics> GetStatistics();
    Task<DashboardCharts> GetCharts();
}