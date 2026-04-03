using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Settings;

public class SettingsService : ISettingsService
{
    private readonly IConfigRepository _configRepository;
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(
        IConfigRepository configRepository,
        IEnvironmentProvider environmentProvider,
        ILogger<SettingsService> logger)
    {
        _configRepository = configRepository;
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
            Statistics = _environmentProvider.StatisticsEnabled,
            UpdateCheckEnabled = ResolveValue<bool>(configs, Constants.UpdateConfig.CheckEnabled),
            VersionTrack = ResolveValue<VersionTrack>(configs, Constants.UpdateConfig.Track),
            ShelfOfShameEnabled = ResolveValue<bool>(configs, Constants.AppConfig.ShelfOfShameEnabled),
            ShelfOfShameMonthsLimit = ResolveValue<int>(configs, Constants.AppConfig.ShelfOfShameMonths),
            GameNightsEnabled = ResolveValue<bool>(configs, Constants.AppConfig.GameNightsEnabled),
            PublicUrl = ResolveValue<string>(configs, Constants.AppConfig.PublicUrl),
            RsvpAuthenticationEnabled = ResolveValue<bool>(configs, Constants.AppConfig.RsvpAuthenticationEnabled),
            BggStatus = GetBggConfigStatusAsync(configs),
            BggApiKey = string.Empty //Never return key to UI
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
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameMonths,
            model.ShelfOfShameMonthsLimit);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.GameNightsEnabled, model.GameNightsEnabled);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.PublicUrl, model.PublicUrl);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.RsvpAuthenticationEnabled,
            model.RsvpAuthenticationEnabled);
        await _configRepository.SetConfigValueAsync(Constants.BggConfig.ApiKey, model.BggApiKey);
        await _configRepository.SetConfigValueAsync(Constants.UpdateConfig.CheckEnabled, model.UpdateCheckEnabled);
        await _configRepository.SetConfigValueAsync(Constants.UpdateConfig.Track, model.VersionTrack);

        return await GetSettingsAsync();
    }

    public async Task<string?> GetBggApiKeyAsync()
    {
        var envValue = Environment.GetEnvironmentVariable(Constants.BggConfig.EnvApiKeyName);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue.Trim();
        }

        return await _configRepository.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey);
    }

    public async Task<bool> IsBggEnabled()
    {
        return string.IsNullOrEmpty(await GetBggApiKeyAsync());
    }

    private static BggConfigStatusDto GetBggConfigStatusAsync(Dictionary<string, string> configs)
    {
        var envValue = Environment.GetEnvironmentVariable(Constants.BggConfig.EnvApiKeyName);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return new BggConfigStatusDto
            {
                IsConfigured = true,
                Source = "env",
                IsReadOnly = true
            };
        }

        var dbValue = ResolveValue<string>(configs, Constants.BggConfig.ApiKey);
        if (!string.IsNullOrWhiteSpace(dbValue))
        {
            return new BggConfigStatusDto
            {
                IsConfigured = true,
                Source = "db",
                IsReadOnly = false
            };
        }

        return new BggConfigStatusDto
        {
            IsConfigured = false,
            Source = "none",
            IsReadOnly = false
        };
    }

    private static T ResolveValue<T>(Dictionary<string, string> configs, string key)
    {
        var envValue = Environment.GetEnvironmentVariable(key.ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(envValue) &&
            TypeConverter.TryConvertFromString<T>(envValue.Trim(), out var envResult))
        {
            return envResult;
        }

        var normalizedKey = key.ToLowerInvariant();
        if (configs.TryGetValue(normalizedKey, out var value) &&
            TypeConverter.TryConvertFromString<T>(value, out var dbResult))
        {
            return dbResult;
        }

        return default!;
    }
}