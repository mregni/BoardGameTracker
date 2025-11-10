using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Common.ViewModels;

public class GameStatisticsViewModel
{
    public GameStatsViewModel GameStats { get; set; } = null!;
    public IEnumerable<TopPlayerViewModel> TopPlayers { get; set; } = [];
    public IEnumerable<PlayByDayChartViewModel> PlayByDayChart{ get; set; } = [];
    public IEnumerable<PlayerCountChartViewModel> PlayerCountChart { get; set; } = [];
    public IEnumerable<PlayerScoringChartViewModel> PlayerScoringChart { get; set; } = [];
    public IEnumerable<ScoreRankChartViewModel> ScoreRankChart { get; set; } = [];
}

public class GameStatsViewModel
{
    public int PlayCount { get; set; }
    public int TotalPlayedTime { get; set; }
    public double? PricePerPlay { get; set; }
    public double? HighScore { get; set; }
    public double? AverageScore { get; set; }
    public DateTime? LastPlayed { get; set; }
    public MostWinnerViewModel? MostWinsPlayer { get; set; }
    public double AveragePlayTime { get; set; }
    public int? ExpansionCount { get; set; }
}

public class TopPlayerViewModel
{
    public int PlayerId { get; set; }
    public int PlayCount { get; set; }
    public int Wins { get; set; }
    public double WinPercentage { get; set; }
    public Trend Trend { get; set; }
}

public class PlayByDayChartViewModel
{
    public int DayOfWeek { get; set; }
    public int PlayCount { get; set; }
}

public class PlayerCountChartViewModel
{
    public int Players { get; set; }
    public int PlayCount { get; set; }
}

public class PlayerScoringChartViewModel
{
    public DateTime DateTime { get; set; }
    public required XValue[] Series { get; set; }
}

public class ScoreRankChartViewModel
{
    public required string Key { get; set; }
    public double Score { get; set; }
    public int PlayerId { get; set; }
}