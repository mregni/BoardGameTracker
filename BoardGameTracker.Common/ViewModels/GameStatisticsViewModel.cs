﻿namespace BoardGameTracker.Common.ViewModels;

public class GameStatisticsViewModel
{
    public int PlayCount { get; set; }
    public int TotalPlayedTime { get; set; }
    public double? PricePerPlay { get; set; }
    public int UniquePlayerCount { get; set; }
    public double? HighScore { get; set; }
    public double? AverageScore { get; set; }
    public PlayerViewModel? MostWinsPlayer { get; set; }
}