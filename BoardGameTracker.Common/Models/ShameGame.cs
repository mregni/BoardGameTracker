namespace BoardGameTracker.Common.Models;

public class ShameGame
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Image { get; set; }
    public DateTime? AdditionDate { get; set; }
    public decimal? Price { get; set; }
    public DateTime? LastSessionDate { get; set; }
}

public class ShameStatistics
{
    public int Count { get; set; }
    public decimal? TotalValue { get; set; }
    public decimal? AverageValue { get; set; }
}
