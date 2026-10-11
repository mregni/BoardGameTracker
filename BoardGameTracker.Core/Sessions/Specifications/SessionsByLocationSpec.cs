using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class SessionsByLocationSpec : Specification<Session>
{
    public SessionsByLocationSpec(int locationId)
    {
        Query
            .Where(x => x.LocationId == locationId)
            .AsNoTracking();
    }
}
