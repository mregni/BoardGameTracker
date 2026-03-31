using System;
using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class GameScoreTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithPositiveValue_ShouldSetValue()
    {
        // Arrange
        var value = 100.5;

        // Act
        var score = new GameScore(value);

        // Assert
        score.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_WithZero_ShouldSucceed()
    {
        // Act
        var score = new GameScore(0);

        // Assert
        score.Value.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNegativeValue_ShouldThrowException()
    {
        // Act
        Action act = () => new GameScore(-1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50.5)]
    [InlineData(100)]
    [InlineData(999.99)]
    public void Constructor_WithVariousValidValues_ShouldSucceed(double value)
    {
        // Act
        var score = new GameScore(value);

        // Assert
        score.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithVariousNegativeValues_ShouldThrowException(double value)
    {
        // Act
        Action act = () => new GameScore(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Implicit Operator Tests

    [Fact]
    public void ImplicitOperator_ShouldConvertToDouble()
    {
        // Arrange
        var score = new GameScore(50.5);

        // Act
        double result = score;

        // Assert
        result.Should().Be(50.5);
    }

    [Fact]
    public void ImplicitOperator_ShouldWorkInArithmetic()
    {
        // Arrange
        var score = new GameScore(10);

        // Act
        double result = score + 5;

        // Assert
        result.Should().Be(15);
    }

    #endregion

    #region Addition Operator Tests

    [Fact]
    public void AdditionOperator_ShouldAddTwoScores()
    {
        // Arrange
        var score1 = new GameScore(10);
        var score2 = new GameScore(20);

        // Act
        var result = score1 + score2;

        // Assert
        result.Value.Should().Be(30);
    }

    [Fact]
    public void AdditionOperator_WithZero_ShouldReturnSameValue()
    {
        // Arrange
        var score1 = new GameScore(50);
        var score2 = new GameScore(0);

        // Act
        var result = score1 + score2;

        // Assert
        result.Value.Should().Be(50);
    }

    [Fact]
    public void AdditionOperator_ShouldReturnNewInstance()
    {
        // Arrange
        var score1 = new GameScore(10);
        var score2 = new GameScore(20);

        // Act
        var result = score1 + score2;

        // Assert
        result.Should().NotBeSameAs(score1);
        result.Should().NotBeSameAs(score2);
    }

    [Fact]
    public void AdditionOperator_WithDecimalValues_ShouldAddCorrectly()
    {
        // Arrange
        var score1 = new GameScore(10.5);
        var score2 = new GameScore(20.75);

        // Act
        var result = score1 + score2;

        // Assert
        result.Value.Should().Be(31.25);
    }

    #endregion

    #region Subtraction Operator Tests

    [Fact]
    public void SubtractionOperator_ShouldSubtractTwoScores()
    {
        // Arrange
        var score1 = new GameScore(30);
        var score2 = new GameScore(10);

        // Act
        var result = score1 - score2;

        // Assert
        result.Value.Should().Be(20);
    }

    [Fact]
    public void SubtractionOperator_WithZero_ShouldReturnSameValue()
    {
        // Arrange
        var score1 = new GameScore(50);
        var score2 = new GameScore(0);

        // Act
        var result = score1 - score2;

        // Assert
        result.Value.Should().Be(50);
    }

    [Fact]
    public void SubtractionOperator_ShouldReturnNewInstance()
    {
        // Arrange
        var score1 = new GameScore(30);
        var score2 = new GameScore(10);

        // Act
        var result = score1 - score2;

        // Assert
        result.Should().NotBeSameAs(score1);
        result.Should().NotBeSameAs(score2);
    }

    [Fact]
    public void SubtractionOperator_ResultingInZero_ShouldSucceed()
    {
        // Arrange
        var score1 = new GameScore(50);
        var score2 = new GameScore(50);

        // Act
        var result = score1 - score2;

        // Assert
        result.Value.Should().Be(0);
    }

    [Fact]
    public void SubtractionOperator_ResultingInNegative_ShouldThrowException()
    {
        // Arrange
        var score1 = new GameScore(10);
        var score2 = new GameScore(20);

        // Act
        Action act = () => { var _ = score1 - score2; };

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        // Arrange
        var score1 = new GameScore(50);
        var score2 = new GameScore(50);

        // Assert
        score1.Should().Be(score2);
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var score1 = new GameScore(50);
        var score2 = new GameScore(60);

        // Assert
        score1.Should().NotBe(score2);
    }

    [Fact]
    public void GetHashCode_SameValue_ShouldBeSame()
    {
        // Arrange
        var score1 = new GameScore(50);
        var score2 = new GameScore(50);

        // Assert
        score1.GetHashCode().Should().Be(score2.GetHashCode());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithVeryLargeValue_ShouldSucceed()
    {
        // Arrange
        var value = 1_000_000_000.0;

        // Act
        var score = new GameScore(value);

        // Assert
        score.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_WithSmallDecimalValue_ShouldSucceed()
    {
        // Arrange
        var value = 0.001;

        // Act
        var score = new GameScore(value);

        // Assert
        score.Value.Should().Be(value);
    }

    #endregion
}
