using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Core.Updates.Interfaces;

namespace BoardGameTracker.Core.Settings;

public class SettingsService : ISettingsService
{
    private readonly IConfigRepository _configRepository;
    private readonly IUpdateService _updateService;
    private readonly IEnvironmentProvider _environmentProvider;

    public SettingsService(
        IConfigRepository configRepository,
        IUpdateService updateService,
        IEnvironmentProvider environmentProvider)
    {
        _configRepository = configRepository;
        _updateService = updateService;
        _environmentProvider = environmentProvider;
    }

    public async Task<UIResourceDto> GetSettingsAsync()
    {
        var updateSettings = await _updateService.GetUpdateSettingsAsync();

        return new UIResourceDto
        {
            TimeFormat = await _configRepository.GetConfigValueAsync<string>(Constants.AppConfig.TimeFormat),
            DateFormat = await _configRepository.GetConfigValueAsync<string>(Constants.AppConfig.DateFormat),
            UiLanguage = await _configRepository.GetConfigValueAsync<string>(Constants.AppConfig.UiLanguage),
            Currency = await _configRepository.GetConfigValueAsync<string>(Constants.AppConfig.Currency),
            Statistics = _environmentProvider.EnableStatistics,
            UpdateCheckEnabled = updateSettings.Enabled,
            ShelfOfShameEnabled = await _configRepository.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled),
            ShelfOfShameMonthsLimit = await _configRepository.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths),
            VersionTrack = updateSettings.VersionTrack,
            GameNightsEnabled = await _configRepository.GetConfigValueAsync<bool>(Constants.AppConfig.GameNightsEnabled),
            PublicUrl = await _configRepository.GetConfigValueAsync<string>(Constants.AppConfig.PublicUrl)
        };
    }

    public async Task UpdateSettingsAsync(UIResourceDto model)
    {
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.Currency, model.Currency);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.TimeFormat, model.TimeFormat);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.DateFormat, model.DateFormat);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.UiLanguage, model.UiLanguage);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameEnabled, model.ShelfOfShameEnabled);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameMonths, model.ShelfOfShameMonthsLimit);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.GameNightsEnabled, model.GameNightsEnabled);
        await _configRepository.SetConfigValueAsync(Constants.AppConfig.PublicUrl, model.PublicUrl);

        await _updateService.UpdateSettingsAsync(model.UpdateCheckEnabled, model.VersionTrack);
    }
}
