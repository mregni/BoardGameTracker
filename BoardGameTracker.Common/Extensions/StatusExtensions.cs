using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Bgg;

namespace BoardGameTracker.Common.Extensions;

public static class StatusExtensions
{
    public static GameState ToGameState(this Status status)
    {
        if (status.Prevowned == 1)
        {
            return GameState.PreviouslyOwned;
        }

        if (status.Fortrade == 1)
        {
            return GameState.ForTrade;
        }

        if (status.Want == 1)
        {
            return GameState.Wanted;
        }

        return GameState.Owned;
    }
}
