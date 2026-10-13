using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class GameNightDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int HostId { get; set; }
    public PlayerDto? Host { get; set; }
    public int LocationId { get; set; }
    public Guid LinkId { get; set; }
    public LocationDto? Location { get; set; }
    public List<GameDto> SuggestedGames { get; set; } = [];
    public List<GameNightRsvpDto> InvitedPlayers { get; set; } = [];
    public GameNightSummaryDto Summary { get; set; } = new();
}

public class GameNightSummaryDto
{
    public int GamesPlayed { get; set; }
    public double TotalMinutes { get; set; }
    public int MostWins { get; set; }
    public List<int> WinnerIds { get; set; } = [];
}

public class GameNightDetailDto
{
    public GameNightDto GameNight { get; set; } = new();
    public List<SessionDto> Sessions { get; set; } = [];
    public List<RunningGameDto> RunningGames { get; set; } = [];
}

public class RunningGameDto
{
    public int Id { get; set; }
    public int GameNightId { get; set; }
    public int GameId { get; set; }
    public DateTime Start { get; set; }
    public List<int> PlayerIds { get; set; } = [];
}

public class GameNightRsvpDto
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public PlayerDto? Player { get; set; }
    public int GameNightId { get; set; }
    public GameNightRsvpState State { get; set; }
}