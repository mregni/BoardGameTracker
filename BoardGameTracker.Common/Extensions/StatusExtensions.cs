using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Extensions;

public static class StatusExtensions
{
    public static GameState ToGameState(this CollectionResponse.Status status)
    {
        if (status.PreviouslyOwned)
        {
            return GameState.PreviouslyOwned;
        }

        if (status.ForTrade)
        {
            return GameState.ForTrade;
        }

        if (status.Want)
        {
            return GameState.Wanted;
        }

        return GameState.Owned;
    }
}
