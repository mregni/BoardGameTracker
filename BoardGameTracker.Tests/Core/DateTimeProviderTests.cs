using System;
using System.Collections.Generic;
using BoardGameTracker.Core.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BoardGameTracker.Tests.Core;

public class DateTimeProviderTests
{
    [Fact]
    public void UtcNow_ShouldReturnUtcDateTime()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);

        // Act
        var result = provider.UtcNow;

        // Assert
        result.Kind.Should().Be(DateTimeKind.Utc);
        (DateTime.UtcNow - result).TotalSeconds.Should().BeLessThan(1, "UtcNow should return current time");
    }

    [Fact]
    public void Now_WithBrusselsTimezone_ShouldReturnLocalTime()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);

        // Act
        var utcNow = provider.UtcNow;
        var localNow = provider.Now;

        // Assert
        provider.TimeZone.Id.Should().Be("Europe/Brussels");

        // Brussels is typically UTC+1 or UTC+2 (DST)
        var offset = provider.TimeZone.GetUtcOffset(utcNow);
        offset.TotalHours.Should().BeInRange(1, 2);

        // Local time should be ahead of UTC for Brussels
        localNow.Should().BeAfter(utcNow);
    }

    [Fact]
    public void Constructor_WithInvalidTimezone_ShouldFallbackToUtc()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Invalid/Timezone" }
            })
            .Build();

        // Act
        var provider = new DateTimeProvider(config);

        // Assert
        provider.TimeZone.Id.Should().Be(TimeZoneInfo.Utc.Id);
    }

    [Fact]
    public void Constructor_WithoutTimezone_ShouldDefaultToUtc()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();

        // Act
        var provider = new DateTimeProvider(config);

        // Assert
        provider.TimeZone.Id.Should().Be(TimeZoneInfo.Utc.Id);
    }

    [Fact]
    public void ConvertToLocalTime_ShouldConvertUtcToBrusselsTime()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);
        var utcTime = new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc); // 13:30 UTC

        // Act
        var localTime = provider.ConvertToLocalTime(utcTime);

        // Assert
        // Brussels is UTC+1 in winter, so 13:30 UTC = 14:30 local
        localTime.Hour.Should().BeOneOf(14, 15); // Account for DST
    }

    [Fact]
    public void ConvertToLocalTime_WithNonUtcDateTime_ShouldThrow()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);
        var localTime = new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Local);

        // Act
        var act = () => provider.ConvertToLocalTime(localTime);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*must be in UTC*");
    }

    [Fact]
    public void ConvertToUtc_ShouldConvertLocalToUtc()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);
        var brusselsTime = new DateTime(2026, 1, 7, 14, 30, 0, DateTimeKind.Unspecified);

        // Act
        var utcTime = provider.ConvertToUtc(brusselsTime);

        // Assert
        utcTime.Kind.Should().Be(DateTimeKind.Utc);
        // Brussels is UTC+1 in winter, so 14:30 local = 13:30 UTC
        utcTime.Hour.Should().BeOneOf(12, 13); // Account for DST
    }

    [Fact]
    public void ConvertToUtc_WithAlreadyUtc_ShouldReturnSameValue()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);
        var utcTime = new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc);

        // Act
        var result = provider.ConvertToUtc(utcTime);

        // Assert
        result.Should().Be(utcTime);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("America/New_York")]
    [InlineData("Asia/Tokyo")]
    [InlineData("Australia/Sydney")]
    [InlineData("Europe/London")]
    public void Constructor_WithVariousTimezones_ShouldWork(string timezoneId)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", timezoneId }
            })
            .Build();

        // Act
        var provider = new DateTimeProvider(config);

        // Assert
        provider.TimeZone.Id.Should().Be(timezoneId);
        provider.UtcNow.Should().NotBe(default);
    }
}
