using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class ShortestPlayIdSpec : Specification<Session, int?>
{
    public ShortestPlayIdSpec(int gameId)
    {
        Query
            .Where(x => x.GameId == gameId)
            .OrderBy(x => (x.End - x.Start).TotalSeconds)
            .AsNoTracking();

        Query.Select(x => (int?)x.Id);
    }
}
