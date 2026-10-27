using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Updates;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.DockerHub;
using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.Extensions.Logging;
using Refit;
using static BoardGameTracker.Common.Constants;

namespace BoardGameTracker.Core.Updates;

public partial class UpdateService : IUpdateService
{
    private readonly IConfigRepository _configRepository;
    private readonly IDockerHubApi _dockerHubApi;
    private readonly ILogger<UpdateService> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;

    private const string DOCKER_OWNER = "uping";
    private const string DOCKER_REPO = "boardgametracker";
    private const int MaxTagPages = 10;

    public UpdateService(
        IConfigRepository configRepository,
        IDockerHubApi dockerHubApi,
        ILogger<UpdateService> logger,
        IDateTimeProvider dateTimeProvider)
    {
        _configRepository = configRepository;
        _dockerHubApi = dockerHubApi;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<UpdateStatus> GetVersionInfoAsync()
    {
        var currentVersion = GetCurrentVersion();
        var config = await _configRepository.GetConfigsByPrefixAsync(UpdateConfig.Prefix);

        var updateStatus = new UpdateStatus
        {
            CurrentVersion = currentVersion,
            LatestVersion = config.GetValueOrDefault(UpdateConfig.AvailableVersion),
            UpdateAvailable = config.GetValueOrDefault(UpdateConfig.Available) == "true",
            ErrorMessage = config.GetValueOrDefault(UpdateConfig.CheckError)
        };

        if (config.TryGetValue(UpdateConfig.CheckLastRun, out var lastRunStr) &&
            DateTime.TryParse(lastRunStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastRun))
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
            var updateTrack = await _configRepository.GetConfigValueAsync<VersionTrack>(UpdateConfig.Track);
            var isBetaTrack = updateTrack == VersionTrack.Beta;

            _logger.LogInformation("Current version: {Version}, Update track: {Track}",
                currentVersion, updateTrack);

            var semverTags = await GetSemverTagsAsync();

            if (semverTags.Count == 0)
            {
                _logger.LogWarning("No valid semantic version tags found on Docker Hub");
                await _configRepository.SetConfigValueAsync(UpdateConfig.CheckError, "No valid versions found");
                await _configRepository.SetConfigValueAsync(UpdateConfig.CheckLastRun, _dateTimeProvider.UtcNow.ToString("O"));
                return;
            }

            var latestVersion = semverTags
                .Where(t => isBetaTrack || !IsBetaVersion(t))
                .OrderByDescending(VersionKey)
                .FirstOrDefault();

            if (latestVersion == null)
            {
                _logger.LogWarning("No matching versions found for track: {Track}", updateTrack);
                await _configRepository.SetConfigValueAsync(UpdateConfig.CheckError,
                    $"No {updateTrack} versions found");
                await _configRepository.SetConfigValueAsync(UpdateConfig.CheckLastRun, _dateTimeProvider.UtcNow.ToString("O"));
                return;
            }

            var updateAvailable = CompareVersions(currentVersion, latestVersion);

            await _configRepository.SetConfigValueAsync(UpdateConfig.AvailableVersion, latestVersion);
            await _configRepository.SetConfigValueAsync(UpdateConfig.Available, updateAvailable);
            await _configRepository.SetConfigValueAsync(UpdateConfig.CheckLastRun, _dateTimeProvider.UtcNow.ToString("O"));
            await _configRepository.SetConfigValueAsync(UpdateConfig.CheckError, string.Empty);

            _logger.LogInformation(
                "Update check completed. Current: {Current}, Latest: {Latest}, Available: {Available}",
                currentVersion, latestVersion, updateAvailable);
        }
        catch (ApiException apiEx)
        {
            _logger.LogError(apiEx, "Docker Hub API error during update check");
            await _configRepository.SetConfigValueAsync(UpdateConfig.CheckError, $"API Error: {apiEx.Message}");
            await _configRepository.SetConfigValueAsync(UpdateConfig.CheckLastRun, _dateTimeProvider.UtcNow.ToString("O"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            await _configRepository.SetConfigValueAsync(UpdateConfig.CheckError, ex.Message);
            await _configRepository.SetConfigValueAsync(UpdateConfig.CheckLastRun, _dateTimeProvider.UtcNow.ToString("O"));
        }
    }

    private async Task<List<string>> GetSemverTagsAsync()
    {
        var tags = new List<string>();
        for (var page = 1; page <= MaxTagPages; page++)
        {
            var response = await _dockerHubApi.GetTags(DOCKER_OWNER, DOCKER_REPO, page);
            if (!response.IsSuccessStatusCode || response.Content == null)
            {
                throw new InvalidOperationException($"Docker Hub API returned status {response.StatusCode}");
            }

            tags.AddRange(response.Content.Results.Select(t => t.Name).Where(IsSemanticVersion));
            if (string.IsNullOrEmpty(response.Content.Next))
            {
                break;
            }
        }

        return tags;
    }

    public string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToVersionString() ?? "0.0.0";
    }

    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[a-zA-Z0-9]+)?$", RegexOptions.None, 100)]
    private static partial Regex SemanticVersionRegex();

    private static bool IsSemanticVersion(string tag)
    {
        return SemanticVersionRegex().IsMatch(tag);
    }

    private static Version ParseVersion(string versionString)
    {
        var versionOnly = versionString.Split('-')[0];
        return Version.TryParse(versionOnly, out var version) ? version : new Version(0, 0, 0);
    }

    private static (Version Version, bool Stable) VersionKey(string versionString)
    {
        return (ParseVersion(versionString), !versionString.Contains('-'));
    }

    private static bool IsBetaVersion(string versionString)
    {
        return versionString.Contains("-beta", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CompareVersions(string current, string latest)
    {
        return VersionKey(latest).CompareTo(VersionKey(current)) > 0;
    }
}
