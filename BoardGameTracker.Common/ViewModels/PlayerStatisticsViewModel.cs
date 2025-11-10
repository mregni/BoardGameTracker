namespace BoardGameTracker.Common.ViewModels;

public class PlayerStatisticsViewModel
{
    public int PlayCount { get; set; }
    public int WinCount { get; set; }
    public int BestGameId { get; set; }
    public double TotalPlayedTime { get; set; }
    public int DistinctGameCount { get; set; }
    public required BestWinningGameViewModel MostWinsGame { get; set; }
}