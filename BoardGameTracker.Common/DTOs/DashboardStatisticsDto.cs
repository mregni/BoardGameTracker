using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Common.DTOs;

public class DashboardStatisticsDto
{
    public int TotalGames { get; set; }
    public int OwnedGames { get; set; }
    public int ActivePlayers { get; set; }
    public int SessionsPlayed { get; set; }
    public double TotalPlayedTime { get; set; }
    public decimal? TotalCollectionValue { get; set; }
    public int PricedGameCount { get; set; }
    public int ExpansionsOwned { get; set; }
    public double AvgSessionTime { get; set; }
    public int SessionsThisMonth { get; set; }
    public int ActivePlayersThisMonth { get; set; }

    public DashboardLinkedPlayerDto? LinkedPlayer { get; set; }
    public DashboardGameNightDto? NextGameNight { get; set; }
    public int PlannedGameNights { get; set; }
    public DashboardShelfOfShameDto? ShelfOfShame { get; set; }

    public List<RecentActivityDto> RecentActivities { get; set; } = [];
    public List<GameStateChart> Collection { get; set; } = [];
    public List<MostPlayedGameDto> MostPlayedGames { get; set; } = [];
    public List<MostPlayedGameDto> MostPlayedGamesThisYear { get; set; } = [];
    public List<MostPlayedGameDto> MostPlayedGamesLast12Months { get; set; } = [];
    public List<DashboardTopPlayerDto> TopPlayers { get; set; } = [];
    public bool TopPlayersIncludeCoopWins { get; set; }
    public List<RecentGameDto> RecentAddedGames { get; set; } = [];
}

public class DashboardLinkedPlayerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SessionsThisMonth { get; set; }
    public int WinsThisMonth { get; set; }
}

public class DashboardGameNightDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int HostId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public int AcceptedCount { get; set; }
    public int MaybeCount { get; set; }
    public int PendingCount { get; set; }
    public int DeclinedCount { get; set; }
    public List<DashboardGameDto> SuggestedGames { get; set; } = [];
}

public class DashboardGameDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Image { get; set; }
}

public class DashboardShelfOfShameDto
{
    public int Count { get; set; }
    public int NeverPlayedCount { get; set; }
    public decimal? TotalValue { get; set; }
}

public class DashboardWinnerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
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
    public List<DashboardWinnerDto> Winners { get; set; } = [];
    public bool IsCoop { get; set; }
    public bool? TeamWon { get; set; }
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
    public int Rank { get; set; }
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
