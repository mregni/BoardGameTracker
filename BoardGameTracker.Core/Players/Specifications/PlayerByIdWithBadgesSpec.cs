using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Players.Specifications;

public sealed class PlayerByIdWithBadgesSpec : SingleResultSpecification<Player>
{
    public PlayerByIdWithBadgesSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .Include(x => x.Badges)
            .AsNoTracking();
    }
}
