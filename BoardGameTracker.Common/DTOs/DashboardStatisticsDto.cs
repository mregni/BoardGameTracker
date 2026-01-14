namespace BoardGameTracker.Common.DTOs;

public class DashboardStatisticsDto
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
    public MostWinningPlayerDto? MostWinningPlayer { get; set; }
}
