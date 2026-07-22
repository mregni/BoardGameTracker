using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Core.Players.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Players;

public class PlayerSpecsTests
{
    [Fact]
    public void PlayersOrderedByNameSpec_ShouldOrderByNameAscending_AndNotTrack()
    {
        var charlie = new Player("Charlie") { Id = 1 };
        var alice = new Player("Alice") { Id = 2 };
        var bob = new Player("Bob") { Id = 3 };
        var players = new List<Player> { charlie, alice, bob };

        var spec = new PlayersOrderedByNameSpec();

        spec.Evaluate(players).Select(x => x.Name).Should().ContainInOrder("Alice", "Bob", "Charlie");
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void PlayerByIdWithBadgesSpec_ShouldMatchId_IncludeBadges_AndNotTrack()
    {
        var player = new Player("Alice") { Id = 7 };
        var spec = new PlayerByIdWithBadgesSpec(7);

        spec.IsSatisfiedBy(player).Should().BeTrue();
        new PlayerByIdWithBadgesSpec(8).IsSatisfiedBy(player).Should().BeFalse();
        spec.AsNoTracking.Should().BeTrue();
        spec.IncludeExpressions.Should().NotBeEmpty();
    }

    [Fact]
    public void PlayerByIdForUpdateSpec_ShouldMatchId_AndTrack()
    {
        var player = new Player("Alice") { Id = 7 };
        var spec = new PlayerByIdForUpdateSpec(7);

        spec.IsSatisfiedBy(player).Should().BeTrue();
        // Tracked on purpose: PlayerService.Update mutates the result and relies on change tracking (fixes bug C2).
        spec.AsNoTracking.Should().BeFalse();
    }

    [Fact]
    public void WonPlayerSessionsByPlayerSpec_ShouldMatchOnlyThatPlayersWins()
    {
        var playerWon = new PlayerSession(5, won: true);
        var playerLost = new PlayerSession(5, won: false);
        var otherPlayerWon = new PlayerSession(6, won: true);

        var result = new WonPlayerSessionsByPlayerSpec(5)
            .Evaluate(new[] { playerWon, playerLost, otherPlayerWon })
            .ToList();

        result.Should().ContainSingle();
        result.Single().PlayerId.Should().Be(5);
        result.Single().Won.Should().BeTrue();
    }
}
