using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Common.DTOs;

public class DashboardChartsDto
{
    public IEnumerable<GameStateChart> GameState { get; set; } = [];
}
