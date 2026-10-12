using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class LastUsedLocationSpec : Specification<Session, int?>
{
    public LastUsedLocationSpec()
    {
        Query
            .Where(x => x.LocationId != null)
            .OrderByDescending(x => x.Start)
            .AsNoTracking();

        Query.Select(x => x.LocationId);
    }
}
