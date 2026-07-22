using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class GameByIdWithDetailsForReadSpec : SingleResultSpecification<Game>
{
    public GameByIdWithDetailsForReadSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .Include(x => x.Accessories)
            .Include(x => x.Categories)
            .Include(x => x.Expansions)
            .Include(x => x.Mechanics)
            .Include(x => x.People)
            .AsNoTracking();
    }
}
