using BoardGameTracker.Common.Models.Dashboard;

namespace BoardGameTracker.Core.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashbardStatistics> GetStatistics();
    Task<DashboardCharts> GetCharts();
}