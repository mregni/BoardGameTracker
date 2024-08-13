using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Common.Models.Dashboard;

public class DashboardCharts
{
    public IEnumerable<GameStateChart> GameState { get; set; }
}