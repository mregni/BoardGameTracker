using System;
using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class RatingTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidValue_ShouldSetValue()
    {
        // Arrange
        var value = 7.5;

        // Act
        var rating = new Rating(value);

        // Assert
        rating.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_WithMinValue_ShouldSucceed()
    {
        // Act
        var rating = new Rating(0);

        // Assert
        rating.Value.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithMaxValue_ShouldSucceed()
    {
        // Act
        var rating = new Rating(10);

        // Assert
        rating.Value.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithNegativeValue_ShouldThrowException()
    {
        // Act
        Action act = () => new Rating(-0.01);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithValueAboveMax_ShouldThrowException()
    {
        // Act
        Action act = () => new Rating(10.01);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7.5)]
    [InlineData(9.99)]
    [InlineData(10)]
    public void Constructor_WithVariousValidValues_ShouldSucceed(double value)
    {
        // Act
        var rating = new Rating(value);

        // Assert
        rating.Value.Should().BeApproximately(value, 0.01);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.001)]
    [InlineData(10.001)]
    [InlineData(11)]
    [InlineData(100)]
    public void Constructor_WithVariousInvalidValues_ShouldThrowException(double value)
    {
        // Act
        Action act = () => new Rating(value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Rounding Tests

    [Fact]
    public void Constructor_ShouldRoundToTwoDecimalPlaces()
    {
        // Arrange
        var value = 7.555;

        // Act
        var rating = new Rating(value);

        // Assert
        rating.Value.Should().Be(7.56);
    }

    [Fact]
    public void Constructor_WithManyDecimalPlaces_ShouldRoundToTwo()
    {
        // Arrange
        var value = 5.123456789;

        // Act
        var rating = new Rating(value);

        // Assert
        rating.Value.Should().Be(5.12);
    }

    [Fact]
    public void Constructor_WithExactTwoDecimals_ShouldNotChange()
    {
        // Arrange
        var value = 8.75;

        // Act
        var rating = new Rating(value);

        // Assert
        rating.Value.Should().Be(8.75);
    }

    [Theory]
    [InlineData(7.554, 7.55)]
    [InlineData(7.555, 7.56)]
    [InlineData(7.556, 7.56)]
    [InlineData(9.999, 10.00)]
    public void Constructor_ShouldRoundCorrectly(double input, double expected)
    {
        // Act
        var rating = new Rating(input);

        // Assert
        rating.Value.Should().Be(expected);
    }

    #endregion

    #region Implicit Operator Tests

    [Fact]
    public void ImplicitOperator_ShouldConvertToDouble()
    {
        // Arrange
        var rating = new Rating(7.5);

        // Act
        double result = rating;

        // Assert
        result.Should().Be(7.5);
    }

    [Fact]
    public void ImplicitOperator_ShouldWorkInComparisons()
    {
        // Arrange
        var rating = new Rating(7.5);

        // Act & Assert
        (rating > 5).Should().BeTrue();
        (rating < 10).Should().BeTrue();
    }

    [Fact]
    public void ImplicitOperator_ShouldWorkInArithmetic()
    {
        // Arrange
        var rating = new Rating(7.5);

        // Act
        double result = rating + 2.5;

        // Assert
        result.Should().Be(10);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnFormattedValue()
    {
        // Arrange
        var rating = new Rating(7.5);
        var expected = 7.5.ToString("F2");

        // Act
        var result = rating.ToString();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToString_WithWholeNumber_ShouldIncludeDecimals()
    {
        // Arrange
        var rating = new Rating(8);
        var expected = 8.0.ToString("F2");

        // Act
        var result = rating.ToString();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToString_WithZero_ShouldShowTwoDecimals()
    {
        // Arrange
        var rating = new Rating(0);
        var expected = 0.0.ToString("F2");

        // Act
        var result = rating.ToString();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToString_WithMaxValue_ShouldShowTwoDecimals()
    {
        // Arrange
        var rating = new Rating(10);
        var expected = 10.0.ToString("F2");

        // Act
        var result = rating.ToString();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        // Arrange
        var rating1 = new Rating(7.5);
        var rating2 = new Rating(7.5);

        // Assert
        rating1.Should().Be(rating2);
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var rating1 = new Rating(7.5);
        var rating2 = new Rating(8.0);

        // Assert
        rating1.Should().NotBe(rating2);
    }

    [Fact]
    public void Equality_SameAfterRounding_ShouldBeEqual()
    {
        // Arrange
        var rating1 = new Rating(7.554);
        var rating2 = new Rating(7.55);

        // Assert - Both round to 7.55
        rating1.Should().Be(rating2);
    }

    [Fact]
    public void GetHashCode_SameValue_ShouldBeSame()
    {
        // Arrange
        var rating1 = new Rating(7.5);
        var rating2 = new Rating(7.5);

        // Assert
        rating1.GetHashCode().Should().Be(rating2.GetHashCode());
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void Constructor_AtLowerBoundary_ShouldSucceed()
    {
        // Act
        var rating = new Rating(0.01);

        // Assert
        rating.Value.Should().Be(0.01);
    }

    [Fact]
    public void Constructor_AtUpperBoundary_ShouldSucceed()
    {
        // Act
        var rating = new Rating(9.99);

        // Assert
        rating.Value.Should().Be(9.99);
    }

    [Fact]
    public void Constructor_JustBelowLowerBoundary_ShouldThrow()
    {
        // Act
        Action act = () => new Rating(-0.01);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_JustAboveUpperBoundary_ShouldThrow()
    {
        // Act
        Action act = () => new Rating(10.01);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion
}
