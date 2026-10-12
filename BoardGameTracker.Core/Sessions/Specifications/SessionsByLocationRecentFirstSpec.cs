using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class SessionsByLocationRecentFirstSpec : Specification<Session>
{
    public SessionsByLocationRecentFirstSpec(int locationId)
    {
        Query
            .Where(x => x.LocationId == locationId)
            .Include(x => x.PlayerSessions)
            .OrderByDescending(x => x.Start)
            .AsNoTracking();
    }
}
