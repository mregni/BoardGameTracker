using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class LeaderboardDto
{
    public LeaderboardEntryDto? MostPlays { get; set; }
    public LeaderboardEntryDto? MostWins { get; set; }
    public LeaderboardEntryDto? BestWinRate { get; set; }
    public LeaderboardEntryDto? MostTimePlayed { get; set; }
    public int MinimumSessions { get; set; }
    public int MinimumPlayersForPodium { get; set; }
    public LeaderboardRankBy RankBy { get; set; }
    public bool ShowPodium { get; set; }
    public int RankedCount { get; set; }
    public int WinCount { get; set; }
    public int CoopWinCount { get; set; }
    public List<LeaderboardEntryDto> Players { get; set; } = [];
}

public class LeaderboardEntryDto
{
    public int? Rank { get; set; }
    public int PlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int PlayCount { get; set; }
    public int WinCount { get; set; }
    public int PodiumCount { get; set; }
    public double WinPercentage { get; set; }
    public double MinutesPlayed { get; set; }
    public bool IsRanked { get; set; }
    public int SessionsToRank { get; set; }
}
