using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class PlayerCountRangeTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(2, 5)]
    public void Constructor_ShouldKeepAValidRange(int min, int max)
    {
        var range = new PlayerCountRange(min, max);

        range.Min.Should().Be(min);
        range.Max.Should().Be(max);
    }

    [Theory]
    [InlineData(0, 4, "min")]
    [InlineData(-1, 4, "min")]
    [InlineData(1, 0, "max")]
    [InlineData(1, -1, "max")]
    public void Constructor_ShouldRefuseCountsBelowOne(int min, int max, string parameter)
    {
        var act = () => new PlayerCountRange(min, max);

        act.Should().Throw<ArgumentException>().WithParameterName(parameter);
    }

    [Fact]
    public void Constructor_ShouldRefuseAMaximumBelowTheMinimum()
    {
        var act = () => new PlayerCountRange(4, 3);

        act.Should().Throw<ArgumentException>().WithMessage("Maximum players cannot be less than minimum players.");
    }
}
