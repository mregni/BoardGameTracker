using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class GameNightDetailSpec : SingleResultSpecification<GameNight>
{
    public GameNightDetailSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .Include(x => x.Host)
            .Include(x => x.Location)
            .Include(x => x.SuggestedGames)
            .Include(x => x.InvitedPlayers)
                .ThenInclude(x => x.Player)
            .Include(x => x.Sessions)
                .ThenInclude(x => x.PlayerSessions)
            .Include(x => x.Sessions)
                .ThenInclude(x => x.Expansions)
            .Include(x => x.RunningGames)
                .ThenInclude(x => x.Players)
            .Include(x => x.Attendance)
            .AsNoTracking();
    }
}
