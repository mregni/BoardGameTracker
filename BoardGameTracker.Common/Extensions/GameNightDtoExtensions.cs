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
            HostId = gameNight.HostId,
            Host = gameNight.Host.ToDto(),
            LocationId = gameNight.LocationId,
            Location = gameNight.Location.ToDto(),
            LinkId =  gameNight.LinkId,
            SuggestedGames = gameNight.SuggestedGames.Select(g => g.ToDto()).ToList(),
            InvitedPlayers = gameNight.InvitedPlayers.Select(p => p.ToDto()).ToList()
        };
    }

    public static List<GameNightDto> ToListDto(this IEnumerable<GameNight> gameNights)
    {
        return gameNights.Select(g => g.ToDto()).ToList();
    }

    public static GameNightRsvpDto ToDto(this GameNightRsvp rsvp)
    {
        return new GameNightRsvpDto
        {
            Id = rsvp.Id,
            PlayerId = rsvp.PlayerId,
            Player = rsvp.Player.ToDto(),
            GameNightId = rsvp.GameNightId,
            State = rsvp.State
        };
    }
}
