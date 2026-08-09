using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class GameNightsOverviewSpec : Specification<GameNight>
{
    public GameNightsOverviewSpec()
    {
        Query
            .Include(x => x.Host)
            .Include(x => x.Location)
            .Include(x => x.SuggestedGames)
            .Include(x => x.InvitedPlayers)
                .ThenInclude(x => x.Player)
            .OrderByDescending(x => x.StartDate)
            .AsNoTracking();
    }
}
