using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Manuals.Specifications;

public sealed class GameIdsWithManualsSpec : Specification<Manual, int>
{
    public GameIdsWithManualsSpec()
    {
        Query.AsNoTracking();
        Query.Select(m => m.GameId);
    }
}
