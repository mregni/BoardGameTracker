namespace BoardGameTracker.Core.Rag.Interfaces;

public interface IManualChunkRepository
{
    Task DeleteByManualAsync(int manualId);
}
