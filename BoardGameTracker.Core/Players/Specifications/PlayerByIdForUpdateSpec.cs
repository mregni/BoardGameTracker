using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Players.Specifications;

public sealed class PlayerByIdForUpdateSpec : SingleResultSpecification<Player>
{
    public PlayerByIdForUpdateSpec(int id)
    {
        Query.Where(x => x.Id == id);
    }
}
