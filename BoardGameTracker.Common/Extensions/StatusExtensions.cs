using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Extensions;

public static class StatusExtensions
{
    public static GameState? ToGameState(this CollectionResponse.Status status)
    {
        return ToGameState(
            status.Owned,
            status.PreviouslyOwned,
            status.ForTrade,
            status.Want || status.WantToBuy || status.Wishlist || status.Preordered);
    }

    public static GameState? ToGameState(bool owned, bool previouslyOwned, bool forTrade, bool wanted)
    {
        if (previouslyOwned)
        {
            return GameState.PreviouslyOwned;
        }

        if (forTrade)
        {
            return GameState.ForTrade;
        }

        if (owned)
        {
            return GameState.Owned;
        }

        if (wanted)
        {
            return GameState.Wanted;
        }

        return null;
    }
}
