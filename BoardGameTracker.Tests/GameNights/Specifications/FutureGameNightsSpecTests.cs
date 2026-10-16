using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.GameNights.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights.Specifications;

public class FutureGameNightsSpecTests
{
    [Fact]
    public void Evaluate_ShouldKeepNightsThatAreComingOrStillBeingPlayed()
    {
        var now = new DateTime(2030, 1, 1, 22, 0, 0, DateTimeKind.Utc);
        var lastWeek = GameNight.Create("Last week", "", now.AddDays(-7), 1, 1);
        var future = GameNight.Create("Future", "", now.AddDays(1), 1, 1);
        var playing = GameNight.Create("Playing", "", now.AddHours(-2), 1, 1);
        var overByTime = GameNight.Create("Over by time", "", now.AddHours(-12), 1, 1);
        var ended = GameNight.Create("Ended", "", now.AddHours(-3), 1, 1);
        ended.End(now.AddMinutes(-10));
        var spec = new FutureGameNightsSpec(now);

        var result = spec.Evaluate(new List<GameNight> { lastWeek, future, playing, overByTime, ended }).ToList();

        result.Select(x => x.Title).Should().BeEquivalentTo("Future", "Playing");
        spec.AsNoTracking.Should().BeTrue();
    }
}
