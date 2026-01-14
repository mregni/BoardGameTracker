using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.DTOs;

public class PlayerSessionDto
{
    public int PlayerId { get; set; }
    public int SessionId { get; set; }
    public double? Score { get; set; }
    public bool FirstPlay { get; set; }
    public bool Won { get; set; }
}

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
