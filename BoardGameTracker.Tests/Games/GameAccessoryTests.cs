using BoardGameTracker.Common.Entities;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class GameAccessoryTests
{
    [Fact]
    public void Constructor_ShouldStoreTheValues()
    {
        var accessory = new GameAccessory("Metal coins", "Heavy coins", 3);

        accessory.Title.Should().Be("Metal coins");
        accessory.Description.Should().Be("Heavy coins");
        accessory.GameId.Should().Be(3);
    }

    [Theory]
    [InlineData("", "Description", 1)]
    [InlineData(" ", "Description", 1)]
    [InlineData("Title", "", 1)]
    [InlineData("Title", " ", 1)]
    [InlineData("Title", "Description", 0)]
    [InlineData("Title", "Description", -1)]
    public void Constructor_ShouldThrow_WhenAValueIsNotUsable(string title, string description, int gameId)
    {
        var action = () => new GameAccessory(title, description, gameId);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateTitle_ShouldStoreValue()
    {
        var accessory = new GameAccessory("Metal coins", "Heavy coins", 3);

        accessory.UpdateTitle("Wooden coins");

        accessory.Title.Should().Be("Wooden coins");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void UpdateTitle_ShouldThrow_WhenBlank(string title)
    {
        var accessory = new GameAccessory("Metal coins", "Heavy coins", 3);

        var action = () => accessory.UpdateTitle(title);

        action.Should().Throw<ArgumentException>();
        accessory.Title.Should().Be("Metal coins");
    }

    [Fact]
    public void UpdateDescription_ShouldStoreValue()
    {
        var accessory = new GameAccessory("Metal coins", "Heavy coins", 3);

        accessory.UpdateDescription("Light coins");

        accessory.Description.Should().Be("Light coins");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void UpdateDescription_ShouldThrow_WhenBlank(string description)
    {
        var accessory = new GameAccessory("Metal coins", "Heavy coins", 3);

        var action = () => accessory.UpdateDescription(description);

        action.Should().Throw<ArgumentException>();
        accessory.Description.Should().Be("Heavy coins");
    }
}
