using System;
using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class SessionTimeRangeTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidTimes_ShouldSetStart()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = new DateTime(2024, 1, 15, 12, 0, 0);

        // Act
        var range = new SessionTimeRange(start, end);

        // Assert
        range.Start.Should().Be(start);
    }

    [Fact]
    public void Constructor_WithValidTimes_ShouldSetEnd()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = new DateTime(2024, 1, 15, 12, 0, 0);

        // Act
        var range = new SessionTimeRange(start, end);

        // Assert
        range.End.Should().Be(end);
    }

    [Fact]
    public void Constructor_WithSameStartAndEnd_ShouldSucceed()
    {
        // Arrange
        var time = new DateTime(2024, 1, 15, 10, 0, 0);

        // Act
        var range = new SessionTimeRange(time, time);

        // Assert
        range.Start.Should().Be(time);
        range.End.Should().Be(time);
    }

    [Fact]
    public void Constructor_WithEndBeforeStart_ShouldThrowException()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 12, 0, 0);
        var end = new DateTime(2024, 1, 15, 10, 0, 0);

        // Act
        Action act = () => new SessionTimeRange(start, end);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*end time cannot be before start time*");
    }

    [Fact]
    public void Constructor_WithDefaultStart_ShouldThrowException()
    {
        // Arrange
        var start = default(DateTime);
        var end = new DateTime(2024, 1, 15, 12, 0, 0);

        // Act
        Action act = () => new SessionTimeRange(start, end);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithDefaultEnd_ShouldThrowException()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = default(DateTime);

        // Act
        Action act = () => new SessionTimeRange(start, end);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithBothDefault_ShouldThrowException()
    {
        // Arrange
        var start = default(DateTime);
        var end = default(DateTime);

        // Act
        Action act = () => new SessionTimeRange(start, end);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Duration Tests

    [Fact]
    public void Duration_ShouldCalculateCorrectly()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = new DateTime(2024, 1, 15, 12, 0, 0);
        var range = new SessionTimeRange(start, end);

        // Act
        var duration = range.Duration;

        // Assert
        duration.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void Duration_WithSameStartAndEnd_ShouldBeZero()
    {
        // Arrange
        var time = new DateTime(2024, 1, 15, 10, 0, 0);
        var range = new SessionTimeRange(time, time);

        // Act
        var duration = range.Duration;

        // Assert
        duration.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithOneSecondDifference_ShouldSucceed()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = new DateTime(2024, 1, 15, 10, 0, 1);

        // Act
        var range = new SessionTimeRange(start, end);

        // Assert
        range.Duration.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithLongSession_ShouldSucceed()
    {
        // Arrange - 24 hour session
        var start = new DateTime(2024, 1, 15, 0, 0, 0);
        var end = new DateTime(2024, 1, 16, 0, 0, 0);

        // Act
        var range = new SessionTimeRange(start, end);

        // Assert
        range.Duration.Should().Be(TimeSpan.FromDays(1));
    }

    [Fact]
    public void Constructor_WithMillisecondDifference_ShouldSucceed()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0, 0);
        var end = new DateTime(2024, 1, 15, 10, 0, 0, 100);

        // Act
        var range = new SessionTimeRange(start, end);

        // Assert
        range.Duration.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    #endregion

    #region DateTime Kind Tests

    [Fact]
    public void Constructor_WithUtcTimes_ShouldSucceed()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var range = new SessionTimeRange(start, end);

        // Assert
        range.Start.Kind.Should().Be(DateTimeKind.Utc);
        range.End.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Constructor_WithLocalTimes_ShouldSucceed()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Local);
        var end = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Local);

        // Act
        var range = new SessionTimeRange(start, end);

        // Assert
        range.Start.Kind.Should().Be(DateTimeKind.Local);
        range.End.Kind.Should().Be(DateTimeKind.Local);
    }

    #endregion
}
