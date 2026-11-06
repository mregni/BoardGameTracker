using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class PlayTimeRangeTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(30, 30)]
    [InlineData(30, 60)]
    public void Constructor_ShouldKeepAValidRange(int min, int max)
    {
        var range = new PlayTimeRange(min, max);

        range.MinMinutes.Should().Be(min);
        range.MaxMinutes.Should().Be(max);
    }

    [Theory]
    [InlineData(-1, 60, "minMinutes")]
    [InlineData(0, -1, "maxMinutes")]
    public void Constructor_ShouldRefuseNegativeMinutes(int min, int max, string parameter)
    {
        var act = () => new PlayTimeRange(min, max);

        act.Should().Throw<ArgumentException>().WithParameterName(parameter);
    }

    [Fact]
    public void Constructor_ShouldRefuseAMaximumBelowTheMinimum()
    {
        var act = () => new PlayTimeRange(60, 59);

        act.Should().Throw<ArgumentException>().WithMessage("Maximum play time (59) cannot be less than minimum play time (60).");
    }
}
