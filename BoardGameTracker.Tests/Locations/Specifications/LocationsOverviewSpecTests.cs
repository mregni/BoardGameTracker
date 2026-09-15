using System;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Locations.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Locations.Specifications;

public class LocationsOverviewSpecTests
{
    [Fact]
    public void ShouldProjectPlayCount_AndOrderByName()
    {
        var zebra = new Location("Zebra") { Id = 1 };
        var alpha = new Location("Alpha") { Id = 2 };
        alpha.Sessions.Add(new Session(1, new DateTime(2030, 1, 1), new DateTime(2030, 1, 1, 2, 0, 0), string.Empty));
        alpha.Sessions.Add(new Session(1, new DateTime(2030, 1, 2), new DateTime(2030, 1, 2, 2, 0, 0), string.Empty));

        var spec = new LocationsOverviewSpec();
        var result = spec.Evaluate([zebra, alpha]).ToList();

        result.Select(x => x.Name).Should().Equal("Alpha", "Zebra");
        result[0].PlayCount.Should().Be(2);
        result[1].PlayCount.Should().Be(0);
        spec.AsNoTracking.Should().BeTrue();
    }
}
