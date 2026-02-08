using System.Reflection;
using System.Text.RegularExpressions;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Updates;
using BoardGameTracker.Core.DockerHub;
using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.Extensions.Logging;
using Refit;
using static BoardGameTracker.Common.Constants;

namespace BoardGameTracker.Core.Updates;

public class UpdateService : IUpdateService
{
    private readonly IUpdateRepository _repository;
    private readonly IDockerHubApi _dockerHubApi;
    private readonly ILogger<UpdateService> _logger;

    private const string DOCKER_OWNER = "uping";
    private const string DOCKER_REPO = "boardgametracker";

    public UpdateService(
        IUpdateRepository repository,
        IDockerHubApi dockerHubApi,
        ILogger<UpdateService> logger)
    {
        _repository = repository;
        _dockerHubApi = dockerHubApi;
        _logger = logger;
    }

    public async Task<UpdateStatus> GetVersionInfoAsync()
    {
        var currentVersion = GetCurrentVersion();
        var config = await _repository.GetUpdateConfigAsync();

        var updateStatus = new UpdateStatus
        {
            CurrentVersion = currentVersion,
            LatestVersion = config.GetValueOrDefault(UpdateConfig.AvailableVersion),
            UpdateAvailable = config.GetValueOrDefault(UpdateConfig.Available) == "true",
            ErrorMessage = config.GetValueOrDefault(UpdateConfig.CheckError)
        };

        if (config.TryGetValue(UpdateConfig.CheckLastRun, out var lastRunStr) &&
            DateTime.TryParse(lastRunStr, out var lastRun))
        {
            updateStatus.LastChecked = lastRun;
        }

        return updateStatus;
    }

    public async Task CheckForUpdatesAsync()
    {
        try
        {
            _logger.LogInformation("Checking for updates from Docker Hub...");

            var currentVersion = GetCurrentVersion();
            var updateTrack = await _repository.GetConfigValueAsync<VersionTrack>(UpdateConfig.Track);
            var isBetaTrack = updateTrack == VersionTrack.Beta;

            _logger.LogInformation("Current version: {Version}, Update track: {Track}",
                currentVersion, updateTrack);

            var response = await _dockerHubApi.GetTags(DOCKER_OWNER, DOCKER_REPO);
            if (!response.IsSuccessStatusCode || response.Content == null)
            {
                throw new Exception($"Docker Hub API returned status {response.StatusCode}");
            }

            var semverTags = response.Content.Results
                .Where(t => IsSemanticVersion(t.Name))
                .ToList();

            if (!semverTags.Any())
            {
                _logger.LogWarning("No valid semantic version tags found on Docker Hub");
                await _repository.SetConfigValueAsync(UpdateConfig.CheckError, "No valid versions found");
                await _repository.SetConfigValueAsync(UpdateConfig.CheckLastRun, DateTime.UtcNow.ToString("O"));
                return;
            }

            var latestVersion = isBetaTrack
                ? semverTags
                    .Where(t => IsBetaVersion(t.Name))
                    .OrderByDescending(t => ParseVersion(t.Name))
                    .FirstOrDefault()?.Name
                : semverTags
                    .Where(t => !IsBetaVersion(t.Name))
                    .OrderByDescending(t => ParseVersion(t.Name))
                    .FirstOrDefault()?.Name;

            if (latestVersion == null)
            {
                _logger.LogWarning("No matching versions found for track: {Track}", updateTrack);
                await _repository.SetConfigValueAsync(UpdateConfig.CheckError,
                    $"No {updateTrack} versions found");
                await _repository.SetConfigValueAsync(UpdateConfig.CheckLastRun, DateTime.UtcNow.ToString("O"));
                return;
            }

            var updateAvailable = CompareVersions(currentVersion, latestVersion);

            await _repository.SetConfigValueAsync(UpdateConfig.AvailableVersion, latestVersion);
            await _repository.SetConfigValueAsync(UpdateConfig.Available, updateAvailable);
            await _repository.SetConfigValueAsync(UpdateConfig.CheckLastRun, DateTime.UtcNow.ToString("O"));
            await _repository.SetConfigValueAsync(UpdateConfig.CheckError, string.Empty);

            _logger.LogInformation(
                "Update check completed. Current: {Current}, Latest: {Latest}, Available: {Available}",
                currentVersion, latestVersion, updateAvailable);
        }
        catch (ApiException apiEx)
        {
            _logger.LogError(apiEx, "Docker Hub API error during update check");
            await _repository.SetConfigValueAsync(UpdateConfig.CheckError, $"API Error: {apiEx.Message}");
            await _repository.SetConfigValueAsync(UpdateConfig.CheckLastRun, DateTime.UtcNow.ToString("O"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            await _repository.SetConfigValueAsync(UpdateConfig.CheckError, ex.Message);
            await _repository.SetConfigValueAsync(UpdateConfig.CheckLastRun, DateTime.UtcNow.ToString("O"));
        }
    }

    public async Task<UpdateSettings> GetUpdateSettingsAsync()
    {
        var enabled = await _repository.GetConfigValueAsync(UpdateConfig.CheckEnabled, true);
        var versionTrack = await _repository.GetConfigValueAsync<VersionTrack>(UpdateConfig.Track);
        var settings = new UpdateSettings
        {
            Enabled = enabled,
            VersionTrack = versionTrack
        };

        return settings;
    }

    public async Task UpdateSettingsAsync(bool enabled, VersionTrack versionTrack)
    {
        await _repository.SetConfigValueAsync(UpdateConfig.CheckEnabled, enabled);
        await _repository.SetConfigValueAsync(UpdateConfig.Track, versionTrack);
    }

    public string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToVersionString() ?? "0.0.0";
    }

    private bool IsSemanticVersion(string tag)
    {
        return Regex.IsMatch(tag, @"^\d+\.\d+\.\d+(-[a-zA-Z0-9]+)?$");
    }

    private Version ParseVersion(string versionString)
    {
        var versionOnly = versionString.Split('-')[0];
        return Version.TryParse(versionOnly, out var version) ? version : new Version(0, 0, 0);
    }

    private bool IsBetaVersion(string versionString)
    {
        return versionString.Contains("-beta", StringComparison.OrdinalIgnoreCase);
    }

    private bool CompareVersions(string current, string latest)
    {
        var currentVer = ParseVersion(current);
        var latestVer = ParseVersion(latest);
        return latestVer > currentVer;
    }
}
