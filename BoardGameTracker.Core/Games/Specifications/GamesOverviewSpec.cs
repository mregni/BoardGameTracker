using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class GamesOverviewSpec : Specification<Game>
{
    public GamesOverviewSpec()
    {
        Query
            .Include(x => x.Expansions)
            .Include(x => x.Categories)
            .OrderBy(x => x.Title)
            .AsNoTracking();
    }
}
