using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Compares.Specifications;

public sealed class SessionsWithBothPlayersSpec : Specification<Session>
{
    public SessionsWithBothPlayersSpec(int playerOne, int playerTwo)
    {
        Query
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo))
            .AsNoTracking();
    }
}
