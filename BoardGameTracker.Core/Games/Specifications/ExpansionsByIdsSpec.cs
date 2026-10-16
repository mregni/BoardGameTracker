using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class ExpansionsByIdsSpec : Specification<Expansion>
{
    public ExpansionsByIdsSpec(int gameId, IEnumerable<int> ids)
    {
        Query.Where(x => x.GameId == gameId && ids.Contains(x.Id));
    }
}
