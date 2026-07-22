using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class LongestPlayIdSpec : Specification<Session, int?>
{
    public LongestPlayIdSpec(int gameId)
    {
        Query
            .Where(x => x.GameId == gameId)
            .OrderByDescending(x => (x.End - x.Start).TotalSeconds)
            .AsNoTracking();

        Query.Select(x => (int?)x.Id);
    }
}
