using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class LastPlayedDateSpec : Specification<Session, DateTime?>
{
    public LastPlayedDateSpec(int gameId)
    {
        Query
            .Where(x => x.GameId == gameId)
            .OrderByDescending(x => x.Start)
            .AsNoTracking();

        Query.Select(x => (DateTime?)x.Start);
    }
}
