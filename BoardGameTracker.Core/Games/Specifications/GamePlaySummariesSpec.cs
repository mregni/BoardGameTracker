using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class GamePlaySummariesSpec : Specification<Game, GamePlaySummary>
{
    public GamePlaySummariesSpec()
    {
        Query
            .Where(g => g.Sessions.Any())
            .AsNoTracking();

        Query.Select(g => new GamePlaySummary
        {
            GameId = g.Id,
            PlayCount = g.Sessions.Count,
            LastPlayed = g.Sessions.Max(s => (DateTime?)s.Start)
        });
    }
}
