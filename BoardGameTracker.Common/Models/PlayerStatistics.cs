namespace BoardGameTracker.Common.Models;

public class PlayerStatistics
{
    public int PlayCount { get; set; }
    public int WinCount { get; set; }
    public double TotalPlayedTime { get; set; }
    public int DistinctGameCount { get; set; }
    public List<MostPlayedGame> MostPlayedGames { get; set; } = [];
}