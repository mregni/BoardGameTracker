using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models.Charts;

public class GameStateChart
{
    public GameState Type { get; set; }
    public int GameCount { get; set; }
}