namespace BoardGameTracker.Core.Maintenance.Interfaces;

public interface IMaintenanceRepository
{
    Task<List<string>> GetChangeDetectionWatchIdsAsync(CancellationToken cancellationToken = default);
    Task ClearUserDataAsync(CancellationToken cancellationToken = default);
    Task ClearSettingsAndAuthAsync(CancellationToken cancellationToken = default);
}
