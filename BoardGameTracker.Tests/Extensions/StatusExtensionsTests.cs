using System;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

public class StatusExtensionsTests
{
    [Theory]
    [InlineData(false, true, false, false, false, false, false, GameState.PreviouslyOwned)]
    [InlineData(false, false, true, false, false, false, false, GameState.ForTrade)]
    [InlineData(true, false, false, false, false, false, false, GameState.Owned)]
    [InlineData(false, false, false, true, false, false, false, GameState.Wanted)]
    [InlineData(false, false, false, false, true, false, false, GameState.Wanted)]
    [InlineData(false, false, false, false, false, true, false, GameState.Wanted)]
    [InlineData(false, false, false, false, false, false, true, GameState.Wanted)]
    [InlineData(false, true, true, true, false, false, false, GameState.PreviouslyOwned)]
    [InlineData(false, false, true, true, false, false, false, GameState.ForTrade)]
    [InlineData(true, false, false, true, false, false, false, GameState.Owned)]
    public void ToGameState_ShouldMapStatusFlagsWithPriority(
        bool owned, bool previouslyOwned, bool forTrade, bool want, bool wantToBuy, bool wishlist, bool preordered, GameState expected)
    {
        var status = new CollectionResponse.Status
        {
            Owned = owned,
            PreviouslyOwned = previouslyOwned,
            ForTrade = forTrade,
            Want = want,
            WantToBuy = wantToBuy,
            Wishlist = wishlist,
            Preordered = preordered,
            LastModified = new DateTime(2023, 1, 1)
        };

        status.ToGameState().Should().Be(expected);
    }

    [Fact]
    public void ToGameState_ShouldReturnNull_WhenTheItemIsOnlyRatedOrPlayed()
    {
        var status = new CollectionResponse.Status
        {
            LastModified = new DateTime(2023, 1, 1)
        };

        status.ToGameState().Should().BeNull();
    }
}
