using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Players.Specifications;

public sealed class PlayerIdsSpec : Specification<Player, int>
{
    public PlayerIdsSpec(IEnumerable<int> ids)
    {
        var idList = ids.ToList();
        Query
            .Where(x => idList.Contains(x.Id))
            .AsNoTracking();

        Query.Select(x => x.Id);
    }
}
