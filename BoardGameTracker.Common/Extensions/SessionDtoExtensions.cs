using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Extensions;

public static class SessionDtoExtensions
{
    public static SessionDto ToDto(this Session session)
    {
        return new SessionDto
        {
            Id = session.Id,
            Comment = session.Comment,
            GameId = session.GameId,
            Start = session.Start,
            End = session.End,
            Minutes = session.GetDuration().TotalMinutes,
            LocationId = session.LocationId,
            PlayerSessions = session.PlayerSessions.Select(ps => ps.ToDto()).ToList(),
            Expansions = session.Expansions.Select(e => e.ToDto()).ToList(),
        };
    }

    public static List<SessionDto> ToListDto(this IEnumerable<Session> sessions)
    {
        return sessions.Select(s => ToDto(s)).ToList();
    }
}
