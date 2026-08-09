using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class RecentlyAddedGamesSpec : Specification<Game>
{
    public RecentlyAddedGamesSpec(int count)
    {
        Query
            .Where(x => x.AdditionDate != null)
            .OrderByDescending(x => x.AdditionDate)
            .Take(count)
            .AsNoTracking();
    }
}
