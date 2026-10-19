using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Extensions;

public static class GameNightDtoExtensions
{
    public static GameNightDto ToDto(this GameNight gameNight)
    {
        return new GameNightDto
        {
            Id = gameNight.Id,
            Title = gameNight.Title,
            Notes = gameNight.Notes,
            StartDate = gameNight.StartDate,
            EndedAt = gameNight.EndedAt,
            HostId = gameNight.HostId,
            Host = gameNight.Host.ToPublicDto(),
            LocationId = gameNight.LocationId,
            Location = gameNight.Location.ToDto(),
            LinkId =  gameNight.LinkId,
            SuggestedGames = gameNight.SuggestedGames.Select(g => g.ToDto()).ToList(),
            InvitedPlayers = gameNight.InvitedPlayers.Select(p => p.ToDto()).ToList(),
            Summary = gameNight.ToSummaryDto()
        };
    }

    public static GameNightSummaryDto ToSummaryDto(this GameNight gameNight)
    {
        return new GameNightSummaryDto
        {
            GamesPlayed = gameNight.Sessions.Count,
            TotalMinutes = gameNight.GetTotalPlayMinutes(),
            MostWins = gameNight.GetMostWins(),
            WinnerIds = gameNight.GetNightWinnerIds(),
            RunningGames = gameNight.RunningGames.Count,
            AttendeeIds = gameNight.GetAttendeeIds(),
            AttendanceRecorded = gameNight.Attendance.Count > 0
        };
    }

    public static GameNightDetailDto ToDetailDto(this GameNight gameNight)
    {
        return new GameNightDetailDto
        {
            GameNight = gameNight.ToDto(),
            Sessions = gameNight.Sessions.OrderByDescending(s => s.Start).ToListDto(),
            RunningGames = gameNight.RunningGames.OrderBy(r => r.Start).Select(r => r.ToDto()).ToList()
        };
    }

    public static RunningGameDto ToDto(this RunningGame runningGame)
    {
        return new RunningGameDto
        {
            Id = runningGame.Id,
            GameNightId = runningGame.GameNightId,
            GameId = runningGame.GameId,
            Start = runningGame.Start,
            PlayerIds = runningGame.GetPlayerIds()
        };
    }

    public static GameNightDto ToPublicDto(this GameNight gameNight)
    {
        var dto = gameNight.ToDto();
        dto.SuggestedGames = gameNight.SuggestedGames.Select(g => g.ToPublicDto()).ToList();
        return dto;
    }

    public static GameNightRsvpDto ToDto(this GameNightRsvp rsvp)
    {
        return new GameNightRsvpDto
        {
            Id = rsvp.Id,
            PlayerId = rsvp.PlayerId,
            Player = rsvp.Player.ToPublicDto(),
            GameNightId = rsvp.GameNightId,
            State = rsvp.State
        };
    }

    public static GameNightRsvpUpdateDto ToUpdateDto(this GameNightRsvp rsvp, bool hostNotified)
    {
        return new GameNightRsvpUpdateDto
        {
            Id = rsvp.Id,
            PlayerId = rsvp.PlayerId,
            Player = rsvp.Player.ToPublicDto(),
            GameNightId = rsvp.GameNightId,
            State = rsvp.State,
            HostNotified = hostNotified
        };
    }

    public static List<GameNightDto> ToListDto(this IEnumerable<GameNight> gameNights)
    {
        return gameNights.Select(g => g.ToDto()).ToList();
    }
}
