namespace BoardGameTracker.Core.Maintenance.Interfaces;

public interface IMaintenanceSeeder
{
    Task ReseedDefaultsAsync(CancellationToken cancellationToken = default);
}
