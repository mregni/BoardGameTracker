using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class GameNightByIdWithDetailsSpec : SingleResultSpecification<GameNight>
{
    public GameNightByIdWithDetailsSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .Include(x => x.Host)
            .Include(x => x.Location)
            .Include(x => x.SuggestedGames)
            .Include(x => x.InvitedPlayers)
                .ThenInclude(x => x.Player);
    }
}
