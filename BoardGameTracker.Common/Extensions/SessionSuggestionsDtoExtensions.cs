using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Common.Extensions;

public static class SessionSuggestionsDtoExtensions
{
    public static SessionSuggestionsDto ToDto(this SessionSuggestions suggestions)
    {
        return new SessionSuggestionsDto
        {
            RecentGameIds = suggestions.RecentGameIds.ToList(),
            FrequentPlayerIds = suggestions.FrequentPlayerIds.ToList(),
            LastLocationId = suggestions.LastLocationId
        };
    }
}
