using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class GameNightStatisticsDto
{
    public GameNightStatisticsPeriod Period { get; set; }
    public int TotalNights { get; set; }
    public int NightsWithSessions { get; set; }
    public int Nights { get; set; }
    public int GamesPlayed { get; set; }
    public double TotalMinutes { get; set; }
    public double AverageTable { get; set; }
    public double? AverageDaysBetweenNights { get; set; }
    public int TiedNights { get; set; }
    public List<NightWinnerStatisticDto> Winners { get; set; } = [];
    public List<NightHistoryDto> History { get; set; } = [];
    public List<NightGameStatisticDto> Games { get; set; } = [];
    public List<PlannedGameStatisticDto> PlannedNeverPlayed { get; set; } = [];
    public GameNightStreaksDto Streaks { get; set; } = new();
    public List<GameNightReferenceDto> RecentNights { get; set; } = [];
    public List<NightAttendanceDto> Attendance { get; set; } = [];
    public GameNightRecordsDto Records { get; set; } = new();
}

public class NightWinnerStatisticDto
{
    public int PlayerId { get; set; }
    public int Rank { get; set; }
    public int NightsWon { get; set; }
    public int NightsAttended { get; set; }
    public int GamesWon { get; set; }
}

public class NightHistoryDto
{
    public int GameNightId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int LocationId { get; set; }
    public int GamesPlayed { get; set; }
    public int MostWins { get; set; }
    public List<int> WinnerIds { get; set; } = [];
    public int WinnerStreak { get; set; }
    public bool CoopOnly { get; set; }
    public bool SharedThroughCoop { get; set; }
}

public class NightGameStatisticDto
{
    public int GameId { get; set; }
    public int TimesPlayed { get; set; }
    public int Nights { get; set; }
    public int TimesPlanned { get; set; }
}

public class PlannedGameStatisticDto
{
    public int GameId { get; set; }
    public int TimesPlanned { get; set; }
}

public class GameNightStreaksDto
{
    public PlayerStreakDto? CurrentWinner { get; set; }
    public PlayerStreakDto? LongestWinner { get; set; }
    public PlayerStreakDto? CurrentAttendance { get; set; }
    public GameStreakDto? LongestGame { get; set; }
}

public class PlayerStreakDto
{
    public int PlayerId { get; set; }
    public int Length { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class GameStreakDto
{
    public int GameId { get; set; }
    public int Length { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public bool IsCurrent { get; set; }
}

public class GameNightReferenceDto
{
    public int GameNightId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
}

public class NightAttendanceDto
{
    public int PlayerId { get; set; }
    public int NightsAttended { get; set; }
    public int NightsHosted { get; set; }
    public List<bool> RecentNights { get; set; } = [];
}

public class GameNightRecordsDto
{
    public GameNightRecordDto? LongestNight { get; set; }
    public GameNightRecordDto? MostGames { get; set; }
    public GameNightRecordDto? BiggestTable { get; set; }
    public GameNightRecordDto? LatestFinish { get; set; }
}

public class GameNightRecordDto
{
    public int GameNightId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int GamesPlayed { get; set; }
    public double Value { get; set; }
    public int? GameId { get; set; }
    public DateTime? At { get; set; }
}
