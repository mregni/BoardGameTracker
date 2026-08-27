using System;
using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class RatingTests
{
    #region Constructor Tests

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7.5)]
    [InlineData(9.99)]
    [InlineData(10)]
    public void Constructor_WithVariousValidValues_ShouldSucceed(double value)
    {
        var rating = new Rating(value);

        rating.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    [InlineData(-0.001)]
    [InlineData(10.01)]
    [InlineData(10.001)]
    [InlineData(11)]
    [InlineData(100)]
    public void Constructor_WithVariousInvalidValues_ShouldThrowException(double value)
    {
        Action act = () => new Rating(value);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("value");
    }

    #endregion

    #region Rounding Tests

    [Theory]
    [InlineData(7.554, 7.55)]
    [InlineData(7.555, 7.56)]
    [InlineData(7.556, 7.56)]
    [InlineData(9.999, 10.00)]
    [InlineData(5.123456789, 5.12)]
    [InlineData(8.75, 8.75)]
    public void Constructor_ShouldRoundToTwoDecimalPlaces(double input, double expected)
    {
        var rating = new Rating(input);

        rating.Value.Should().Be(expected);
    }

    #endregion

    #region Implicit Operator Tests

    [Fact]
    public void ImplicitOperator_ShouldConvertToDouble()
    {
        var rating = new Rating(7.5);

        double result = rating;

        result.Should().Be(7.5);
    }

    #endregion

    #region ToString Tests

    [Theory]
    [InlineData(7.5)]
    [InlineData(8)]
    [InlineData(0)]
    [InlineData(10)]
    public void ToString_ShouldReturnFormattedValue(double value)
    {
        var rating = new Rating(value);

        rating.ToString().Should().Be(value.ToString("F2"));
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameAfterRounding_ShouldBeEqual()
    {
        var rating1 = new Rating(7.554);
        var rating2 = new Rating(7.55);

        rating1.Should().Be(rating2);
    }

    #endregion
}
