using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;

namespace BoardGameTracker.Core.Sessions;

public class SessionSuggestionService : ISessionSuggestionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IPlayerRepository _playerRepository;

    public SessionSuggestionService(ISessionRepository sessionRepository, IPlayerRepository playerRepository)
    {
        _sessionRepository = sessionRepository;
        _playerRepository = playerRepository;
    }

    public async Task<SessionSuggestionsDto> GetSuggestions(CancellationToken cancellationToken = default)
    {
        var recentGameIds = await _sessionRepository.GetRecentlyPlayedGameIds(Constants.SessionForm.RecentGamesCount, cancellationToken);
        var frequentPlayers = await _playerRepository.GetTopPlayers(Constants.SessionForm.FrequentPlayersCount, cancellationToken);
        var lastLocationId = await _sessionRepository.FirstOrDefaultAsync(new LastUsedLocationSpec(), cancellationToken);

        return new SessionSuggestionsDto
        {
            RecentGameIds = recentGameIds,
            FrequentPlayerIds = frequentPlayers.Select(x => x.Id).ToList(),
            LastLocationId = lastLocationId
        };
    }
}
