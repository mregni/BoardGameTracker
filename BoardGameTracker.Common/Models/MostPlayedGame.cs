namespace BoardGameTracker.Common.Models;

public class MostPlayedGame
{
    public int Id { get; set; }
    public string Image { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int TotalWins { get; set; }
    public int TotalSessions { get; set; }
    public double WinningPercentage { get; set; }
}