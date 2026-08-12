namespace BoardGameTracker.Core.Rag.Interfaces;

public interface IManualIndexingQueue
{
    void Enqueue(int manualId);
    ValueTask<int> DequeueAsync(CancellationToken cancellationToken);
}
