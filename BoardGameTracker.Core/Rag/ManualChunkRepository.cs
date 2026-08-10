using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Rag.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Rag;

public class ManualChunkRepository : IManualChunkRepository
{
    private readonly MainDbContext _context;

    public ManualChunkRepository(MainDbContext context)
    {
        _context = context;
    }

    public async Task DeleteByManualAsync(int manualId)
    {
        await _context.ManualChunks
            .Where(c => c.ManualId == manualId)
            .ExecuteDeleteAsync();
    }
}
