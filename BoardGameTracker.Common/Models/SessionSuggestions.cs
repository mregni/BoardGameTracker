namespace BoardGameTracker.Common.Models;

public class SessionSuggestions
{
    public List<int> RecentGameIds { get; init; } = [];
    public List<int> FrequentPlayerIds { get; init; } = [];
    public int? LastLocationId { get; init; }
}
