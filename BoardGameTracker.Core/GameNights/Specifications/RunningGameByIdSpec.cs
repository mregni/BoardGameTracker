using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class RunningGameByIdSpec : SingleResultSpecification<RunningGame>
{
    public RunningGameByIdSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .Include(x => x.GameNight);
    }
}
