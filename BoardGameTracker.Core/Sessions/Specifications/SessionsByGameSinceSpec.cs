using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class SessionsByGameSinceSpec : Specification<Session>
{
    public SessionsByGameSinceSpec(int gameId, DateTime cutoff)
    {
        Query
            .Where(x => x.GameId == gameId && x.Start > cutoff)
            .Include(x => x.PlayerSessions)
            .OrderBy(x => x.Start)
            .AsNoTracking();
    }
}
