using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class RsvpByPlayerAndGameNightSpec : Specification<GameNightRsvp>
{
    public RsvpByPlayerAndGameNightSpec(int playerId, int gameNightId)
    {
        Query
            .Where(x => x.GameNightId == gameNightId && x.PlayerId == playerId)
            .Include(x => x.Player)
            .Include(x => x.GameNight)
            .ThenInclude(x => x.Host);
    }
}
