namespace BoardGameTracker.Common.DTOs;

public class SessionDto
{
    public int Id { get; set; }
    public string Comment { get; set; } = string.Empty;
    public int GameId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public double Minutes { get; set; }
    public int? LocationId { get; set; }
    public List<PlayerSessionDto> PlayerSessions { get; set; } = [];
    public List<ExpansionDto> Expansions { get; set; } = [];
}
