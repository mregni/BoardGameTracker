using System.Linq;
using BoardGameTracker.Common.Configuration;
using FluentAssertions;
using Xunit;
using static BoardGameTracker.Common.Constants;

namespace BoardGameTracker.Tests.Configuration;

public class ConfigDefaultsTests
{
    public static TheoryData<string, string> ExpectedDefaults => new()
    {
        { AppConfig.Currency, "€" },
        { AppConfig.DateFormat, "dd-MM-yyyy" },
        { AppConfig.TimeFormat, "HH:mm" },
        { AppConfig.UiLanguage, "en-us" },
        { AppConfig.ShelfOfShameEnabled, "true" },
        { AppConfig.ShelfOfShameMonths, "6" },
        { AppConfig.GameNightsEnabled, "true" },
        { AppConfig.PublicUrl, "" },
        { AppConfig.RsvpAuthenticationEnabled, "false" },
        { BggConfig.ApiKey, "" },
        { ChangeDetectionConfig.BaseUrl, "" },
        { ChangeDetectionConfig.ApiKey, "" },
        { AiConfig.Provider, "ollama" },
        { AiConfig.BaseUrl, "http://ollama:11434" },
        { AiConfig.ChatModel, "qwen3:4b" },
        { AiConfig.ApiKey, "" },
        { AiConfig.EmbeddingBaseUrl, "http://ollama:11434" },
        { AiConfig.EmbeddingNumGpu, "-1" },
        { AiConfig.TopK, "5" },
        { UpdateConfig.Track, "stable" },
        { UpdateConfig.CheckEnabled, "true" },
        { UpdateConfig.CheckIntervalHours, "24" },
        { UpdateConfig.CheckError, "" },
        { UpdateConfig.CheckLastRun, "" },
        { UpdateConfig.AvailableVersion, "" },
        { UpdateConfig.Available, "false" },
    };

    [Fact]
    public void All_ShouldContainExactlyTheExpectedKeys()
    {
        var expectedKeys = ExpectedDefaults.Select(x => x.Data.Item1);

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
