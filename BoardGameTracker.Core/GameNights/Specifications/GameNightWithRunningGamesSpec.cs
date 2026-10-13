using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class GameNightWithRunningGamesSpec : SingleResultSpecification<GameNight>
{
    public GameNightWithRunningGamesSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .Include(x => x.RunningGames);
    }
}
