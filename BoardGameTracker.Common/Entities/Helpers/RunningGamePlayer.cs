using Ardalis.GuardClauses;

namespace BoardGameTracker.Common.Entities.Helpers;

public class RunningGamePlayer
{
    public int RunningGameId { get; private set; }
    public RunningGame RunningGame { get; private set; } = null!;
    public int PlayerId { get; private set; }
    public Player Player { get; private set; } = null!;

    public static RunningGamePlayer Create(int playerId)
    {
        return new RunningGamePlayer
        {
            PlayerId = Guard.Against.NegativeOrZero(playerId),
        };
    }
}
