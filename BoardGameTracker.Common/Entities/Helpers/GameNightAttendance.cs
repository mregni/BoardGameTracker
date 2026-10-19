using Ardalis.GuardClauses;

namespace BoardGameTracker.Common.Entities.Helpers;

public class GameNightAttendance
{
    public int GameNightId { get; private set; }
    public GameNight GameNight { get; private set; } = null!;
    public int PlayerId { get; private set; }
    public Player Player { get; private set; } = null!;
    public bool Present { get; private set; }

    public static GameNightAttendance Create(int playerId, bool present)
    {
        return new GameNightAttendance
        {
            PlayerId = Guard.Against.NegativeOrZero(playerId),
            Present = present,
        };
    }

    public void UpdatePresent(bool present)
    {
        Present = present;
    }
}
