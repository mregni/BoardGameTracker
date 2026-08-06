using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

public class PlayerDtoEmailPrivacyTests
{
    [Fact]
    public void ToDto_ShouldIncludeEmail()
    {
        var player = new Player("Alice", null, "alice@test.com") { Id = 5 };

        var dto = player.ToDto();

        dto!.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public void ToPublicDto_ShouldNotIncludeEmail()
    {
        var player = new Player("Alice", "img.png", "alice@test.com") { Id = 5 };

        var dto = player.ToPublicDto();

        dto!.Email.Should().BeNull();
        dto.Id.Should().Be(5);
        dto.Name.Should().Be("Alice");
        dto.Image.Should().Be("img.png");
    }

    [Fact]
    public void GameNightRsvpToDto_ShouldNotExposePlayerEmail()
    {
        var player = new Player("Guest", null, "guest@test.com") { Id = 2 };
        var rsvp = GameNightRsvp.Create(2, GameNightRsvpState.Pending);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.Player))!.SetValue(rsvp, player);

        var dto = rsvp.ToDto();

        dto.Player!.Email.Should().BeNull();
        dto.Player.Name.Should().Be("Guest");
    }
}
