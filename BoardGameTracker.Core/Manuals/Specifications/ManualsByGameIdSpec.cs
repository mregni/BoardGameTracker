using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Manuals.Specifications;

public sealed class ManualsByGameIdSpec : Specification<Manual>
{
    public ManualsByGameIdSpec(int gameId)
    {
        Query
            .Where(m => m.GameId == gameId)
            .OrderBy(m => m.UploadDate);
    }
}
