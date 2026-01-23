using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Common.ValueObjects;

namespace BoardGameTracker.Common.DTOs;

public class DashboardStatisticsDto
{
    public int TotalGames { get; set; }
    public int ActivePlayers { get; set; }
    public int SessionsPlayed { get; set; }
    public double TotalPlayedTime { get; set; }
    public double? TotalCollectionValue { get; set; }
    public double? AvgGamePrice { get; set; }
    public int ExpansionsOwned { get; set; }
    public double AvgSessionTime { get; set; }

    public List<RecentActivityDto> RecentActivities { get; set; } = [];
    public List<GameStateChart> Collection { get; set; } = [];
    public List<MostPlayedGameDto> MostPlayedGames { get; set; } = [];
    public List<DashboardTopPlayerDto> TopPlayers { get; set; } = [];
    public List<RecentGameDto> RecentAddedGames { get; set; } = [];
    public List<PlayByDay> SessionsByDayOfWeek { get; set; } = [];
}

public class RecentActivityDto
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string GameTitle { get; set; } = string.Empty;
    public string? GameImage { get; set; }
    public DateTime Start { get; set; }
    public int PlayerCount { get; set; }
    public double DurationInMinutes { get; set; }
    public int? WinnerId { get; set; }
    public string? WinnerName { get; set; }
}

public class MostPlayedGameDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int TotalSessions { get; set; }
}

public class DashboardTopPlayerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int PlayCount { get; set; }
    public int WinCount { get; set; }
}

public class RecentGameDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Image { get; set; }
    public DateTime? AdditionDate { get; set; }
    public decimal? Price { get; set; }
}
