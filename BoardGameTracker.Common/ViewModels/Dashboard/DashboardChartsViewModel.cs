namespace BoardGameTracker.Common.ViewModels.Dashboard;

public class DashboardChartsViewModel
{
    public IEnumerable<GameStateChartViewModel> GameState { get; set; }
}