using BoardGameTracker.Common.Models.Updates;

namespace BoardGameTracker.Core.Updates.Interfaces;

public interface IUpdateService
{
    Task<UpdateStatus> GetVersionInfoAsync();
    Task CheckForUpdatesAsync();
    Task<UpdateSettings> GetUpdateSettingsAsync();
    Task UpdateSettingsAsync(bool enabled);
    string GetCurrentVersion();
}
