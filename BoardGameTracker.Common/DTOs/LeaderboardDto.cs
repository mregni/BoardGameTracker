namespace BoardGameTracker.Common.DTOs;

public class LeaderboardDto
{
    public LeaderboardEntryDto? MostPlays { get; set; }
    public LeaderboardEntryDto? MostWins { get; set; }
    public LeaderboardEntryDto? BestWinRate { get; set; }
    public LeaderboardEntryDto? MostTimePlayed { get; set; }
    public int MinimumPlaysForWinRate { get; set; }
    public List<LeaderboardEntryDto> Players { get; set; } = [];
}

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public int PlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int PlayCount { get; set; }
    public int WinCount { get; set; }
    public int PodiumCount { get; set; }
    public double WinPercentage { get; set; }
    public double MinutesPlayed { get; set; }
}
