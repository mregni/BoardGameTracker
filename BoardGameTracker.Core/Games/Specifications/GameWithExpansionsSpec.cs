using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class GameWithExpansionsSpec : SingleResultSpecification<Game>
{
    public GameWithExpansionsSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .Include(x => x.Expansions);
    }
}
