namespace BoardGameTracker.Common.Models;

public class GameStatistics
{
    public int PlayCount { get; set; }
    public TimeSpan TotalPlayedTime { get; set; }
    public double? PricePerPlay { get; set; }
    public double? HighScore { get; set; }
    public double? AverageScore { get; set; }
    public MostWinningPlayer? MostWinsPlayer { get; set; }
    public double AveragePlayTime { get; set; }
    public DateTime? LastPlayed { get; set; }
} 