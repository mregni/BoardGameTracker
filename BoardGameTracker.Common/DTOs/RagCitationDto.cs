namespace BoardGameTracker.Common.DTOs;

public class RagCitationDto
{
    public int ManualId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? Page { get; set; }
    public string Snippet { get; set; } = string.Empty;
    public double Score { get; set; }
    public string? ImageUrl { get; set; }
}
