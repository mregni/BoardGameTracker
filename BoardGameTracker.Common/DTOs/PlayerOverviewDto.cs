namespace BoardGameTracker.Common.DTOs;

public class PlayerOverviewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int PlayCount { get; set; }
    public int WinCount { get; set; }
}
