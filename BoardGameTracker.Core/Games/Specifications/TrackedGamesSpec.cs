using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class TrackedGamesSpec : Specification<Game>
{
    public TrackedGamesSpec()
    {
        Query
            .Where(x => x.ChangeDetectionWatchId != null)
            .AsNoTracking();
    }
}
