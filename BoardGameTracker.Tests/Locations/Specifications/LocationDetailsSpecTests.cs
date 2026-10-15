using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Locations.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Locations.Specifications;

public class LocationDetailsSpecTests
{
    [Fact]
    public void LocationDetailsSpec_ShouldProjectMatchingLocationWithPlayCount()
    {
        var home = new Location("Home") { Id = 1 };
        var office = new Location("Office") { Id = 2 };
        home.Sessions.Add(new Session(1, new DateTime(2030, 1, 1), new DateTime(2030, 1, 1, 2, 0, 0), string.Empty));

        var spec = new LocationDetailsSpec(1);
        var result = spec.Evaluate([home, office]).ToList();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("Home");
        result[0].PlayCount.Should().Be(1);
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void LocationByIdSpec_ShouldMatchId_AndNotTrack()
    {
        var home = new Location("Home") { Id = 1 };
        var spec = new LocationByIdSpec(1);

        spec.IsSatisfiedBy(home).Should().BeTrue();
        new LocationByIdSpec(2).IsSatisfiedBy(home).Should().BeFalse();
        spec.AsNoTracking.Should().BeTrue();
    }
}
