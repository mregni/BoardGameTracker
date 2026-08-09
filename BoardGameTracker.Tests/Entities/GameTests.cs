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

        var action = () => game.UpdateShopUrl(value);

        action.Should().Throw<ArgumentException>();
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
}
