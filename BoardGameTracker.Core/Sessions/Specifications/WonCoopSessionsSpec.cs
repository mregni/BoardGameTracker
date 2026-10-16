using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Sessions.Specifications;

public sealed class WonCoopSessionsSpec : Specification<Session>
{
    public WonCoopSessionsSpec()
    {
        Query
            .Where(x => x.IsCoop && x.TeamWon == true)
            .AsNoTracking();
    }
}
