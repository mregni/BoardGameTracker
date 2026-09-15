using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Settings;

public class SettingsService : ISettingsService
{
    private readonly IConfigRepository _configRepository;
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(
        IConfigRepository configRepository,
        IEnvironmentProvider environmentProvider,
        IUnitOfWork unitOfWork,
        ILogger<SettingsService> logger)
    {
        _configRepository = configRepository;
        _environmentProvider = environmentProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UIResourceDto> GetSettingsAsync()
    {
        _logger.LogDebug("Fetching settings");
        var configs = await _configRepository.GetAllConfigsAsync();
        var overrides = new Dictionary<string, string>();

        T Resolve<T>(string field, string key, T fallback = default!)
        {
            var value = ResolveValue(configs, key, fallback, out var fromEnvironment);
            if (fromEnvironment)
            {
                overrides[ToCamelCase(field)] = EnvironmentVariableFor(key);
            }

            return value;
        }

        return new UIResourceDto
        {
            TimeFormat = Resolve<string>(nameof(UIResourceDto.TimeFormat), Constants.AppConfig.TimeFormat),
            DateFormat = Resolve<string>(nameof(UIResourceDto.DateFormat), Constants.AppConfig.DateFormat),
            UiLanguage = Resolve<string>(nameof(UIResourceDto.UiLanguage), Constants.AppConfig.UiLanguage),
            Currency = Resolve<string>(nameof(UIResourceDto.Currency), Constants.AppConfig.Currency),
            Statistics = _environmentProvider.StatisticsEnabled,
            UpdateCheckEnabled = Resolve<bool>(nameof(UIResourceDto.UpdateCheckEnabled), Constants.UpdateConfig.CheckEnabled),
            VersionTrack = Resolve<VersionTrack>(nameof(UIResourceDto.VersionTrack), Constants.UpdateConfig.Track),
            ShelfOfShameEnabled = Resolve<bool>(nameof(UIResourceDto.ShelfOfShameEnabled), Constants.AppConfig.ShelfOfShameEnabled),
            ShelfOfShameMonthsLimit = Resolve(nameof(UIResourceDto.ShelfOfShameMonthsLimit), Constants.AppConfig.ShelfOfShameMonths, Constants.AppConfig.DefaultShelfOfShameMonths),
            GameNightsEnabled = Resolve<bool>(nameof(UIResourceDto.GameNightsEnabled), Constants.AppConfig.GameNightsEnabled),
            PublicUrl = Resolve<string>(nameof(UIResourceDto.PublicUrl), Constants.AppConfig.PublicUrl),
            RsvpAuthenticationEnabled = Resolve<bool>(nameof(UIResourceDto.RsvpAuthenticationEnabled), Constants.AppConfig.RsvpAuthenticationEnabled),
            EmailEnabled = _environmentProvider.EmailEnabled,
            RagEnabled = _environmentProvider.RagEnabled,
            BggStatus = GetBggConfigStatusAsync(configs),
            BggApiKey = string.Empty, //Never return key to UI
            ChangeDetectionBaseUrl = configs.GetValueOrDefault(
                Constants.ChangeDetectionConfig.BaseUrl.ToLowerInvariant(), string.Empty), //DB value only, never the env override
            ChangeDetectionStatus = GetChangeDetectionConfigStatus(configs),
            ChangeDetectionApiKey = string.Empty, //Never return key to UI
            EnvironmentOverrides = overrides
        };
    }

    public async Task<UIResourceDto> UpdateSettingsAsync(UIResourceDto model)
    {
        _logger.LogDebug("Updating settings");
        var publicUrl = ValidateOptionalHttpUrl(model.PublicUrl, Constants.Errors.SettingsInvalidPublicUrl);
        var changeDetectionBaseUrl = ValidateOptionalHttpUrl(model.ChangeDetectionBaseUrl,
            Constants.Errors.ChangeDetectionInvalidBaseUrl);

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        await SetUnlessOverriddenAsync(Constants.AppConfig.Currency, model.Currency);
        await SetUnlessOverriddenAsync(Constants.AppConfig.TimeFormat, model.TimeFormat);
        await SetUnlessOverriddenAsync(Constants.AppConfig.DateFormat, model.DateFormat);
        await SetUnlessOverriddenAsync(Constants.AppConfig.UiLanguage, model.UiLanguage);
        await SetUnlessOverriddenAsync(Constants.AppConfig.ShelfOfShameEnabled, model.ShelfOfShameEnabled);
        await SetUnlessOverriddenAsync(Constants.AppConfig.ShelfOfShameMonths, model.ShelfOfShameMonthsLimit);
        await SetUnlessOverriddenAsync(Constants.AppConfig.GameNightsEnabled, model.GameNightsEnabled);
        await SetUnlessOverriddenAsync(Constants.AppConfig.PublicUrl, publicUrl);
        await SetUnlessOverriddenAsync(Constants.AppConfig.RsvpAuthenticationEnabled, model.RsvpAuthenticationEnabled);
        await SetUnlessOverriddenAsync(Constants.UpdateConfig.CheckEnabled, model.UpdateCheckEnabled);
        await SetUnlessOverriddenAsync(Constants.UpdateConfig.Track, model.VersionTrack);
        await _configRepository.SetConfigValueAsync(Constants.ChangeDetectionConfig.BaseUrl, changeDetectionBaseUrl);

        if (!IsOverriddenByEnvironment(Constants.BggConfig.EnvApiKeyName))
        {
            await StoreSecretAsync(Constants.BggConfig.ApiKey, model.BggApiKey);
        }

        await StoreSecretAsync(Constants.ChangeDetectionConfig.ApiKey, model.ChangeDetectionApiKey);
        await transaction.CommitAsync();

        return await GetSettingsAsync();
    }

    private static string ValidateOptionalHttpUrl(string? value, string errorKey)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ValidationException(errorKey);
        }

