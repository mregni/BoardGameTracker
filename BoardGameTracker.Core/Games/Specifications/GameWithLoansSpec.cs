using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class GameWithLoansSpec : SingleResultSpecification<Game>
{
    public GameWithLoansSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .Include(x => x.Loans);
    }
}
