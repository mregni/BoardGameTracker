using System.Collections.Generic;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BoardGameTracker.Tests.Common;

[Collection(EnvironmentVariablesCollection.Name)]
public class DateTimeProviderTests
{
    [Fact]
    public void UtcNow_ShouldReturnUtcDateTime()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);

        var result = provider.UtcNow;

        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Constructor_WithValidTimezone_ShouldUseConfiguredTimezone()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);

        provider.TimeZone.BaseUtcOffset.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void Constructor_WithInvalidTimezone_ShouldFallbackToUtc()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Invalid/Timezone" }
            })
            .Build();

        var provider = new DateTimeProvider(config);

        provider.TimeZone.Id.Should().Be(TimeZoneInfo.Utc.Id);
    }

    [Fact]
    public void Constructor_WithoutTimezone_ShouldDefaultToUtc()
    {
        var config = new ConfigurationBuilder().Build();

        var provider = new DateTimeProvider(config);

        provider.TimeZone.Id.Should().Be(TimeZoneInfo.Utc.Id);
    }

    [Fact]
    public void ConvertToLocalTime_ShouldConvertUtcToBrusselsTime()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);
        var utcTime = new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc);

        var localTime = provider.ConvertToLocalTime(utcTime);

        localTime.Should().Be(new DateTime(2026, 1, 7, 14, 30, 0));
    }

    [Fact]
    public void ConvertToLocalTime_WithNonUtcDateTime_ShouldThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);
        var localTime = new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Local);

        var act = () => provider.ConvertToLocalTime(localTime);

        act.Should().Throw<ArgumentException>().WithMessage("*must be in UTC*");
    }

    [Fact]
    public void ConvertToUtc_ShouldConvertLocalToUtc()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);
        var brusselsTime = new DateTime(2026, 1, 7, 14, 30, 0, DateTimeKind.Unspecified);

        var utcTime = provider.ConvertToUtc(brusselsTime);

        utcTime.Kind.Should().Be(DateTimeKind.Utc);
        utcTime.Should().Be(new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ConvertToUtc_WithAlreadyUtc_ShouldReturnSameValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "Europe/Brussels" }
            })
            .Build();

        var provider = new DateTimeProvider(config);
        var utcTime = new DateTime(2026, 1, 7, 13, 30, 0, DateTimeKind.Utc);

        var result = provider.ConvertToUtc(utcTime);

        result.Should().Be(utcTime);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ConvertToUtc_ShouldShiftForward_WhenTheLocalTimeFallsInTheDaylightSavingGap()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TZ", "America/Santiago" }
            })
            .Build();
        var provider = new DateTimeProvider(config);
        var midnight = new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Unspecified);

        var result = provider.ConvertToUtc(midnight);

        result.Should().Be(new DateTime(2026, 9, 6, 4, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Constructor_WithoutConfiguredTimezone_ShouldUseTheEnvironmentVariable()
    {
        var original = Environment.GetEnvironmentVariable("TZ");
        Environment.SetEnvironmentVariable("TZ", "Asia/Tokyo");
        try
        {
            var provider = new DateTimeProvider(new ConfigurationBuilder().Build());

            provider.TimeZone.BaseUtcOffset.Should().Be(TimeSpan.FromHours(9));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TZ", original);
        }
    }

    [Fact]
    public void Constructor_ShouldPreferTheConfiguredTimezone_OverTheEnvironmentVariable()
    {
        var original = Environment.GetEnvironmentVariable("TZ");
        Environment.SetEnvironmentVariable("TZ", "Asia/Tokyo");
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { { "TZ", "Europe/Brussels" } })
                .Build();

            new DateTimeProvider(config).TimeZone.BaseUtcOffset.Should().Be(TimeSpan.FromHours(1));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TZ", original);
        }
    }

    [Fact]
    public void Now_ShouldBeTheCurrentTimeInTheConfiguredTimezone()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "TZ", "Asia/Tokyo" } })
            .Build();
        var provider = new DateTimeProvider(config);

        provider.Now.Should().BeCloseTo(DateTime.UtcNow.AddHours(9), TimeSpan.FromSeconds(5));
        provider.UtcNow.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
