using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class SessionsByGamePagedSpec : Specification<Session>
{
    public SessionsByGamePagedSpec(int gameId, int skip, int? take)
    {
        Query
            .Where(x => x.GameId == gameId)
            .Include(x => x.Location)
            .Include(x => x.PlayerSessions)
                .ThenInclude(x => x.Player)
            .OrderByDescending(x => x.Start)
            .Skip(skip)
            .AsNoTracking();

        if (take.HasValue)
        {
            Query.Take(take.Value);
        }
    }
}
