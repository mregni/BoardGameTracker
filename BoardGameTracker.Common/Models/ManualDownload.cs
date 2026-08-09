namespace BoardGameTracker.Common.Models;

public class ManualDownload
{
    public Stream Stream { get; set; } = null!;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
