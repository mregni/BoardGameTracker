using System;
using System.Collections.Generic;
using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class SessionTimeRangeTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidTimes_ShouldSetStartAndEnd()
    {
        var start = new DateTime(2024, 1, 15, 10, 0, 0);
        var end = new DateTime(2024, 1, 15, 12, 0, 0);

        var range = new SessionTimeRange(start, end);

        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Fact]
    public void Constructor_WithSameStartAndEnd_ShouldSucceed()
    {
        var time = new DateTime(2024, 1, 15, 10, 0, 0);

        var range = new SessionTimeRange(time, time);

        range.Start.Should().Be(time);
        range.End.Should().Be(time);
    }

    [Fact]
    public void Constructor_WithEndBeforeStart_ShouldThrowException()
    {
        var start = new DateTime(2024, 1, 15, 12, 0, 0);
        var end = new DateTime(2024, 1, 15, 10, 0, 0);

        Action act = () => new SessionTimeRange(start, end);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*end time cannot be before start time*")
            .WithParameterName("end");
    }

    public static IEnumerable<object[]> DefaultGuardCases()
    {
        var valid = new DateTime(2024, 1, 15, 12, 0, 0);
        yield return new object[] { default(DateTime), valid, "start" };
        yield return new object[] { valid, default(DateTime), "end" };
        yield return new object[] { default(DateTime), default(DateTime), "start" };
    }

    [Theory]
    [MemberData(nameof(DefaultGuardCases))]
    public void Constructor_WithDefaultDateTime_ShouldThrowForOffendingParameter(DateTime start, DateTime end, string expectedParam)
    {
        Action act = () => new SessionTimeRange(start, end);

        act.Should().Throw<ArgumentException>().WithParameterName(expectedParam);
    }

    #endregion

    #region Duration Tests

    public static IEnumerable<object[]> DurationCases()
    {
        yield return new object[] { new DateTime(2024, 1, 15, 10, 0, 0), new DateTime(2024, 1, 15, 12, 0, 0), TimeSpan.FromHours(2) };
        yield return new object[] { new DateTime(2024, 1, 15, 10, 0, 0), new DateTime(2024, 1, 15, 10, 0, 0), TimeSpan.Zero };
        yield return new object[] { new DateTime(2024, 1, 15, 10, 0, 0), new DateTime(2024, 1, 15, 10, 0, 1), TimeSpan.FromSeconds(1) };
        yield return new object[] { new DateTime(2024, 1, 15, 0, 0, 0), new DateTime(2024, 1, 16, 0, 0, 0), TimeSpan.FromDays(1) };
        yield return new object[] { new DateTime(2024, 1, 15, 10, 0, 0, 0), new DateTime(2024, 1, 15, 10, 0, 0, 100), TimeSpan.FromMilliseconds(100) };
    }

    [Theory]
    [MemberData(nameof(DurationCases))]
    public void Duration_ShouldCalculateCorrectly(DateTime start, DateTime end, TimeSpan expected)
    {
        var range = new SessionTimeRange(start, end);

        range.Duration.Should().Be(expected);
    }

    #endregion

    #region DateTime Kind Tests

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    public void Constructor_ShouldPreserveDateTimeKind(DateTimeKind kind)
    {
        var start = new DateTime(2024, 1, 15, 10, 0, 0, kind);
        var end = new DateTime(2024, 1, 15, 12, 0, 0, kind);

        var range = new SessionTimeRange(start, end);

        range.Start.Kind.Should().Be(kind);
        range.End.Kind.Should().Be(kind);
    }

    #endregion
}
