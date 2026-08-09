using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class RsvpByIdSpec : Specification<GameNightRsvp>
{
    public RsvpByIdSpec(int rsvpId)
    {
        Query
            .Where(x => x.Id == rsvpId)
            .Include(x => x.Player)
            .Include(x => x.GameNight)
            .ThenInclude(x => x.Host);
    }
}
