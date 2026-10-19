using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class ScoredSessionsByGameSpec : Specification<Session>
{
    public ScoredSessionsByGameSpec(int gameId)
    {
        Query
            .Where(x => x.GameId == gameId && x.PlayerSessions.Any(ps => ps.Score != null))
            .AsNoTracking();
    }
}
