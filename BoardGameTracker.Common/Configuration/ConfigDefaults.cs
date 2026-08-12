using static BoardGameTracker.Common.Constants;

namespace BoardGameTracker.Common.Configuration;

public record ConfigDefault(string Key, string Value);

public static class ConfigDefaults
{
    public static IReadOnlyList<ConfigDefault> All { get; } = new List<ConfigDefault>
    {
        new(AppConfig.Currency, "€"),
        new(AppConfig.DateFormat, "yy-MM-dd"),
        new(AppConfig.TimeFormat, "HH:mm"),
        new(AppConfig.UiLanguage, "en-us"),
        new(AppConfig.ShelfOfShameEnabled, "true"),
        new(AppConfig.ShelfOfShameMonths, "6"),
        new(AppConfig.GameNightsEnabled, "true"),
        new(AppConfig.PublicUrl, "http://localhost:5444"),
        new(AppConfig.RsvpAuthenticationEnabled, "false"),

        new(BggConfig.ApiKey, ""),

        new(AiConfig.Provider, AiConfig.OllamaProvider),
        new(AiConfig.BaseUrl, "http://ollama:11434"),
        new(AiConfig.ChatModel, "qwen3:4b"),
        new(AiConfig.ApiKey, ""),
        new(AiConfig.TopK, "5"),

        new(UpdateConfig.Track, "stable"),
        new(UpdateConfig.CheckEnabled, "true"),
        new(UpdateConfig.CheckIntervalHours, "24"),
        new(UpdateConfig.CheckError, ""),
        new(UpdateConfig.CheckLastRun, ""),
        new(UpdateConfig.AvailableVersion, ""),
        new(UpdateConfig.Available, "false"),
    };
}
