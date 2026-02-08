using BoardGameTracker.Common.Entities;
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
}

public class GameNightRsvpDto
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public PlayerDto? Player { get; set; }
    public int GameNightId { get; set; }
    public GameNightRsvpState State { get; set; }
}