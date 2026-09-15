using BoardGameTracker.Core.Rag.Specifications;

namespace BoardGameTracker.Core.Rag.Interfaces;

public interface IManualChunkRepository
{
    Task<List<ManualChunkMatch>> SearchAsync(NearestManualChunksSpec spec, CancellationToken cancellationToken = default);
    Task DeleteByManualAsync(int manualId);
}
