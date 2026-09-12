using BoardGameTracker.Common.Models.ChangeDetection;

namespace BoardGameTracker.Common.DTOs;

public class GamePriceDto
{
    public int GameId { get; set; }
    public string? WatchId { get; set; }
    public bool Available { get; set; }
    public ChangeDetectionStatus Status { get; set; }
    public bool? InStock { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public DateTime? CheckedAt { get; set; }
    public string? ShopUrl { get; set; }
    public bool RecheckQueued { get; set; }
    public DateTime? FetchedAt { get; set; }
}
