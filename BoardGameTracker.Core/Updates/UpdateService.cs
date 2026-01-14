using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Updates;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.DockerHub;
using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.Extensions.Logging;
using Refit;

namespace BoardGameTracker.Core.Updates;

public class UpdateService : IUpdateService
{
    private readonly IUpdateRepository _repository;
    private readonly IDockerHubApi _dockerHubApi;
    private readonly ILogger<UpdateService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    private const string DOCKER_OWNER = "uping";
    private const string DOCKER_REPO = "boardgametracker";

    public UpdateService(
        IUpdateRepository repository,
        IDockerHubApi dockerHubApi,
        ILogger<UpdateService> logger, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _dockerHubApi = dockerHubApi;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateStatus> GetUpdateStatusAsync()
    {
        var currentVersion = GetCurrentVersion();
        var config = await _repository.GetUpdateConfigAsync();

        var updateStatus = new UpdateStatus
        {
            CurrentVersion = currentVersion,
            LatestVersion = config.GetValueOrDefault("update_available_version"),
            UpdateAvailable = config.GetValueOrDefault("update_available") == "true",
            ErrorMessage = config.GetValueOrDefault("update_check_error")
        };

        if (config.TryGetValue("update_check_last_run", out var lastRunStr) &&
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
            var currentArchitecture = GetCurrentArchitecture();

            _logger.LogInformation("Current version: {Version}, Architecture: {Architecture}",
                currentVersion, currentArchitecture);

            await _repository.SetConfigValueAsync("update_current_architecture", currentArchitecture);

            var response = await _dockerHubApi.GetTags(DOCKER_OWNER, DOCKER_REPO);

            if (!response.IsSuccessStatusCode || response.Content == null)
            {
                throw new Exception($"Docker Hub API returned status {response.StatusCode}");
            }

            var semverTags = response.Content.Results
                .Where(t => IsSemanticVersion(t.Name))
                .OrderByDescending(t => ParseVersion(t.Name))
                .ToList();

            if (!semverTags.Any())
            {
                _logger.LogWarning("No valid semantic version tags found on Docker Hub");
                await _repository.SetConfigValueAsync("update_check_error", "No valid versions found");
                await _repository.SetConfigValueAsync("update_check_last_run", DateTime.UtcNow.ToString("O"));
                return;
            }

            string? latestVersionForArch = null;
            foreach (var tag in semverTags)
            {
                try
                {
                    var manifestResponse = await _dockerHubApi.GetTagManifest(DOCKER_OWNER, DOCKER_REPO, tag.Name);

                    if (manifestResponse is {IsSuccessStatusCode: true, Content: not null})
                    {
                        var supportsArchitecture = manifestResponse.Content.Architecture == currentArchitecture ||
                                                   manifestResponse.Content.Manifests.Any(m =>
                                                       m.Platform.Architecture == currentArchitecture);

                        if (supportsArchitecture)
                        {
                            latestVersionForArch = tag.Name;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking manifest for tag {Tag}", tag.Name);
                }
            }

            await _unitOfWork.BeginTransactionAsync();
            if (latestVersionForArch == null)
            {
                _logger.LogWarning("No versions found for architecture {Architecture}", currentArchitecture);
                await _repository.SetConfigValueAsync("update_check_error",
                    $"No versions found for architecture {currentArchitecture}");
                await _repository.SetConfigValueAsync("update_check_last_run", DateTime.UtcNow.ToString("O"));
                return;
            }

            var updateAvailable = CompareVersions(currentVersion, latestVersionForArch);

            await _repository.SetConfigValueAsync("update_available_version", latestVersionForArch);
            await _repository.SetConfigValueAsync("update_available", updateAvailable.ToString().ToLowerInvariant());
            await _repository.SetConfigValueAsync("update_check_last_run", DateTime.UtcNow.ToString("O"));
            await _repository.SetConfigValueAsync("update_check_error", string.Empty);

            _logger.LogInformation(
                "Update check completed. Current: {Current}, Latest: {Latest}, Available: {Available}",
                currentVersion, latestVersionForArch, updateAvailable);
        }
        catch (ApiException apiEx)
        {
            _logger.LogError(apiEx, "Docker Hub API error during update check");
            await _repository.SetConfigValueAsync("update_check_error", $"API Error: {apiEx.Message}");
            await _repository.SetConfigValueAsync("update_check_last_run", DateTime.UtcNow.ToString("O"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            await _repository.SetConfigValueAsync("update_check_error", ex.Message);
            await _repository.SetConfigValueAsync("update_check_last_run", DateTime.UtcNow.ToString("O"));
        }
        
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<UpdateSettings> GetUpdateSettingsAsync()
    {
        var enabled = await _repository.GetConfigValueAsync("update_check_enabled");
        var intervalStr = await _repository.GetConfigValueAsync("update_check_interval_hours");

        var settings = new UpdateSettings
        {
            Enabled = enabled != "false",
            IntervalHours = int.TryParse(intervalStr, out var hours) ? hours : 24
        };

        return settings;
    }

    public async Task UpdateSettingsAsync(bool enabled, int intervalHours)
    {
        if (intervalHours < 1)
        {
            throw new ArgumentException("Interval must be at least 1 hour", nameof(intervalHours));
        }

        await _unitOfWork.BeginTransactionAsync();
        await _repository.SetConfigValueAsync("update_check_enabled", enabled.ToString().ToLowerInvariant());
        await _repository.SetConfigValueAsync("update_check_interval_hours", intervalHours.ToString());
        await _unitOfWork.SaveChangesAsync();
    }

    public string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToVersionString() ?? "0.0.0";
    }

    private string GetCurrentArchitecture()
    {
        var arch = RuntimeInformation.ProcessArchitecture;
        return arch switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm/v7",
            _ => arch.ToString().ToLowerInvariant()
        };
    }

    private bool IsSemanticVersion(string tag)
    {
        return Regex.IsMatch(tag, @"^\d+\.\d+\.\d+$");
    }

    private Version ParseVersion(string versionString)
    {
        return Version.TryParse(versionString, out var version) ? version : new Version(0, 0, 0);
    }

    private bool CompareVersions(string current, string latest)
    {
        var currentVer = ParseVersion(current);
        var latestVer = ParseVersion(latest);
        return latestVer > currentVer;
    }
}
