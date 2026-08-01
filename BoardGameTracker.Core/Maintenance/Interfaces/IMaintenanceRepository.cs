namespace BoardGameTracker.Core.Maintenance.Interfaces;

public interface IMaintenanceRepository
{
    Task ClearUserDataAsync(CancellationToken cancellationToken = default);
    Task ClearSettingsAndAuthAsync(CancellationToken cancellationToken = default);
}
