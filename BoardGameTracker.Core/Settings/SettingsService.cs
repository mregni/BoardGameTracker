using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Settings;

public class SettingsService : ISettingsService
{
    private readonly IConfigRepository _configRepository;
    private readonly IUpdateService _updateService;
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(
        IConfigRepository configRepository,
        IUpdateService updateService,
        IEnvironmentProvider environmentProvider,
        ILogger<SettingsService> logger)
    {
        _configRepository = configRepository;
        _updateService = updateService;
        _environmentProvider = environmentProvider;
        _logger = logger;
    }

    public async Task<UIResourceDto> GetSettingsAsync()
    {
        _logger.LogDebug("Fetching settings");
        var configs = await _configRepository.GetAllConfigsAsync();

        return new UIResourceDto
        {
            TimeFormat = ResolveValue<string>(configs, Constants.AppConfig.TimeFormat),
            DateFormat = ResolveValue<string>(configs, Constants.AppConfig.DateFormat),
            UiLanguage = ResolveValue<string>(configs, Constants.AppConfig.UiLanguage),
            Currency = ResolveValue<string>(configs, Constants.AppConfig.Currency),
            Statistics = _environmentProvider.EnableStatistics,
            UpdateCheckEnabled = ResolveValue<bool>(configs, Constants.UpdateConfig.CheckEnabled),
            VersionTrack = ResolveValue<VersionTrack>(configs, Constants.UpdateConfig.Track),
            ShelfOfShameEnabled = ResolveValue<bool>(configs, Constants.AppConfig.ShelfOfShameEnabled),
            ShelfOfShameMonthsLimit = ResolveValue<int>(configs, Constants.AppConfig.ShelfOfShameMonths),
            GameNightsEnabled = ResolveValue<bool>(configs, Constants.AppConfig.GameNightsEnabled),
            PublicUrl = ResolveValue<string>(configs, Constants.AppConfig.PublicUrl),
            RsvpAuthenticationEnabled = ResolveValue<bool>(configs, Constants.AppConfig.RsvpAuthenticationEnabled)
        };
    }

    public async Task<UIResourceDto> UpdateSettingsAsync(UIResourceDto model)
    {
        _logger.LogDebug("Updating settings");
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.Currency, model.Currency);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.TimeFormat, model.TimeFormat);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.DateFormat, model.DateFormat);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.UiLanguage, model.UiLanguage);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameEnabled, model.ShelfOfShameEnabled);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameMonths, model.ShelfOfShameMonthsLimit);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.GameNightsEnabled, model.GameNightsEnabled);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.PublicUrl, model.PublicUrl);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.RsvpAuthenticationEnabled, model.RsvpAuthenticationEnabled);

        await _updateService.UpdateSettingsAsync(model.UpdateCheckEnabled, model.VersionTrack);
        _logger.LogInformation("Settings updated");

        return await GetSettingsAsync();
    }

    private static T ResolveValue<T>(Dictionary<string, string> configs, string key)
    {
        var envValue = Environment.GetEnvironmentVariable(key.ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(envValue) && TypeConverter.TryConvertFromString<T>(envValue.Trim(), out var envResult))
        {
            return envResult;
        }

        var normalizedKey = key.ToLowerInvariant();
        if (configs.TryGetValue(normalizedKey, out var value) && TypeConverter.TryConvertFromString<T>(value, out var dbResult))
        {
            return dbResult;
        }

        return default!;
    }
}
