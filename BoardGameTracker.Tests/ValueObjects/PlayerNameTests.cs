using System;
using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class PlayerNameTests
{
    #region Constructor Tests

    [Theory]
    [InlineData("John Doe")]
    [InlineData("J")]
    [InlineData("Alice")]
    [InlineData("Player One")]
    [InlineData("Jane-Doe")]
    [InlineData("O'Brien")]
    [InlineData("John@#$%")]
    [InlineData("Mühler Straße")]
    [InlineData("Player1")]
    public void Constructor_WithVariousValidNames_ShouldSucceed(string name)
    {
        var playerName = new PlayerName(name);

        playerName.Value.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithValidName_ShouldTrimWhitespace()
    {
        var playerName = new PlayerName("  John Doe  ");

        playerName.Value.Should().Be("John Doe");
    }

    [Fact]
    public void Constructor_WithMaxLength_ShouldSucceed()
    {
        var name = new string('a', 100);

        var playerName = new PlayerName(name);

        playerName.Value.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithNullValue_ShouldThrowArgumentNullException()
    {
        Action act = () => new PlayerName(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespace_ShouldThrowException(string name)
    {
        Action act = () => new PlayerName(name);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithExceedingMaxLength_ShouldThrowException()
    {
        var name = new string('a', 101);

        Action act = () => new PlayerName(name);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot exceed 100 characters*");
    }

    #endregion

    #region Explicit Operator Tests

    [Fact]
    public void ExplicitOperator_ShouldConvertToString()
    {
        var playerName = new PlayerName("John Doe");

        string result = (string)playerName;

        result.Should().Be("John Doe");
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var playerName = new PlayerName("Jane Smith");

        playerName.ToString().Should().Be("Jane Smith");
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_TrimmedVsNonTrimmed_ShouldBeEqual()
    {
        var playerName1 = new PlayerName("John");
        var playerName2 = new PlayerName("  John  ");

        playerName1.Should().Be(playerName2);
    }

    #endregion
}
