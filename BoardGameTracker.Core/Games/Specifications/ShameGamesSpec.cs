using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Games.Specifications;

public sealed class ShameGamesSpec : Specification<Game, ShameGame>
{
    public ShameGamesSpec(DateTime cutoffDate)
    {
        Query
            .Where(g => g.State == GameState.Owned && !g.Sessions.Any(s => s.Start >= cutoffDate))
            .OrderBy(g => g.Title)
            .AsNoTracking();

        Query.Select(g => new ShameGame
        {
            Id = g.Id,
            Title = g.Title,
            Image = g.Image,
            AdditionDate = g.AdditionDate,
            Price = g.BuyingPrice != null ? g.BuyingPrice.Amount : null,
            LastSessionDate = g.Sessions
                .OrderByDescending(s => s.Start)
                .Select(s => (DateTime?)s.Start)
                .FirstOrDefault()
        });
    }
}
