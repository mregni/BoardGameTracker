using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Players;

public class PlayerDtoExtensionsTests
{
    private static readonly DateTime AwardedAt = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ToDto_ShouldReturnNull_WhenThePlayerIsNull()
    {
        ((Player?)null).ToDto().Should().BeNull();
    }

    [Fact]
    public void ToDto_ShouldMapEveryField()
    {
        var badge = TestBadges.Create(BadgeType.Wins, BadgeLevel.Gold, 8);
        var playerBadge = new PlayerBadge(5, badge.Id, AwardedAt);
        typeof(PlayerBadge).GetProperty(nameof(PlayerBadge.Badge))!.SetValue(playerBadge, badge);
        var player = new Player("Jane", "jane.png", "jane@example.com") { Id = 5 };
        player.PlayerBadges.Add(playerBadge);
        var account = new PlayerLinkedAccountDto { Id = "u1", Username = "jane" };

        var dto = player.ToDto(account)!;

        dto.Id.Should().Be(5);
        dto.Name.Should().Be("Jane");
        dto.Image.Should().Be("jane.png");
        dto.ImageSource.Should().Be(PlayerImageSource.Upload);
        dto.Email.Should().Be("jane@example.com");
        dto.LinkedAccount.Should().BeSameAs(account);
        dto.Badges.Should().ContainSingle(x => x.Id == 8 && x.AwardedAt == AwardedAt);
    }

    [Fact]
    public void ToDto_ShouldLeaveTheLinkedAccountEmpty_WhenNoneIsGiven()
    {
        var dto = new Player("Jane").ToDto()!;

        dto.LinkedAccount.Should().BeNull();
        dto.Badges.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_ShouldLeaveTheBadgesEmpty_WhenTheyAreNotLoaded()
    {
        var player = new Player("Jane");
        typeof(Player).GetProperty(nameof(Player.PlayerBadges))!.SetValue(player, null);

        player.ToDto()!.Badges.Should().BeNull();
    }

    [Fact]
    public void ToPublicDto_ShouldReturnNull_WhenThePlayerIsNull()
    {
        ((Player?)null).ToPublicDto().Should().BeNull();
    }

    [Fact]
    public void ToListDto_ShouldMapEveryPlayerInOrder()
    {
        var players = new List<Player>
        {
            new("Bob") { Id = 2 },
            new("Alice") { Id = 1 }
        };

        var dtos = players.ToListDto();

        dtos.Select(x => x.Id).Should().Equal(2, 1);
        dtos.Select(x => x.Name).Should().Equal("Bob", "Alice");
    }

    [Fact]
    public void ToListDto_ShouldReturnAnEmptyList_WhenThereAreNoPlayers()
    {
        new List<Player>().ToListDto().Should().BeEmpty();
    }
}
