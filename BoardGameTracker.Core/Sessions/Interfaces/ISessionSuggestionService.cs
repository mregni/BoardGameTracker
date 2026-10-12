using BoardGameTracker.Common.DTOs;

namespace BoardGameTracker.Core.Sessions.Interfaces;

public interface ISessionSuggestionService
{
    Task<SessionSuggestionsDto> GetSuggestions(CancellationToken cancellationToken = default);
}
