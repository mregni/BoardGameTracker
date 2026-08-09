using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class SessionsByGameSpec : Specification<Session>
{
    public SessionsByGameSpec(int gameId, int? count = null)
    {
        Query
            .Where(x => x.GameId == gameId)
            .Include(x => x.PlayerSessions)
            .OrderByDescending(x => x.Start)
            .AsNoTracking();

        if (count.HasValue)
        {
            Query.Take(count.Value);
        }
    }
}
