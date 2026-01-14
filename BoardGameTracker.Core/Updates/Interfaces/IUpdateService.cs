using BoardGameTracker.Common.Models.Updates;

namespace BoardGameTracker.Core.Updates.Interfaces;

public interface IUpdateService
{
    Task<UpdateStatus> GetUpdateStatusAsync();
    Task CheckForUpdatesAsync();
    Task<UpdateSettings> GetUpdateSettingsAsync();
    Task UpdateSettingsAsync(bool enabled, int intervalHours);
    string GetCurrentVersion();
}
