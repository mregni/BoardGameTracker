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

    [Fact]
    public void Duration_WithMinutes_ShouldCalculateCorrectly()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = new DateTime(2024, 1, 15, 10, 45, 0);
        var range = new SessionTimeRange(start, end);

        // Act
        var duration = range.Duration;

        // Assert
        duration.Should().Be(TimeSpan.FromMinutes(45));
    }

    [Fact]
    public void Duration_SpanningDays_ShouldCalculateCorrectly()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 22, 0, 0);
        var end = new DateTime(2024, 1, 16, 2, 0, 0);
        var range = new SessionTimeRange(start, end);

        // Act
        var duration = range.Duration;

        // Assert
        duration.Should().Be(TimeSpan.FromHours(4));
    }

    [Fact]
    public void Duration_WithSeconds_ShouldCalculateCorrectly()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = new DateTime(2024, 1, 15, 10, 0, 30);
        var range = new SessionTimeRange(start, end);

        // Act
        var duration = range.Duration;

        // Assert
        duration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void Duration_WithVariousHours_ShouldCalculateCorrectly(int hours)
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = start.AddHours(hours);
        var range = new SessionTimeRange(start, end);

        // Act
        var duration = range.Duration;

        // Assert
        duration.Should().Be(TimeSpan.FromHours(hours));
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = new DateTime(2024, 1, 15, 12, 0, 0);
        var range1 = new SessionTimeRange(start, end);
        var range2 = new SessionTimeRange(start, end);

        // Assert
        range1.Should().Be(range2);
    }

    [Fact]
    public void Equality_DifferentStart_ShouldNotBeEqual()
    {
        // Arrange
        var start1 = new DateTime(2024, 1, 15, 10, 0, 0);
        var start2 = new DateTime(2024, 1, 15, 11, 0, 0);
        var end = new DateTime(2024, 1, 15, 14, 0, 0);
        var range1 = new SessionTimeRange(start1, end);
        var range2 = new SessionTimeRange(start2, end);

        // Assert
        range1.Should().NotBe(range2);
    }

    [Fact]
    public void Equality_DifferentEnd_ShouldNotBeEqual()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end1 = new DateTime(2024, 1, 15, 12, 0, 0);
        var end2 = new DateTime(2024, 1, 15, 13, 0, 0);
        var range1 = new SessionTimeRange(start, end1);
        var range2 = new SessionTimeRange(start, end2);

        // Assert
        range1.Should().NotBe(range2);
    }

    [Fact]
    public void GetHashCode_SameValues_ShouldBeSame()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = new DateTime(2024, 1, 15, 12, 0, 0);
        var range1 = new SessionTimeRange(start, end);
        var range2 = new SessionTimeRange(start, end);

        // Assert
        range1.GetHashCode().Should().Be(range2.GetHashCode());
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

    [Fact]
    public void Duration_ShouldBeReadOnlyComputed()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = new DateTime(2024, 1, 15, 12, 0, 0);
        var range = new SessionTimeRange(start, end);

        // Act - Call Duration multiple times
        var duration1 = range.Duration;
        var duration2 = range.Duration;

        // Assert - Should always return the same value
        duration1.Should().Be(duration2);
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
