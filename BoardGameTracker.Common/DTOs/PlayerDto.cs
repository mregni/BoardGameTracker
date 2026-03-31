namespace BoardGameTracker.Common.DTOs;

public class PlayerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public List<BadgeDto>? Badges { get; set; }
}
