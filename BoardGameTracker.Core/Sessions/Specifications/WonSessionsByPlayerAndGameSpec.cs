using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class WonSessionsByPlayerAndGameSpec : Specification<Session>
{
    public WonSessionsByPlayerAndGameSpec(int playerId, int gameId)
    {
        Query
            .Where(x => x.GameId == gameId && x.PlayerSessions.Any(y => y.PlayerId == playerId && y.Won));
    }
}
