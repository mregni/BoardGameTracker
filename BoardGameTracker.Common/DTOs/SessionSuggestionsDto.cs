namespace BoardGameTracker.Common.DTOs;

public class SessionSuggestionsDto
{
    public List<int> RecentGameIds { get; set; } = [];
    public List<int> FrequentPlayerIds { get; set; } = [];
    public int? LastLocationId { get; set; }
}
