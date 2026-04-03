using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Updates;

namespace BoardGameTracker.Core.Updates.Interfaces;

public interface IUpdateService
{
    Task<UpdateStatus> GetVersionInfoAsync();
    Task CheckForUpdatesAsync();
    string GetCurrentVersion();
}
