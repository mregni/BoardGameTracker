namespace BoardGameTracker.Common.DTOs;

public class LocationStatisticsDto
{
    public int DistinctGameCount { get; set; }
    public int DistinctPlayerCount { get; set; }
    public double TotalPlayedTime { get; set; }
}
