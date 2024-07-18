using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Models;

public class BestWinningGame : Game
{
    public int TotalWins { get; set; }
}