using System;
using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.GameNights.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.GameNights;

public class FutureGameNightsSpecTests
{
    [Fact]
    public void Evaluate_ShouldReturnOnlyGameNightsAtOrAfterNow()
    {
        var now = new DateTime(2030, 1, 1);
        var past = GameNight.Create("Past", "", now.AddDays(-1), 1, 1);
        var future = GameNight.Create("Future", "", now.AddDays(1), 1, 1);
        var exactly = GameNight.Create("Now", "", now, 1, 1);
        var nights = new List<GameNight> { past, future, exactly };
        var spec = new FutureGameNightsSpec(now);

        var result = spec.Evaluate(nights).ToList();

        result.Select(x => x.Title).Should().BeEquivalentTo("Future", "Now");
        result.Should().NotContain(x => x.Title == "Past");
        spec.AsNoTracking.Should().BeTrue();
    }
}
