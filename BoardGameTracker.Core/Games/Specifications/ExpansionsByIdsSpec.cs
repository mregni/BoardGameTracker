using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class ExpansionsByIdsSpec : Specification<Expansion>
{
    public ExpansionsByIdsSpec(IEnumerable<int> ids)
    {
        Query.Where(x => ids.Contains(x.Id));
    }
}
