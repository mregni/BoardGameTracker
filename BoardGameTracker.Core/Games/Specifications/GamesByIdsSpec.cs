using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class GamesByIdsSpec : Specification<Game>
{
    public GamesByIdsSpec(IEnumerable<int> ids)
    {
        Query.Where(g => ids.Contains(g.Id));
    }
}