        return trimmed;
    }

    private async Task StoreSecretAsync(string key, string? submitted)
    {
        if (submitted == null)
        {
            await _configRepository.SetConfigValueAsync(key, string.Empty);
        }
        else if (!string.IsNullOrWhiteSpace(submitted))
        {
            await _configRepository.SetConfigValueAsync(key, submitted.Trim());
        }
    }

    private async Task SetUnlessOverriddenAsync<T>(string key, T value)
    {
        var variable = EnvironmentVariableFor(key);
        if (TryReadEnvironment<T>(variable, out _))
        {
            _logger.LogDebug("Skipping {Key}: value is forced by environment variable {Variable}", key, variable);
            return;
        }

        await _configRepository.SetConfigValueAsync(key, value);
    }

    private static string EnvironmentVariableFor(string key) => key.ToUpperInvariant();

    private static bool IsOverriddenByEnvironment(string variable) =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable));

    private static bool TryReadEnvironment<T>(string variable, out T value)
    {
        var envValue = Environment.GetEnvironmentVariable(variable);
        if (!string.IsNullOrWhiteSpace(envValue) && TypeConverter.TryConvertFromString<T>(envValue.Trim(), out var result))
        {
            value = result;
            return true;
        }

        value = default!;
        return false;
    }

    private static string ToCamelCase(string name) => char.ToLowerInvariant(name[0]) + name[1..];

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
        var key = await GetBggApiKeyAsync();
        return !string.IsNullOrEmpty(key);
    }

    public async Task<(string? BaseUrl, string? ApiKey)> GetChangeDetectionSettingsAsync()
    {
        var configs = await _configRepository.GetConfigsByPrefixAsync(Constants.ChangeDetectionConfig.Prefix);
        return (configs.GetValueOrDefault(Constants.ChangeDetectionConfig.BaseUrl),
            configs.GetValueOrDefault(Constants.ChangeDetectionConfig.ApiKey));
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

    private static ChangeDetectionConfigStatusDto GetChangeDetectionConfigStatus(Dictionary<string, string> configs)
    {
        var baseUrl = configs.GetValueOrDefault(Constants.ChangeDetectionConfig.BaseUrl.ToLowerInvariant(), string.Empty);
        var apiKey = configs.GetValueOrDefault(Constants.ChangeDetectionConfig.ApiKey.ToLowerInvariant(), string.Empty);

        var isConfigured = !string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(apiKey);

        return new ChangeDetectionConfigStatusDto
        {
            IsConfigured = isConfigured,
            Source = isConfigured ? "db" : "none",
            IsReadOnly = false
        };
    }

    private static T ResolveValue<T>(Dictionary<string, string> configs, string key, T fallback = default!)
    {
        return ResolveValue(configs, key, fallback, out _);
    }

    private static T ResolveValue<T>(Dictionary<string, string> configs, string key, T fallback, out bool fromEnvironment)
    {
        fromEnvironment = TryReadEnvironment<T>(EnvironmentVariableFor(key), out var envResult);
        if (fromEnvironment)
        {
            return envResult;
        }

        var normalizedKey = key.ToLowerInvariant();
        if (configs.TryGetValue(normalizedKey, out var value) &&
            TypeConverter.TryConvertFromString<T>(value, out var dbResult))
        {
            return dbResult;
        }

        return fallback;
    }
}
