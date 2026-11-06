using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Locations;

public class LocationDtoExtensionsTests
{
    [Fact]
    public void ToDto_ShouldReturnNull_WhenThereIsNoLocation()
    {
        ((Location?)null).ToDto().Should().BeNull();
    }

    [Fact]
    public void ToDto_ShouldCopyTheIdAndName()
    {
        var dto = new Location("Attic") { Id = 4 }.ToDto();

        dto!.Id.Should().Be(4);
        dto.Name.Should().Be("Attic");
    }

    [Fact]
    public void ToListDto_ShouldMapEveryLocationInOrder()
    {
        var locations = new[] { new Location("Attic") { Id = 4 }, new Location("Cellar") { Id = 2 } };

        var result = locations.ToListDto();

        result.Select(x => (x.Id, x.Name)).Should().Equal((4, "Attic"), (2, "Cellar"));
    }

    [Fact]
    public void ToListDto_ShouldReturnAnEmptyList_WhenThereAreNoLocations()
    {
        Array.Empty<Location>().ToListDto().Should().BeEmpty();
    }
}
