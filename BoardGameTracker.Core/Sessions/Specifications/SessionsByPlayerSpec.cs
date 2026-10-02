using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class SessionsByPlayerSpec : Specification<Session>
{
    public SessionsByPlayerSpec(int playerId)
    {
        Query
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == playerId))
            .Include(x => x.PlayerSessions);
    }
}
