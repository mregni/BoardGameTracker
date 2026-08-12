using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class ManualDto
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string Title { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadDate { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public ManualIndexStatus IndexStatus { get; set; }
    public int IndexedChunkCount { get; set; }
    public string? IndexError { get; set; }
    public DateTime? IndexedDate { get; set; }
}
