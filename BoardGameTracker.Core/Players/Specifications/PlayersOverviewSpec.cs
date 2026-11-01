using Ardalis.Specification;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Players.Specifications;

public sealed class PlayersOverviewSpec : Specification<Player, PlayerOverviewDto>
{
    public PlayersOverviewSpec()
    {
        Query
            .OrderByDescending(x => x.PlayerSessions.Count)
            .ThenBy(x => x.Name)
            .AsNoTracking();

        Query.Select(x => new PlayerOverviewDto
        {
            Id = x.Id,
            Name = x.Name,
            Image = x.Image,
            PlayCount = x.PlayerSessions.Count,
            WinCount = x.PlayerSessions.Count(ps => ps.Won),
            LastPlayed = x.PlayerSessions.Max(ps => (DateTime?)ps.Session.Start)
        });
    }
}
