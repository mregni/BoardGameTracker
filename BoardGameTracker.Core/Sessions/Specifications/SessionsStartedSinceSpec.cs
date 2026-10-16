using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class SessionsStartedSinceSpec : Specification<Session>
{
    public SessionsStartedSinceSpec(DateTime since)
    {
        Query
            .Where(x => x.Start >= since)
            .AsNoTracking();
    }
}
