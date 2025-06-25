namespace BoardGameTracker.Common.Models.Dashboard;

public class DashboardStatistics
{
    public int GameCount { get; set; }
    public int ExpansionCount { get; set; }
    public int PlayerCount { get; set; }
    public int LocationCount { get; set; }
    public int SessionCount { get; set; }
    
    public double? TotalCost { get; set; }
    public double? MeanPayed { get; set; }
    public double TotalPlayTime { get; set; }
    public double MeanPlayTime { get; set; }
    public MostWinningPlayer MostWinningPlayer { get; set; }
}