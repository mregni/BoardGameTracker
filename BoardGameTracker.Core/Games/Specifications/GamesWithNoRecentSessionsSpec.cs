using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class GamesWithNoRecentSessionsSpec : Specification<Game>
{
    public GamesWithNoRecentSessionsSpec(DateTime cutoffDate)
    {
        Query
            .Where(g => g.State == GameState.Owned && !g.Sessions.Any(s => s.Start >= cutoffDate))
            .OrderBy(g => g.Title)
            .AsNoTracking();
    }
}
