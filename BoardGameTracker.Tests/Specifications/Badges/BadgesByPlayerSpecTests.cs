using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Badges;

public class BadgesByPlayerSpecTests
{
    private static Badge BadgeForPlayers(int badgeId, params int[] playerIds)
    {
        var badge = Badge.CreateWithId(badgeId, "title", "description", BadgeType.Sessions, "img.png");
        foreach (var playerId in playerIds)
        {
            badge.Players.Add(new Player($"Player {playerId}") { Id = playerId });
        }

        return badge;
    }

    [Fact]
    public void Evaluate_ShouldReturnOnlyBadgesHeldByThePlayer()
    {
        var held = BadgeForPlayers(1, 5, 7);
        var notHeld = BadgeForPlayers(2, 8);
        var badges = new List<Badge> { held, notHeld };

        var result = new BadgesByPlayerSpec(5).Evaluate(badges).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public void IsSatisfiedBy_ShouldBeFalse_WhenPlayerDoesNotHoldBadge()
    {
        var badge = BadgeForPlayers(1, 5);

        new BadgesByPlayerSpec(99).IsSatisfiedBy(badge).Should().BeFalse();
    }
}
