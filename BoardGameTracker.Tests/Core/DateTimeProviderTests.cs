using BoardGameTracker.Core.Common;
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
        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.True((DateTime.UtcNow - result).TotalSeconds < 1, "UtcNow should return current time");
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
        Assert.Equal("Europe/Brussels", provider.TimeZone.Id);

        // Brussels is typically UTC+1 or UTC+2 (DST)
        var offset = provider.TimeZone.GetUtcOffset(utcNow);
        Assert.True(offset.TotalHours is >= 1 and <= 2);

        // Local time should be ahead of UTC for Brussels
        Assert.True(localNow > utcNow);
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
        Assert.Equal(TimeZoneInfo.Utc.Id, provider.TimeZone.Id);
    }

    [Fact]
    public void Constructor_WithoutTimezone_ShouldDefaultToUtc()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();

        // Act
        var provider = new DateTimeProvider(config);

        // Assert
        Assert.Equal(TimeZoneInfo.Utc.Id, provider.TimeZone.Id);
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
        Assert.True(localTime.Hour == 14 || localTime.Hour == 15); // Account for DST
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

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => provider.ConvertToLocalTime(localTime));
        Assert.Contains("must be in UTC", ex.Message);
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
        Assert.Equal(DateTimeKind.Utc, utcTime.Kind);
        // Brussels is UTC+1 in winter, so 14:30 local = 13:30 UTC
        Assert.True(utcTime.Hour == 13 || utcTime.Hour == 12); // Account for DST
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
        Assert.Equal(utcTime, result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
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
        Assert.Equal(timezoneId, provider.TimeZone.Id);
        Assert.NotNull(provider.UtcNow);
    }
}
