using System;
using BoardGameTracker.Common.Entities;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Entities;

public class GameTests
{
    #region UpdateShopUrl

    [Fact]
    public void UpdateShopUrl_ShouldStoreValue_WhenValidHttpsUrl()
    {
        var game = new Game("Test Game");

        game.UpdateShopUrl("https://shop.example.com/game");

        game.ShopUrl.Should().Be("https://shop.example.com/game");
    }

    [Fact]
    public void UpdateShopUrl_ShouldStoreValue_WhenValidHttpUrl()
    {
        var game = new Game("Test Game");

        game.UpdateShopUrl("http://shop.example.com/game");

        game.ShopUrl.Should().Be("http://shop.example.com/game");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateShopUrl_ShouldStoreNull_WhenNullOrWhitespace(string? value)
    {
        var game = new Game("Test Game");

        game.UpdateShopUrl(value);

        game.ShopUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("not a url")]
    [InlineData("ftp://shop.example.com/game")]
    [InlineData("/relative/path")]
    public void UpdateShopUrl_ShouldThrow_WhenNotAbsoluteHttpUrl(string value)
    {
        var game = new Game("Test Game");
        game.UpdateShopUrl("https://shop.example.com/original");

        var action = () => game.UpdateShopUrl(value);

        action.Should().Throw<ArgumentException>();
        game.ShopUrl.Should().Be("https://shop.example.com/original");
    }

    #endregion

    #region UpdateLanguage

    [Fact]
    public void UpdateLanguage_ShouldStoreValue_WhenProvided()
    {
        var game = new Game("Test Game");

        game.UpdateLanguage("en");

        game.Language.Should().Be("en");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateLanguage_ShouldStoreNull_WhenNullOrWhitespace(string? value)
    {
        var game = new Game("Test Game");

        game.UpdateLanguage(value);

        game.Language.Should().BeNull();
    }

    #endregion

    #region UpdateYearPublished

    [Theory]
    [InlineData(1630)]
    [InlineData(1876)]
    [InlineData(1899)]
    [InlineData(1900)]
    [InlineData(2020)]
    public void UpdateYearPublished_ShouldStoreValue_WhenYearIsValid(int year)
    {
        var game = new Game("Test Game");

        game.UpdateYearPublished(year);

        game.YearPublished.Should().Be(year);
    }

    [Fact]
    public void UpdateYearPublished_ShouldStoreNull_WhenNull()
    {
        var game = new Game("Test Game");

        game.UpdateYearPublished(null);

        game.YearPublished.Should().BeNull();
    }

    [Fact]
    public void UpdateYearPublished_ShouldStoreValue_WhenYearIsAtUpperBound()
    {
        var game = new Game("Test Game");
        var upperBound = DateTime.UtcNow.Year + 10;

        game.UpdateYearPublished(upperBound);

        game.YearPublished.Should().Be(upperBound);
    }

    [Fact]
    public void UpdateYearPublished_ShouldThrow_WhenYearIsTooFarInTheFuture()
    {
        var game = new Game("Test Game");
        game.UpdateYearPublished(2020);

        var action = () => game.UpdateYearPublished(DateTime.UtcNow.Year + 11);

        action.Should().Throw<ArgumentOutOfRangeException>();
        game.YearPublished.Should().Be(2020);
    }

    #endregion

    #region UpdateTitle

    [Fact]
    public void UpdateTitle_ShouldStoreValue_WhenValid()
    {
        var game = new Game("Test Game");

        game.UpdateTitle("New Title");

        game.Title.Should().Be("New Title");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTitle_ShouldThrow_WhenNullOrWhitespace(string value)
    {
        var game = new Game("Test Game");

        var action = () => game.UpdateTitle(value);

        action.Should().Throw<ArgumentException>();
        game.Title.Should().Be("Test Game");
    }

    #endregion

    #region UpdateMinAge

    [Fact]
    public void UpdateMinAge_ShouldStoreValue_WhenPositive()
    {
        var game = new Game("Test Game");

        game.UpdateMinAge(8);

        game.MinAge.Should().Be(8);
    }

    [Fact]
    public void UpdateMinAge_ShouldStoreNull_WhenNull()
    {
        var game = new Game("Test Game");
        game.UpdateMinAge(8);

        game.UpdateMinAge(null);

        game.MinAge.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateMinAge_ShouldThrow_WhenZeroOrNegative(int value)
    {
        var game = new Game("Test Game");
        game.UpdateMinAge(8);

        var action = () => game.UpdateMinAge(value);

        action.Should().Throw<ArgumentException>();
        game.MinAge.Should().Be(8);
    }

    #endregion

    #region UpdateBggId

    [Fact]
    public void UpdateBggId_ShouldStoreValue_WhenPositive()
    {
        var game = new Game("Test Game");

        game.UpdateBggId(12345);

        game.BggId.Should().Be(12345);
    }

    [Fact]
    public void UpdateBggId_ShouldStoreNull_WhenNull()
    {
        var game = new Game("Test Game");
        game.UpdateBggId(12345);

        game.UpdateBggId(null);

        game.BggId.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void UpdateBggId_ShouldThrow_WhenZeroOrNegative(int value)
    {
        var game = new Game("Test Game");
        game.UpdateBggId(12345);

        var action = () => game.UpdateBggId(value);

        action.Should().Throw<ArgumentException>();
        game.BggId.Should().Be(12345);
    }

    #endregion

    #region UpdatePlayerCount

    [Fact]
    public void UpdatePlayerCount_ShouldStoreRange_WhenBothValuesProvided()
    {
        var game = new Game("Test Game");

        game.UpdatePlayerCount(2, 6);

        game.PlayerCount.Should().NotBeNull();
        game.PlayerCount!.Min.Should().Be(2);
        game.PlayerCount.Max.Should().Be(6);
    }

    [Theory]
    [InlineData(null, 6)]
    [InlineData(2, null)]
    [InlineData(null, null)]
    public void UpdatePlayerCount_ShouldStoreNull_WhenAnyValueMissing(int? min, int? max)
    {
        var game = new Game("Test Game");
        game.UpdatePlayerCount(2, 6);

        game.UpdatePlayerCount(min, max);

        game.PlayerCount.Should().BeNull();
    }

    #endregion

    #region UpdatePlayTime

    [Fact]
    public void UpdatePlayTime_ShouldStoreRange_WhenBothValuesProvided()
    {
        var game = new Game("Test Game");

        game.UpdatePlayTime(30, 90);

        game.PlayTime.Should().NotBeNull();
        game.PlayTime!.MinMinutes.Should().Be(30);
        game.PlayTime.MaxMinutes.Should().Be(90);
    }

    [Theory]
    [InlineData(null, 90)]
    [InlineData(30, null)]
    [InlineData(null, null)]
    public void UpdatePlayTime_ShouldStoreNull_WhenAnyValueMissing(int? min, int? max)
    {
        var game = new Game("Test Game");
        game.UpdatePlayTime(30, 90);

        game.UpdatePlayTime(min, max);

        game.PlayTime.Should().BeNull();
    }

    #endregion
}
