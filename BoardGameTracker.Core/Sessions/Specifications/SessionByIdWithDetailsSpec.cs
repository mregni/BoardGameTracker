using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class SessionByIdWithDetailsSpec : Specification<Session>
{
    public SessionByIdWithDetailsSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .Include(x => x.PlayerSessions)
            .Include(x => x.Expansions);
    }
}
