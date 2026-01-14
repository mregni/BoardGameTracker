namespace BoardGameTracker.Common.Models;

public class MostWinningPlayer
{
    public int Id { get; set; }
    public string Image { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalWins { get; set; }
}