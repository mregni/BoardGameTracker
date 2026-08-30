namespace BoardGameTracker.Common.DTOs;

public class GamePriceDto
{
    public int GameId { get; set; }
    public string? WatchId { get; set; }
    public bool Available { get; set; }
    public bool? InStock { get; set; }
    public decimal? Price { get; set; }
    public DateTime? FetchedAt { get; set; }
}
