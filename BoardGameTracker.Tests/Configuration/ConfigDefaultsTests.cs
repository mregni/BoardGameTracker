using System.Linq;
using BoardGameTracker.Common.Configuration;
using FluentAssertions;
using Xunit;
using static BoardGameTracker.Common.Constants;

namespace BoardGameTracker.Tests.Configuration;

public class ConfigDefaultsTests
{
    [Fact]
    public void All_ShouldNotBeEmpty()
    {
        ConfigDefaults.All.Should().NotBeEmpty();
    }

    [Fact]
    public void All_ShouldContainAllExpectedDefaults()
    {
        ConfigDefaults.All.Should().HaveCount(17);
    }

    [Fact]
    public void All_ShouldHaveUniqueKeys()
    {
        var keys = ConfigDefaults.All.Select(x => x.Key).ToList();

        keys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_ShouldNotContainNullOrEmptyKeys()
    {
        ConfigDefaults.All.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Key));
    }

    [Theory]
    [InlineData(AppConfig.Currency, "€")]
    [InlineData(AppConfig.DateFormat, "yy-MM-dd")]
    [InlineData(AppConfig.TimeFormat, "HH:mm")]
    [InlineData(AppConfig.UiLanguage, "en-us")]
    [InlineData(AppConfig.ShelfOfShameEnabled, "true")]
    [InlineData(AppConfig.ShelfOfShameMonths, "6")]
    [InlineData(AppConfig.GameNightsEnabled, "true")]
    [InlineData(AppConfig.PublicUrl, "http://localhost:5444")]
    [InlineData(AppConfig.RsvpAuthenticationEnabled, "false")]
    [InlineData(BggConfig.ApiKey, "")]
    [InlineData(UpdateConfig.Track, "stable")]
    [InlineData(UpdateConfig.CheckEnabled, "true")]
    [InlineData(UpdateConfig.CheckIntervalHours, "24")]
    [InlineData(UpdateConfig.CheckError, "")]
    [InlineData(UpdateConfig.CheckLastRun, "")]
    [InlineData(UpdateConfig.AvailableVersion, "")]
    [InlineData(UpdateConfig.Available, "false")]
    public void All_ShouldContainExpectedDefaultValue_ForEachKey(string key, string expectedValue)
    {
        var entry = ConfigDefaults.All.SingleOrDefault(x => x.Key == key);

        entry.Should().NotBeNull();
        entry!.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void ConfigDefault_ShouldSupportValueEquality()
    {
        var first = new ConfigDefault("key", "value");
        var second = new ConfigDefault("key", "value");
        var different = new ConfigDefault("key", "other");

        first.Should().Be(second);
        first.Should().NotBe(different);
    }
}
