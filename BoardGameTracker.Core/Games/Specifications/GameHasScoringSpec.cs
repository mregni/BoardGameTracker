using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class GameHasScoringSpec : Specification<Game, bool?>
{
    public GameHasScoringSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .AsNoTracking();

        Query.Select(x => (bool?)x.HasScoring);
    }
}
