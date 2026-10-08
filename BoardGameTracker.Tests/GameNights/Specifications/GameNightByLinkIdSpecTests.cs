using System;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights.Specifications;

public class GameNightByLinkIdSpecTests
{
    [Fact]
    public void IsSatisfiedBy_ShouldMatchOnlyTheRequestedLinkId()
    {
        var gameNight = GameNight.Create("Night", "", new DateTime(2030, 1, 1), 1, 1);
        var linkId = gameNight.LinkId;

        new GameNightByLinkIdSpec(linkId).IsSatisfiedBy(gameNight).Should().BeTrue();
        new GameNightByLinkIdSpec(Guid.NewGuid()).IsSatisfiedBy(gameNight).Should().BeFalse();
    }

    [Fact]
    public void Spec_ShouldIncludeDetailGraph()
    {
        new GameNightByLinkIdSpec(Guid.NewGuid()).ShouldIncludeExactly("Host", "Location", "SuggestedGames", "InvitedPlayers", "InvitedPlayers.Player");
    }
}
