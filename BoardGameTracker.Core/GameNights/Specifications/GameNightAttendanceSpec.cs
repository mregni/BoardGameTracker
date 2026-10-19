using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights.Specifications;

public sealed class GameNightAttendanceSpec : SingleResultSpecification<GameNight>
{
    public GameNightAttendanceSpec(int id)
    {
        Query
            .Where(x => x.Id == id)
            .Include(x => x.Sessions)
                .ThenInclude(x => x.PlayerSessions)
            .Include(x => x.RunningGames)
                .ThenInclude(x => x.Players)
            .Include(x => x.Attendance);
    }
}
