using Ardalis.Specification.EntityFrameworkCore;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Rag.Interfaces;
using BoardGameTracker.Core.Rag.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BoardGameTracker.Core.Rag;

public class ManualChunkRepository : IManualChunkRepository
{
    private const string EnableIterativeScanSql = "SET LOCAL hnsw.iterative_scan = relaxed_order";
    private const string VectorVersionSql = "SELECT extversion AS \"Value\" FROM pg_extension WHERE extname = 'vector'";
    private static readonly Version IterativeScanVersion = new(0, 8);
    private static volatile bool _iterativeScanUnsupported;
    private static volatile bool _versionChecked;

    private readonly MainDbContext _context;
    private readonly ILogger<ManualChunkRepository> _logger;

    public ManualChunkRepository(MainDbContext context, ILogger<ManualChunkRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ManualChunkMatch>> SearchAsync(NearestManualChunksSpec spec, CancellationToken cancellationToken = default)
    {
        if (_context.Database.IsNpgsql() && !_iterativeScanUnsupported && await SupportsIterativeScanAsync(cancellationToken))
        {
            var matches = await SearchWithIterativeScanAsync(spec, cancellationToken);
            if (matches != null)
            {
                return matches;
            }
        }

        return await Evaluate(spec).ToListAsync(cancellationToken);
    }

    public async Task DeleteByManualAsync(int manualId)
    {
        await _context.ManualChunks
            .Where(c => c.ManualId == manualId)
            .ExecuteDeleteAsync();
    }

    private async Task<List<ManualChunkMatch>?> SearchWithIterativeScanAsync(NearestManualChunksSpec spec, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _context.Database.ExecuteSqlRawAsync(EnableIterativeScanSql, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.UndefinedObject or PostgresErrorCodes.InvalidName)
        {
            MarkIterativeScanUnsupported();
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var matches = await Evaluate(spec).ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return matches;
    }

    private async Task<bool> SupportsIterativeScanAsync(CancellationToken cancellationToken)
    {
        if (_versionChecked)
        {
            return !_iterativeScanUnsupported;
        }

        var version = await _context.Database.SqlQueryRaw<string>(VectorVersionSql).FirstOrDefaultAsync(cancellationToken);
        _versionChecked = true;
        if (Version.TryParse(version, out var installed) && installed >= IterativeScanVersion)
        {
            return true;
        }

        MarkIterativeScanUnsupported();
        return false;
    }

    private void MarkIterativeScanUnsupported()
    {
        _iterativeScanUnsupported = true;
        _logger.LogWarning("pgvector iterative scans are unavailable (pgvector 0.8 or newer is needed); filtered rulebook searches may return fewer chunks than requested");
    }

    private IQueryable<ManualChunkMatch> Evaluate(NearestManualChunksSpec spec)
    {
        return _context.ManualChunks.WithSpecification(spec);
    }
}
