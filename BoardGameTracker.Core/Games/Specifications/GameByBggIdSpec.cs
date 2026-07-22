using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class GameByBggIdSpec : SingleResultSpecification<Game>
{
    public GameByBggIdSpec(int bggId)
    {
        Query.Where(x => x.BggId == bggId);
    }
}
