using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Models;

public class GameStatistics
{
    public int PlayCount { get; set; }
    public TimeSpan TotalPlayedTime { get; set; }
    public double? PricePerPlay { get; set; }
    public int UniquePlayerCount { get; set; }
    public double? HighScore { get; set; }
    public double? AverageScore { get; set; }
    public Player? MostWinsPlayer { get; set; }
} 