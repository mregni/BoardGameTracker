using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Manuals.Specifications;

public sealed class ManualsByGameIdsSpec : Specification<Manual>
{
    public ManualsByGameIdsSpec(IEnumerable<int> gameIds)
    {
        Query
            .Where(m => gameIds.Contains(m.GameId))
            .OrderBy(m => m.UploadDate);
    }
}
