using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class GameNightRsvp : HasId
{
    public int PlayerId { get; private set; }
    public Player Player { get; private set; } = null!;
    public int GameNightId { get; private set; }
    public GameNight GameNight { get; private set; } = null!;
    public GameNightRsvpState State { get; private set; }

    public static GameNightRsvp Create(int playerId, GameNightRsvpState state)
    {
        return new GameNightRsvp()
        {
            PlayerId = playerId,
            State = state
        };
    }

    public void UpdateState(GameNightRsvpState state)
    {
        State = state;
    }
}
