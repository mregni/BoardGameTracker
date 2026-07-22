using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Locations.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Locations;

public class LocationsWithSessionsSpecTests
{
    [Fact]
    public void Evaluate_ShouldOrderByNameAscending()
    {
        var bravo = new Location("Bravo") { Id = 1 };
        var alpha = new Location("Alpha") { Id = 2 };
        var charlie = new Location("Charlie") { Id = 3 };
        var locations = new List<Location> { bravo, alpha, charlie };

        var result = new LocationsWithSessionsSpec().Evaluate(locations).ToList();

        result.Select(x => x.Name).Should().ContainInOrder("Alpha", "Bravo", "Charlie");
    }

    [Fact]
    public void Spec_ShouldIncludeSessions()
    {
        new LocationsWithSessionsSpec().IncludeExpressions.Should().ContainSingle();
    }
}
