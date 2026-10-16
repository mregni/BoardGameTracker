using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class GameHasCoopModeSpec : Specification<Game, bool?>
{
    public GameHasCoopModeSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .AsNoTracking();

        Query.Select(x => (bool?)x.HasCoopMode);
    }
}
