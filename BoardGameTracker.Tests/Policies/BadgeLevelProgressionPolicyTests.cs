using System;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Policies;

public class BadgeLevelProgressionPolicyTests
{
    private const BadgeLevel UndefinedLevel = (BadgeLevel)99;

    private readonly BadgeLevelProgressionPolicy _policy;

    public BadgeLevelProgressionPolicyTests()
    {
        _policy = new BadgeLevelProgressionPolicy();
    }

    #region CanProgressTo Tests

    [Theory]
    [InlineData(BadgeLevel.Green, BadgeLevel.Blue, true)]
    [InlineData(BadgeLevel.Blue, BadgeLevel.Red, true)]
    [InlineData(BadgeLevel.Red, BadgeLevel.Gold, true)]
    [InlineData(BadgeLevel.Green, BadgeLevel.Red, false)]
    [InlineData(BadgeLevel.Green, BadgeLevel.Gold, false)]
    [InlineData(BadgeLevel.Blue, BadgeLevel.Gold, false)]
    [InlineData(BadgeLevel.Blue, BadgeLevel.Green, false)]
    [InlineData(BadgeLevel.Red, BadgeLevel.Blue, false)]
    [InlineData(BadgeLevel.Gold, BadgeLevel.Red, false)]
    [InlineData(BadgeLevel.Green, BadgeLevel.Green, false)]
    [InlineData(BadgeLevel.Blue, BadgeLevel.Blue, false)]
    [InlineData(BadgeLevel.Red, BadgeLevel.Red, false)]
    [InlineData(BadgeLevel.Gold, BadgeLevel.Gold, false)]
    public void CanProgressTo_ShouldOnlyAllowSingleStepForward(BadgeLevel current, BadgeLevel next, bool expected)
    {
        // Act
        var result = _policy.CanProgressTo(current, next);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CanProgressTo_ShouldThrow_WhenCurrentLevelIsUndefined()
    {
        var act = () => _policy.CanProgressTo(UndefinedLevel, BadgeLevel.Green);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("current");
    }

    [Fact]
    public void CanProgressTo_ShouldThrow_WhenNextLevelIsUndefined()
    {
        var act = () => _policy.CanProgressTo(BadgeLevel.Gold, UndefinedLevel);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("next");
    }

    #endregion

    #region GetNextLevel Tests

    [Theory]
    [InlineData(BadgeLevel.Green, BadgeLevel.Blue)]
    [InlineData(BadgeLevel.Blue, BadgeLevel.Red)]
    [InlineData(BadgeLevel.Red, BadgeLevel.Gold)]
    [InlineData(BadgeLevel.Gold, null)]
    public void GetNextLevel_ShouldReturnFollowingLevel(BadgeLevel current, BadgeLevel? expected)
    {
        // Act
        var result = _policy.GetNextLevel(current);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetNextLevel_ShouldThrow_WhenLevelIsUndefined()
    {
        var act = () => _policy.GetNextLevel(UndefinedLevel);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("current");
    }

    #endregion

    #region GetPreviousLevel Tests

    [Theory]
    [InlineData(BadgeLevel.Green, null)]
    [InlineData(BadgeLevel.Blue, BadgeLevel.Green)]
    [InlineData(BadgeLevel.Red, BadgeLevel.Blue)]
    [InlineData(BadgeLevel.Gold, BadgeLevel.Red)]
    public void GetPreviousLevel_ShouldReturnPrecedingLevel(BadgeLevel current, BadgeLevel? expected)
    {
        // Act
        var result = _policy.GetPreviousLevel(current);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetPreviousLevel_ShouldThrow_WhenLevelIsUndefined()
    {
        var act = () => _policy.GetPreviousLevel(UndefinedLevel);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("current");
    }

    #endregion

    #region IsMaxLevel Tests

    [Theory]
    [InlineData(BadgeLevel.Gold, true)]
    [InlineData(BadgeLevel.Green, false)]
    [InlineData(BadgeLevel.Blue, false)]
    [InlineData(BadgeLevel.Red, false)]
    public void IsMaxLevel_ShouldOnlyReturnTrueForGold(BadgeLevel level, bool expected)
    {
        // Act
        var result = _policy.IsMaxLevel(level);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsMaxLevel_ShouldThrow_WhenLevelIsUndefined()
    {
        var act = () => _policy.IsMaxLevel(UndefinedLevel);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("level");
    }

    #endregion

    #region IsStartingLevel Tests

    [Theory]
    [InlineData(BadgeLevel.Green, true)]
    [InlineData(BadgeLevel.Blue, false)]
    [InlineData(BadgeLevel.Red, false)]
    [InlineData(BadgeLevel.Gold, false)]
    public void IsStartingLevel_ShouldOnlyReturnTrueForGreen(BadgeLevel level, bool expected)
    {
        // Act
        var result = _policy.IsStartingLevel(level);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsStartingLevel_ShouldThrow_WhenLevelIsUndefined()
    {
        var act = () => _policy.IsStartingLevel(UndefinedLevel);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("level");
    }

    #endregion

    #region GetLevelOrder Tests

    [Theory]
    [InlineData(BadgeLevel.Green, 1)]
    [InlineData(BadgeLevel.Blue, 2)]
    [InlineData(BadgeLevel.Red, 3)]
    [InlineData(BadgeLevel.Gold, 4)]
    public void GetLevelOrder_ShouldReturnCorrectOrder(BadgeLevel level, int expectedOrder)
    {
        // Act
        var result = _policy.GetLevelOrder(level);

        // Assert
        result.Should().Be(expectedOrder);
    }

    [Fact]
    public void GetLevelOrder_ShouldThrow_WhenLevelIsUndefined()
    {
        var act = () => _policy.GetLevelOrder(UndefinedLevel);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("level");
    }

    #endregion

    #region CompareLevels Tests

    [Theory]
    [InlineData(BadgeLevel.Green, BadgeLevel.Blue, -1)]
    [InlineData(BadgeLevel.Blue, BadgeLevel.Red, -1)]
    [InlineData(BadgeLevel.Red, BadgeLevel.Gold, -1)]
    [InlineData(BadgeLevel.Green, BadgeLevel.Gold, -1)]
    [InlineData(BadgeLevel.Blue, BadgeLevel.Green, 1)]
    [InlineData(BadgeLevel.Red, BadgeLevel.Blue, 1)]
    [InlineData(BadgeLevel.Gold, BadgeLevel.Red, 1)]
    [InlineData(BadgeLevel.Gold, BadgeLevel.Green, 1)]
    [InlineData(BadgeLevel.Green, BadgeLevel.Green, 0)]
    [InlineData(BadgeLevel.Blue, BadgeLevel.Blue, 0)]
    [InlineData(BadgeLevel.Red, BadgeLevel.Red, 0)]
    [InlineData(BadgeLevel.Gold, BadgeLevel.Gold, 0)]
    public void CompareLevels_ShouldReturnExpectedSign(BadgeLevel level1, BadgeLevel level2, int expectedSign)
    {
        // Act
        var result = _policy.CompareLevels(level1, level2);

        // Assert
        Math.Sign(result).Should().Be(expectedSign);
    }

    [Theory]
    [InlineData("level1")]
    [InlineData("level2")]
    public void CompareLevels_ShouldThrow_WhenEitherLevelIsUndefined(string undefinedParam)
    {
        var level1 = undefinedParam == "level1" ? UndefinedLevel : BadgeLevel.Green;
        var level2 = undefinedParam == "level2" ? UndefinedLevel : BadgeLevel.Green;

        var act = () => _policy.CompareLevels(level1, level2);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(undefinedParam);
    }

    #endregion
}
