using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace BoardGameTracker.Core.Rag.Specifications;

public sealed class NearestManualChunksSpec : Specification<ManualChunk, ManualChunkMatch>
{
    public NearestManualChunksSpec(int gameId, Vector query, int k, int? manualId = null)
    {
        Query
            .Where(c => c.GameId == gameId)
            .AsNoTracking();

        if (manualId.HasValue)
        {
            Query.Where(c => c.ManualId == manualId.Value);
        }

        Query
            .OrderBy(c => c.Embedding.CosineDistance(query))
            .Take(k);

        Query.Select(c => new ManualChunkMatch(c, c.Embedding.CosineDistance(query)));
    }
}
