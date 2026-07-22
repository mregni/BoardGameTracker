using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class RecentSessionsSpec : Specification<Session>
{
    public RecentSessionsSpec(int count)
    {
        Query
            .Include(x => x.Game)
            .Include(x => x.PlayerSessions)
                .ThenInclude(ps => ps.Player)
            .OrderByDescending(x => x.Start)
            .Take(count)
            .AsNoTracking();
    }
}
