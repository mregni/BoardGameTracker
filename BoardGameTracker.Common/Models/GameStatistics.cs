namespace BoardGameTracker.Common.Models;

public class GameStatistics
{
    public int PlayCount { get; set; }
    public double TotalPlayedTime { get; set; }
    public double? PricePerPlay { get; set; }
    public double? HighScore { get; set; }
    public double? AverageScore { get; set; }
    public MostWinningPlayer? MostWinsPlayer { get; set; }
    public double AveragePlayTime { get; set; }
    public DateTime? LastPlayed { get; set; }
    public int? ExpansionCount { get; set; }
}

public class MostWinningPlayer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public int TotalWins { get; set; }
}
