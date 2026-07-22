using System;
using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.GameNights.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.GameNights;

public class GameNightsOverviewSpecTests
{
    [Fact]
    public void Evaluate_ShouldOrderByStartDateDescending()
    {
        var oldest = GameNight.Create("Oldest", "", new DateTime(2030, 1, 1), 1, 1);
        oldest.Id = 1;
        var newest = GameNight.Create("Newest", "", new DateTime(2030, 3, 1), 1, 1);
        newest.Id = 2;
        var middle = GameNight.Create("Middle", "", new DateTime(2030, 2, 1), 1, 1);
        middle.Id = 3;
        var nights = new List<GameNight> { oldest, newest, middle };

        var result = new GameNightsOverviewSpec().Evaluate(nights).ToList();

        result.Select(x => x.Id).Should().ContainInOrder(2, 3, 1);
    }

    [Fact]
    public void Spec_ShouldBeNoTracking_AndIncludeDetailGraph()
    {
        var spec = new GameNightsOverviewSpec();

        spec.AsNoTracking.Should().BeTrue();
        spec.IncludeExpressions.Should().HaveCount(5);
    }
}
