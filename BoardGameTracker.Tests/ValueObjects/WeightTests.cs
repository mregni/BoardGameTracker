using System;
using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class WeightTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidValue_ShouldSetValue()
    {
        // Arrange
        var value = 3.5;

        // Act
        var weight = new Weight(value);

        // Assert
        weight.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_WithMinValue_ShouldSucceed()
    {
        // Act
        var weight = new Weight(0);

        // Assert
        weight.Value.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithMaxValue_ShouldSucceed()
    {
        // Act
        var weight = new Weight(5);

        // Assert
        weight.Value.Should().Be(5);
    }

    [Fact]
    public void Constructor_WithNegativeValue_ShouldThrowException()
    {
        // Act
        Action act = () => new Weight(-0.01);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithValueAboveMax_ShouldThrowException()
    {
        // Act
        Action act = () => new Weight(5.01);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2.5)]
    [InlineData(3.75)]
    [InlineData(4.99)]
    [InlineData(5)]
    public void Constructor_WithVariousValidValues_ShouldSucceed(double value)
    {
        // Act
        var weight = new Weight(value);

        // Assert
        weight.Value.Should().BeApproximately(value, 0.01);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.001)]
    [InlineData(5.001)]
    [InlineData(6)]
    [InlineData(10)]
    public void Constructor_WithVariousInvalidValues_ShouldThrowException(double value)
    {
        // Act
        Action act = () => new Weight(value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Rounding Tests

    [Fact]
    public void Constructor_ShouldRoundToTwoDecimalPlaces()
    {
        // Arrange
        var value = 3.555;

        // Act
        var weight = new Weight(value);

        // Assert
        weight.Value.Should().Be(3.56);
    }

    [Fact]
    public void Constructor_WithManyDecimalPlaces_ShouldRoundToTwo()
    {
        // Arrange
        var value = 2.123456789;

        // Act
        var weight = new Weight(value);

        // Assert
        weight.Value.Should().Be(2.12);
    }

    [Fact]
    public void Constructor_WithExactTwoDecimals_ShouldNotChange()
    {
        // Arrange
        var value = 4.75;

        // Act
        var weight = new Weight(value);

        // Assert
        weight.Value.Should().Be(4.75);
    }

    [Theory]
    [InlineData(3.554, 3.55)]
    [InlineData(3.555, 3.56)]
    [InlineData(3.556, 3.56)]
    [InlineData(4.999, 5.00)]
    public void Constructor_ShouldRoundCorrectly(double input, double expected)
    {
        // Act
        var weight = new Weight(input);

        // Assert
        weight.Value.Should().Be(expected);
    }

    #endregion

    #region Implicit Operator Tests

    [Fact]
    public void ImplicitOperator_ShouldConvertToDouble()
    {
        // Arrange
        var weight = new Weight(3.5);

        // Act
        double result = weight;

        // Assert
        result.Should().Be(3.5);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnFormattedValue()
    {
        // Arrange
        var weight = new Weight(3.5);
        var expected = 3.5.ToString("F2");

        // Act
        var result = weight.ToString();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToString_WithWholeNumber_ShouldIncludeDecimals()
    {
        // Arrange
        var weight = new Weight(4);
        var expected = 4.0.ToString("F2");

        // Act
        var result = weight.ToString();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToString_WithZero_ShouldShowTwoDecimals()
    {
        // Arrange
        var weight = new Weight(0);
        var expected = 0.0.ToString("F2");

        // Act
        var result = weight.ToString();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToString_WithMaxValue_ShouldShowTwoDecimals()
    {
        // Arrange
        var weight = new Weight(5);
        var expected = 5.0.ToString("F2");

        // Act
        var result = weight.ToString();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameAfterRounding_ShouldBeEqual()
    {
        // Arrange
        var weight1 = new Weight(3.554);
        var weight2 = new Weight(3.55);

        // Assert - Both round to 3.55
        weight1.Should().Be(weight2);
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void Constructor_AtLowerBoundary_ShouldSucceed()
    {
        // Act
        var weight = new Weight(0.01);

        // Assert
        weight.Value.Should().Be(0.01);
    }

    [Fact]
    public void Constructor_AtUpperBoundary_ShouldSucceed()
    {
        // Act
        var weight = new Weight(4.99);

        // Assert
        weight.Value.Should().Be(4.99);
    }

    #endregion
}
