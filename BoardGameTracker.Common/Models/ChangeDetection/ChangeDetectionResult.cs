namespace BoardGameTracker.Common.Models.ChangeDetection;

public class ChangeDetectionResult
{
    public bool Available { get; set; }
    public bool? InStock { get; set; }
    public decimal? Price { get; set; }
    public DateTime? FetchedAt { get; set; }

    public static ChangeDetectionResult Unavailable() => new() { Available = false };
}
