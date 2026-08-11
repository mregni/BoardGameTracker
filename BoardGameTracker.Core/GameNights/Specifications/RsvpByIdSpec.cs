using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class RsvpByIdSpec : SingleResultSpecification<GameNightRsvp>
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
