using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Models;

public class MostWinningPlayer : Player
{
    public int TotalWins { get; set; }
}