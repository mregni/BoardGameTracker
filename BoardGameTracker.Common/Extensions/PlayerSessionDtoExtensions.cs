using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Extensions;

public static class PlayerSessionDtoExtensions
{
    public static PlayerSessionDto ToDto(this PlayerSession playerSession)
    {
        return new PlayerSessionDto
        {
            PlayerId = playerSession.PlayerId,
            SessionId = playerSession.SessionId,
            Score = playerSession.Score,
            FirstPlay = playerSession.FirstPlay,
            Won = playerSession.Won
        };
    }
}
