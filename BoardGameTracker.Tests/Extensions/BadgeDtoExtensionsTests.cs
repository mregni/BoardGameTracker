using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

public class BadgeDtoExtensionsTests
{
    private static readonly DateTime AwardedAt = new(2026, 10, 21, 18, 30, 0, DateTimeKind.Utc);

    private static PlayerBadge CreatePlayerBadge(Badge badge, DateTime? awardedAt)
    {
        var playerBadge = new PlayerBadge(3, badge.Id, awardedAt);
        typeof(PlayerBadge).GetProperty(nameof(PlayerBadge.Badge))!.SetValue(playerBadge, badge);
        return playerBadge;
    }

    [Fact]
    public void ToDto_ShouldLeaveAwardedAtEmpty_ForACatalogBadge()
    {
        var badge = Badge.CreateWithId(5, "sessions.green.title", "sessions.green.description", BadgeType.Sessions, "sessions-green.png", BadgeLevel.Green);

        var dto = badge.ToDto();

        dto.Id.Should().Be(5);
        dto.AwardedAt.Should().BeNull();
    }

    [Fact]
    public void ToDto_ShouldCarryTheBadgeAndItsAwardedAt_ForAnEarnedBadge()
    {
        var badge = Badge.CreateWithId(6, "sessions.blue.title", "sessions.blue.description", BadgeType.Sessions, "sessions-blue.png", BadgeLevel.Blue);

        var dto = CreatePlayerBadge(badge, AwardedAt).ToDto();

        dto.Id.Should().Be(6);
        dto.TitleKey.Should().Be("sessions.blue.title");
        dto.DescriptionKey.Should().Be("sessions.blue.description");
        dto.Type.Should().Be(BadgeType.Sessions);
        dto.Level.Should().Be(BadgeLevel.Blue);
        dto.Image.Should().Be("sessions-blue.png");
        dto.AwardedAt.Should().Be(AwardedAt);
    }

    [Fact]
    public void PlayerToDto_ShouldListEarnedBadgesWithTheirAwardedAt()
    {
        var player = new Player("Alice") { Id = 3 };
        var recent = Badge.CreateWithId(6, "sessions.blue.title", "sessions.blue.description", BadgeType.Sessions, "sessions-blue.png", BadgeLevel.Blue);
        var unknown = Badge.CreateWithId(36, "first-try.title", "first-try.description", BadgeType.FirstTry, "first-try.png");
        player.PlayerBadges.Add(CreatePlayerBadge(recent, AwardedAt));
        player.PlayerBadges.Add(CreatePlayerBadge(unknown, null));

        var dto = player.ToDto();

        dto!.Badges!.Select(x => (x.Id, x.AwardedAt)).Should().Equal((6, AwardedAt), (36, (DateTime?)null));
    }
}
