namespace BoardGameTracker.Common.ViewModels;

public class PlayerStatisticsViewModel
{
    public int PlayCount { get; set; }
    public int WinCount { get; set; }
    public int BestGameId { get; set; }
    public string? FavoriteColor { get; set; }
    public double TotalPlayedTime { get; set; }
}