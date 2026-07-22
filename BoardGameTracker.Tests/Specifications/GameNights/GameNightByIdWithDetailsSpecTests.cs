using System;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.GameNights.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.GameNights;

public class GameNightByIdWithDetailsSpecTests
{
    [Fact]
    public void IsSatisfiedBy_ShouldMatchOnlyTheRequestedId()
    {
        var gameNight = GameNight.Create("Night", "", new DateTime(2030, 1, 1), 1, 1);
        gameNight.Id = 5;

        var spec = new GameNightByIdWithDetailsSpec(5);

        spec.IsSatisfiedBy(gameNight).Should().BeTrue();
        new GameNightByIdWithDetailsSpec(6).IsSatisfiedBy(gameNight).Should().BeFalse();
    }

    [Fact]
    public void Spec_ShouldIncludeDetailGraph_AndTrack()
    {
        var spec = new GameNightByIdWithDetailsSpec(1);

        spec.IncludeExpressions.Should().HaveCount(5);
        spec.AsNoTracking.Should().BeFalse();
    }
}
