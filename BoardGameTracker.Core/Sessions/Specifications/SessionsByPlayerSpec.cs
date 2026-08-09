using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class SessionsByPlayerSpec : Specification<Session>
{
    public SessionsByPlayerSpec(int playerId, bool? won = null)
    {
        Query
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == playerId))
            .Include(x => x.PlayerSessions);

        if (won.HasValue)
        {
            Query.Where(x => x.PlayerSessions.Any(y => y.PlayerId == playerId && y.Won == won.Value));
        }
    }
}
