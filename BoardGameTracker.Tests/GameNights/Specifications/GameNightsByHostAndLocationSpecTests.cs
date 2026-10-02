using System;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.GameNights.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights.Specifications;

public class GameNightsByHostAndLocationSpecTests
{
    private static GameNight Night(int hostId, int locationId) =>
        GameNight.Create("Night", string.Empty, new DateTime(2030, 1, 1), hostId, locationId);

    [Fact]
    public void GameNightsHostedByPlayerSpec_ShouldMatchOnlyTheHost()
    {
        var spec = new GameNightsHostedByPlayerSpec(7);

        spec.IsSatisfiedBy(Night(7, 1)).Should().BeTrue();
        spec.IsSatisfiedBy(Night(8, 1)).Should().BeFalse();
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void GameNightsAtLocationSpec_ShouldMatchOnlyTheLocation()
    {
        var spec = new GameNightsAtLocationSpec(3);

        spec.IsSatisfiedBy(Night(1, 3)).Should().BeTrue();
        spec.IsSatisfiedBy(Night(1, 4)).Should().BeFalse();
        spec.AsNoTracking.Should().BeTrue();
    }
}
