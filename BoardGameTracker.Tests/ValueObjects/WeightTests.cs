using System;
using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class WeightTests
{
    #region Constructor Tests

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(2.5)]
    [InlineData(3.75)]
    [InlineData(4.99)]
    [InlineData(5)]
    public void Constructor_WithVariousValidValues_ShouldSucceed(double value)
    {
        var weight = new Weight(value);

        weight.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    [InlineData(-0.001)]
    [InlineData(5.01)]
    [InlineData(5.001)]
    [InlineData(6)]
    [InlineData(10)]
    public void Constructor_WithVariousInvalidValues_ShouldThrowException(double value)
    {
        Action act = () => new Weight(value);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("value");
    }

    #endregion

    #region Rounding Tests

    [Theory]
    [InlineData(3.554, 3.55)]
    [InlineData(3.555, 3.56)]
    [InlineData(3.556, 3.56)]
    [InlineData(4.999, 5.00)]
    [InlineData(2.123456789, 2.12)]
    [InlineData(4.75, 4.75)]
    public void Constructor_ShouldRoundToTwoDecimalPlaces(double input, double expected)
    {
        var weight = new Weight(input);

        weight.Value.Should().Be(expected);
    }

    #endregion

    #region Implicit Operator Tests

    [Fact]
    public void ImplicitOperator_ShouldConvertToDouble()
    {
        var weight = new Weight(3.5);

        double result = weight;

        result.Should().Be(3.5);
    }

    #endregion

    #region ToString Tests

    [Theory]
    [InlineData(3.5)]
    [InlineData(4)]
    [InlineData(0)]
    [InlineData(5)]
    public void ToString_ShouldReturnFormattedValue(double value)
    {
        var weight = new Weight(value);

        weight.ToString().Should().Be(value.ToString("F2"));
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameAfterRounding_ShouldBeEqual()
    {
        var weight1 = new Weight(3.554);
        var weight2 = new Weight(3.55);

        weight1.Should().Be(weight2);
    }

    #endregion
}
