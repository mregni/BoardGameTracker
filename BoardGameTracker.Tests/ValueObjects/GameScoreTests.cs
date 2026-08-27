using System;
using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class GameScoreTests
{
    #region Constructor Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50.5)]
    [InlineData(100)]
    [InlineData(999.99)]
    [InlineData(1_000_000_000.0)]
    [InlineData(0.001)]
    public void Constructor_WithVariousValidValues_ShouldSucceed(double value)
    {
        var score = new GameScore(value);

        score.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithVariousNegativeValues_ShouldThrowException(double value)
    {
        Action act = () => new GameScore(value);

        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        new GameScore(5).Should().Be(new GameScore(5));
    }

    [Fact]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        new GameScore(5).Should().NotBe(new GameScore(6));
    }

    #endregion

    #region Implicit Operator Tests

    [Fact]
    public void ImplicitOperator_ShouldConvertToDouble()
    {
        var score = new GameScore(50.5);

        double result = score;

        result.Should().Be(50.5);
    }

    #endregion

    #region Addition Operator Tests

    [Theory]
    [InlineData(10, 20, 30)]
    [InlineData(50, 0, 50)]
    [InlineData(10.5, 20.75, 31.25)]
    public void AdditionOperator_ShouldAddTwoScores(double left, double right, double expected)
    {
        var result = new GameScore(left) + new GameScore(right);

        result.Value.Should().Be(expected);
    }

    #endregion

    #region Subtraction Operator Tests

    [Theory]
    [InlineData(30, 10, 20)]
    [InlineData(50, 0, 50)]
    [InlineData(50, 50, 0)]
    public void SubtractionOperator_ShouldSubtractTwoScores(double left, double right, double expected)
    {
        var result = new GameScore(left) - new GameScore(right);

        result.Value.Should().Be(expected);
    }

    [Fact]
    public void SubtractionOperator_ResultingInNegative_ShouldThrowException()
    {
        var score1 = new GameScore(10);
        var score2 = new GameScore(20);

        Action act = () => { var _ = score1 - score2; };

        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    #endregion
}
