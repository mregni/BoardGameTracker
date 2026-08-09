using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Badges.Specifications;

public sealed class BadgesByPlayerSpec : Specification<Badge>
{
    public BadgesByPlayerSpec(int playerId)
    {
        Query.Where(x => x.Players.Any(p => p.Id == playerId));
    }
}
