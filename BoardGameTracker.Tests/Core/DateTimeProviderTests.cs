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
    }

    [Fact]
    public void Constructor_WithValidTimezone_ShouldUseConfiguredTimezone()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        // Act
        var provider = new DateTimeProvider(config);

        // Assert
        provider.TimeZone.Id.Should().Be("Europe/Brussels");
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
        var utcTime = new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc);

        // Act
        var localTime = provider.ConvertToLocalTime(utcTime);

        // Assert
        localTime.Should().Be(new DateTime(2026, 1, 7, 14, 30, 0));
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
        utcTime.Should().Be(new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc));
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
}
