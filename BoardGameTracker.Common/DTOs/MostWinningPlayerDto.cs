namespace BoardGameTracker.Common.DTOs;

public class MostWinningPlayerDto
{
    public int Id { get; set; }
    public string Image { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalWins { get; set; }
}
