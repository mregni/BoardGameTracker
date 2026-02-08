using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Updates;

namespace BoardGameTracker.Core.Updates.Interfaces;

public interface IUpdateService
{
    Task<UpdateStatus> GetVersionInfoAsync();
    Task CheckForUpdatesAsync();
    Task<UpdateSettings> GetUpdateSettingsAsync();
    Task UpdateSettingsAsync(bool enabled, VersionTrack versionTrack);
    string GetCurrentVersion();
}
