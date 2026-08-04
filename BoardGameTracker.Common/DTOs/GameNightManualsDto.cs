namespace BoardGameTracker.Common.DTOs;

public class GameNightManualsDto
{
    public int GameId { get; set; }
    public string GameTitle { get; set; } = string.Empty;
    public List<ManualDto> Manuals { get; set; } = [];
}
