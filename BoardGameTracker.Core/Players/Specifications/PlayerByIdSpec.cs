using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Players.Specifications;

public sealed class PlayerByIdSpec : Specification<Player>
{
    public PlayerByIdSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .AsNoTracking();
    }
}
