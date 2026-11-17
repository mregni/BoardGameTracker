using System;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class ExpansionTests
{
    [Fact]
    public void Constructor_ShouldStartOwned_AddedNow_WithoutPricesOrCover()
    {
        var before = DateTime.UtcNow;

        var expansion = new Expansion("Seafarers", 325, 1);

        expansion.State.Should().Be(GameState.Owned);
        expansion.AdditionDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        expansion.BuyingPrice.Should().BeNull();
        expansion.SoldPrice.Should().BeNull();
        expansion.Image.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldLinkTheGame_WhenTheGameIsNotSavedYet()
    {
        var game = new Game("Catan");

        var expansion = new Expansion("Seafarers", 325, game);

        expansion.Game.Should().BeSameAs(game);
        expansion.GameId.Should().Be(0);
    }

    [Fact]
    public void UpdateMethods_ShouldStoreStatePricesDateAndCover()
    {
        var expansion = new Expansion("Seafarers", 325, 1);
        var added = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        expansion.UpdateState(GameState.Sold);
        expansion.UpdateBuyingPrice(30.456m);
        expansion.UpdateSoldPrice(12m);
        expansion.UpdateAdditionDate(added);
        expansion.UpdateImage("/images/cover/325.webp");

        expansion.State.Should().Be(GameState.Sold);
        expansion.BuyingPrice!.Amount.Should().Be(30.46m);
        expansion.SoldPrice!.Amount.Should().Be(12m);
        expansion.AdditionDate.Should().Be(added);
        expansion.Image.Should().Be("/images/cover/325.webp");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void UpdateImage_ShouldClearTheCover_WhenTheValueIsBlank(string? image)
    {
        var expansion = new Expansion("Seafarers", 325, 1);
        expansion.UpdateImage("/images/cover/325.webp");

        expansion.UpdateImage(image);

        expansion.Image.Should().BeNull();
    }

    [Fact]
    public void UpdatePrices_ShouldClearThePrice_WhenTheValueIsNull()
    {
        var expansion = new Expansion("Seafarers", 325, 1);
        expansion.UpdateBuyingPrice(30m);
        expansion.UpdateSoldPrice(12m);

        expansion.UpdateBuyingPrice(null);
        expansion.UpdateSoldPrice(null);

        expansion.BuyingPrice.Should().BeNull();
        expansion.SoldPrice.Should().BeNull();
    }

    [Fact]
    public void UpdateState_ShouldReject_AnUnknownState()
    {
        var expansion = new Expansion("Seafarers", 325, 1);

        var act = () => expansion.UpdateState((GameState)42);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateBuyingPrice_ShouldReject_ANegativePrice()
    {
        var expansion = new Expansion("Seafarers", 325, 1);

        var act = () => expansion.UpdateBuyingPrice(-1m);

        act.Should().Throw<ArgumentException>();
    }
}
