using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class WantedGamesWithWatchIdSpec : Specification<Game>
{
    public WantedGamesWithWatchIdSpec()
    {
        Query
            .Where(x => x.State == GameState.Wanted && x.ChangeDetectionWatchId != null)
            .AsNoTracking();
    }
}
