using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class SessionDto
{
    public int Id { get; set; }
    public string Comment { get; set; } = string.Empty;
    public int GameId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public double Minutes { get; set; }
    public int? LocationId { get; set; }
    public List<PlayerSessionDto> PlayerSessions { get; set; } = [];
    public List<ExpansionDto> Expansions { get; set; } = [];
    public List<SessionFlag>? Flags { get; set; }
}

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
            Flags = new List<SessionFlag>()
        };
    }

    public static List<SessionDto> ToListDto(this IEnumerable<Session> sessions)
    {
        return sessions.Select(s => s.ToDto()).ToList();
    }

    public static Session ToEntity(this SessionDto dto)
    {
        var end = dto.Start.AddMinutes(dto.Minutes);
        return new Session(dto.GameId, dto.Start, end, dto.Comment);
    }
}
