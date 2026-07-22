using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Players.Specifications;

public sealed class PlayersOrderedByNameSpec : Specification<Player>
{
    public PlayersOrderedByNameSpec()
    {
        Query
            .OrderBy(x => x.Name)
            .AsNoTracking();
    }
}
