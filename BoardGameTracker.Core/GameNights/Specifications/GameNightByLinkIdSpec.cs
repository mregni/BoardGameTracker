using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class GameNightByLinkIdSpec : SingleResultSpecification<GameNight>
{
    public GameNightByLinkIdSpec(Guid linkId)
    {
        Query
            .Where(x => x.LinkId == linkId)
            .Include(x => x.Host)
            .Include(x => x.Location)
            .Include(x => x.SuggestedGames)
            .Include(x => x.InvitedPlayers)
                .ThenInclude(x => x.Player);
    }
}
