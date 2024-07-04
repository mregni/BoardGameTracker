using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Models;

public class MostWinner : Player
{
    public int TotalWins { get; set; }
}