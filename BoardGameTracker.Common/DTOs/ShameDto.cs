namespace BoardGameTracker.Common.DTOs;

public class ShameDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Image { get; set; }
    public DateTime? AdditionDate { get; set; }
    public decimal? Price { get; set; }
    public DateTime? LastSessionDate { get; set; }
    public int PlayCount { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }
    public int? MinPlayTime { get; set; }
    public int? MaxPlayTime { get; set; }
    public int? PlannedGameNightId { get; set; }
    public DateTime? PlannedFor { get; set; }
}

public class ShameStatisticsDto
{
    public int Count { get; set; }
    public decimal? TotalValue { get; set; }
    public decimal? AverageValue { get; set; }
    public int PricedGameCount { get; set; }
    public int Months { get; set; }
    public int OwnedCount { get; set; }
    public decimal CollectionValue { get; set; }
    public int NeverPlayedCount { get; set; }
    public decimal NeverPlayedValue { get; set; }
    public int PlannedCount { get; set; }
    public ShameGameNightDto? NextGameNight { get; set; }
}

public class ShameGameNightDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
}
