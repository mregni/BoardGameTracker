using Ardalis.Specification;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Core.Players.Specifications;

public sealed class WonPlayerSessionsByPlayerSpec : Specification<PlayerSession>
{
    public WonPlayerSessionsByPlayerSpec(int playerId)
    {
        Query
            .Where(x => x.PlayerId == playerId && x.Won);
    }
}
