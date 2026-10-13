using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class GameNightByIdSpec : SingleResultSpecification<GameNight>
{
    public GameNightByIdSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .AsNoTracking();
    }
}
