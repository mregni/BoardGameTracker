namespace BoardGameTracker.Common.Models.ChangeDetection;

public class ChangeDetectionResult
{
    public ChangeDetectionStatus Status { get; set; } = ChangeDetectionStatus.NotConfigured;
    public bool Available => Status == ChangeDetectionStatus.Ok;
    public bool? InStock { get; set; }
    public decimal? Price { get; set; }
    public DateTime? FetchedAt { get; set; }

    public static ChangeDetectionResult Unavailable(ChangeDetectionStatus status) => new() { Status = status };
}
