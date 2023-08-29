namespace BoardGameTracker.Common.Models;

public class PlayerStatistics
{
    public int PlayCount { get; set; }
    public int WinCount { get; set; }
    public int BestGameId { get; set; }
    public string? FavoriteColor { get; set; }
    public double TotalPlayedTime { get; set; }
}