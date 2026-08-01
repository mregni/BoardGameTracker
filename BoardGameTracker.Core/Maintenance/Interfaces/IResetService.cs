namespace BoardGameTracker.Core.Maintenance.Interfaces;

public interface IResetService
{
    Task ResetDataAsync(CancellationToken cancellationToken = default);
    Task FactoryResetAsync(CancellationToken cancellationToken = default);
}
