using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class GameByIdSpec : Specification<Game>
{
    public GameByIdSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .AsNoTracking();
    }
}
