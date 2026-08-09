using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class SessionsByPlayerAndGameSpec : Specification<Session>
{
    public SessionsByPlayerAndGameSpec(int playerId, int gameId)
    {
        Query
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == playerId))
            .Where(x => x.GameId == gameId);
    }
}
