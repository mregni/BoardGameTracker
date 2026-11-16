using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Storage;

public class StoredItemTests
{
    [Fact]
    public void Game_ShouldKeepItsPlace_UntilItIsNoLongerInTheCollection()
    {
        var game = new Game("Wingspan");
        game.Store(1, 7, 3);

        game.StoredAtLocationId.Should().Be(1);
        game.StorageLocationId.Should().Be(7);
        game.StoragePosition.Should().Be(3);

        game.UpdateState(GameState.ForTrade);
        game.StorageLocationId.Should().Be(7);

        game.UpdateState(GameState.Sold);
        game.StoredAtLocationId.Should().BeNull();
        game.StorageLocationId.Should().BeNull();
        game.StoragePosition.Should().Be(0);
    }

    [Fact]
    public void Game_ShouldNotGetAPlace_WhenItIsWanted()
    {
        var game = new Game("Wingspan", state: GameState.Wanted);

        var act = () => game.Store(1, null, 0);

        act.Should().Throw<DomainException>().WithMessage(Constants.Errors.StorageNotAllowedForState);
    }

    [Fact]
    public void Expansion_ShouldKeepItsPlace_UntilItIsNoLongerInTheCollection()
    {
        var expansion = new Expansion("Oceania", 1, 5);
        expansion.Store(2, null, 0);
        expansion.UpdateBoxOrientation(BoxOrientation.Flat);

        expansion.StoredAtLocationId.Should().Be(2);
        expansion.BoxOrientation.Should().Be(BoxOrientation.Flat);

        expansion.UpdateState(GameState.PreviouslyOwned);
        expansion.StoredAtLocationId.Should().BeNull();

        var act = () => expansion.Store(2, null, 0);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ClearStorage_ShouldPutTheExpansionBackInItsBaseGameBox()
    {
        var expansion = new Expansion("Oceania", 1, 5);
        expansion.Store(2, 9, 4);

        expansion.ClearStorage();

        expansion.StoredAtLocationId.Should().BeNull();
        expansion.StorageLocationId.Should().BeNull();
        expansion.StoragePosition.Should().Be(0);
    }
}
