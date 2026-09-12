using System.Collections.Generic;
using System.Linq;
using BoardGameTracker.Common.Configuration;
using FluentAssertions;
using Xunit;
using static BoardGameTracker.Common.Constants;

namespace BoardGameTracker.Tests.Configuration;

public class ConfigDefaultsTests
{
    public static IEnumerable<object[]> ExpectedDefaults()
    {
        yield return new object[] { AppConfig.Currency, "€" };
        yield return new object[] { AppConfig.DateFormat, "yy-MM-dd" };
        yield return new object[] { AppConfig.TimeFormat, "HH:mm" };
        yield return new object[] { AppConfig.UiLanguage, "en-us" };
        yield return new object[] { AppConfig.ShelfOfShameEnabled, "true" };
        yield return new object[] { AppConfig.ShelfOfShameMonths, "6" };
        yield return new object[] { AppConfig.GameNightsEnabled, "true" };
        yield return new object[] { AppConfig.PublicUrl, "http://localhost:5444" };
        yield return new object[] { AppConfig.RsvpAuthenticationEnabled, "false" };
        yield return new object[] { BggConfig.ApiKey, "" };
        yield return new object[] { ChangeDetectionConfig.BaseUrl, "" };
        yield return new object[] { ChangeDetectionConfig.ApiKey, "" };
        yield return new object[] { AiConfig.Provider, "ollama" };
        yield return new object[] { AiConfig.BaseUrl, "http://ollama:11434" };
        yield return new object[] { AiConfig.ChatModel, "qwen3:4b" };
        yield return new object[] { AiConfig.ApiKey, "" };
        yield return new object[] { AiConfig.EmbeddingBaseUrl, "http://ollama:11434" };
        yield return new object[] { AiConfig.EmbeddingNumGpu, "-1" };
        yield return new object[] { AiConfig.TopK, "5" };
        yield return new object[] { UpdateConfig.Track, "stable" };
        yield return new object[] { UpdateConfig.CheckEnabled, "true" };
        yield return new object[] { UpdateConfig.CheckIntervalHours, "24" };
        yield return new object[] { UpdateConfig.CheckError, "" };
        yield return new object[] { UpdateConfig.CheckLastRun, "" };
        yield return new object[] { UpdateConfig.AvailableVersion, "" };
        yield return new object[] { UpdateConfig.Available, "false" };
    }

    [Fact]
    public void All_ShouldContainExactlyTheExpectedKeys()
    {
        var expectedKeys = ExpectedDefaults().Select(x => (string)x[0]);

        ConfigDefaults.All.Select(x => x.Key).Should().BeEquivalentTo(expectedKeys);
    }

    [Fact]
    public void All_ShouldHaveUniqueKeys()
    {
        ConfigDefaults.All.Select(x => x.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_ShouldNotContainNullOrEmptyKeys()
    {
        ConfigDefaults.All.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Key));
    }

    [Fact]
    public void All_ShouldNotContainNullValues()
    {
        ConfigDefaults.All.Should().OnlyContain(x => x.Value != null);
    }

    [Theory]
    [MemberData(nameof(ExpectedDefaults))]
    public void All_ShouldContainExpectedDefaultValue_ForEachKey(string key, string expectedValue)
    {
        var entry = ConfigDefaults.All.SingleOrDefault(x => x.Key == key);

        entry.Should().NotBeNull();
        entry!.Value.Should().Be(expectedValue);
    }
}
