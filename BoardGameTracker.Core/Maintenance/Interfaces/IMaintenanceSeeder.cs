namespace BoardGameTracker.Core.Maintenance.Interfaces;

public interface IMaintenanceSeeder
{
    Task EnsureAdminPasswordAcceptedAsync(CancellationToken cancellationToken = default);
    Task ReseedDefaultsAsync(CancellationToken cancellationToken = default);
}
