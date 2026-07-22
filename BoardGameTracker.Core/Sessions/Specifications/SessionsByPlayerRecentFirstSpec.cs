using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class SessionsByPlayerRecentFirstSpec : Specification<Session>
{
    public SessionsByPlayerRecentFirstSpec(int playerId, int? count = null)
    {
        Query
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == playerId))
            .Include(x => x.PlayerSessions)
            .OrderByDescending(x => x.Start)
            .AsNoTracking();

        if (count.HasValue)
        {
            Query.Take(count.Value);
        }
    }
}
