using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class GameNightsHostedByPlayerSpec : Specification<GameNight>
{
    public GameNightsHostedByPlayerSpec(int playerId)
    {
        Query
            .Where(x => x.HostId == playerId)
            .AsNoTracking();
    }
}
