using System;
using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class PlayerNameTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidName_ShouldSetValue()
    {
        // Arrange
        var name = "John Doe";

        // Act
        var playerName = new PlayerName(name);

        // Assert
        playerName.Value.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithValidName_ShouldTrimWhitespace()
    {
        // Arrange
        var name = "  John Doe  ";

        // Act
        var playerName = new PlayerName(name);

        // Assert
        playerName.Value.Should().Be("John Doe");
    }

    [Fact]
    public void Constructor_WithSingleCharacter_ShouldSucceed()
    {
        // Arrange
        var name = "J";

        // Act
        var playerName = new PlayerName(name);

        // Assert
        playerName.Value.Should().Be("J");
    }

    [Fact]
    public void Constructor_WithMaxLength_ShouldSucceed()
    {
        // Arrange
        var name = new string('a', 100);

        // Act
        var playerName = new PlayerName(name);

        // Assert
        playerName.Value.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithNullValue_ShouldThrowException()
    {
        // Act
        Action act = () => new PlayerName(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyString_ShouldThrowException()
    {
        // Act
        Action act = () => new PlayerName(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithWhitespaceOnly_ShouldThrowException()
    {
        // Act
        Action act = () => new PlayerName("   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithExceedingMaxLength_ShouldThrowException()
    {
        // Arrange
        var name = new string('a', 101);

        // Act
        Action act = () => new PlayerName(name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot exceed 100 characters*");
    }

    [Theory]
    [InlineData("Alice")]
    [InlineData("Bob")]
    [InlineData("Player One")]
    [InlineData("Jane-Doe")]
    [InlineData("O'Brien")]
    public void Constructor_WithVariousValidNames_ShouldSucceed(string name)
    {
        // Act
        var playerName = new PlayerName(name);

        // Assert
        playerName.Value.Should().Be(name);
    }

    #endregion

    #region Explicit Operator Tests

    [Fact]
    public void ExplicitOperator_ShouldConvertToString()
    {
        // Arrange
        var playerName = new PlayerName("John Doe");

        // Act
        string result = (string)playerName;

        // Assert
        result.Should().Be("John Doe");
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var playerName = new PlayerName("Jane Smith");

        // Act
        var result = playerName.ToString();

        // Assert
        result.Should().Be("Jane Smith");
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        // Arrange
        var playerName1 = new PlayerName("John");
        var playerName2 = new PlayerName("John");

        // Assert
        playerName1.Should().Be(playerName2);
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var playerName1 = new PlayerName("John");
        var playerName2 = new PlayerName("Jane");

        // Assert
        playerName1.Should().NotBe(playerName2);
    }

    [Fact]
    public void Equality_TrimmedVsNonTrimmed_ShouldBeEqual()
    {
        // Arrange
        var playerName1 = new PlayerName("John");
        var playerName2 = new PlayerName("  John  ");

        // Assert - Both should have "John" as value after trimming
        playerName1.Should().Be(playerName2);
    }

    [Fact]
    public void GetHashCode_SameValue_ShouldBeSame()
    {
        // Arrange
        var playerName1 = new PlayerName("John");
        var playerName2 = new PlayerName("John");

        // Assert
        playerName1.GetHashCode().Should().Be(playerName2.GetHashCode());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithSpecialCharacters_ShouldSucceed()
    {
        // Arrange
        var name = "John@#$%";

        // Act
        var playerName = new PlayerName(name);

        // Assert
        playerName.Value.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithUnicodeCharacters_ShouldSucceed()
    {
        // Arrange
        var name = "Mühler Straße";

        // Act
        var playerName = new PlayerName(name);

        // Assert
        playerName.Value.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithNumbers_ShouldSucceed()
    {
        // Arrange
        var name = "Player1";

        // Act
        var playerName = new PlayerName(name);

        // Assert
        playerName.Value.Should().Be(name);
    }

    #endregion
}
