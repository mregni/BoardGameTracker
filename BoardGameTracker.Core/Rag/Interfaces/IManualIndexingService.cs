namespace BoardGameTracker.Core.Rag.Interfaces;

public interface IManualIndexingService
{
    Task IndexAsync(int manualId, CancellationToken cancellationToken = default);
    Task EnqueuePendingAsync(CancellationToken cancellationToken = default);
}
