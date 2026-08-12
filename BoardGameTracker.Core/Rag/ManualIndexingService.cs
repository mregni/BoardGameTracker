using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Rag.Interfaces;
using BoardGameTracker.Core.Rag.Specifications;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace BoardGameTracker.Core.Rag;

public class ManualIndexingService : IManualIndexingService
{
    private readonly IRepository<Manual> _manualRepository;
    private readonly IRepository<ManualChunk> _chunkWriteRepository;
    private readonly IManualChunkRepository _chunkRepository;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IRulebookChunker _chunker;
    private readonly IAiClientFactory _aiClientFactory;
    private readonly IManualIndexingQueue _queue;
    private readonly IDiskProvider _diskProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ManualIndexingService> _logger;

    public ManualIndexingService(
        IRepository<Manual> manualRepository,
        IRepository<ManualChunk> chunkWriteRepository,
        IManualChunkRepository chunkRepository,
        IPdfTextExtractor pdfTextExtractor,
        IRulebookChunker chunker,
        IAiClientFactory aiClientFactory,
        IManualIndexingQueue queue,
        IDiskProvider diskProvider,
        IUnitOfWork unitOfWork,
        ILogger<ManualIndexingService> logger)
    {
        _manualRepository = manualRepository;
        _chunkWriteRepository = chunkWriteRepository;
        _chunkRepository = chunkRepository;
        _pdfTextExtractor = pdfTextExtractor;
        _chunker = chunker;
        _aiClientFactory = aiClientFactory;
        _queue = queue;
        _diskProvider = diskProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task EnqueuePendingAsync(CancellationToken cancellationToken = default)
    {
        var manuals = await _manualRepository.ListAsync(new ManualsToIndexSpec(), cancellationToken);
        foreach (var manual in manuals)
        {
            _queue.Enqueue(manual.Id);
        }

        if (manuals.Count > 0)
        {
            _logger.LogInformation("Enqueued {Count} manual(s) for indexing", manuals.Count);
        }
    }

    public async Task IndexAsync(int manualId, CancellationToken cancellationToken = default)
    {
        var manual = await _manualRepository.GetByIdAsync(manualId);
        if (manual == null)
        {
            _logger.LogWarning("Manual {ManualId} not found for indexing", manualId);
            return;
        }

        try
        {
            manual.MarkIndexing();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _aiClientFactory.EnsureModelsAvailableAsync(cancellationToken);

            var path = GetPhysicalPath(manual.StoredFileName);
            List<PdfPageText> pages;
            await using (var stream = _diskProvider.OpenRead(path))
            {
                pages = _pdfTextExtractor.Extract(stream).ToList();
            }

            var chunks = _chunker.Chunk(pages);
            if (chunks.Count == 0)
            {
                manual.MarkFailed("No extractable text found (the PDF may be scanned or image-only).");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var embedder = await _aiClientFactory.CreateEmbeddingGeneratorAsync(cancellationToken);
            var embeddings = await embedder.GenerateAsync(
                chunks.Select(c => c.Content).ToList(),
                cancellationToken: cancellationToken);

            await _chunkRepository.DeleteByManualAsync(manualId);

            var entities = new List<ManualChunk>(chunks.Count);
            for (var i = 0; i < chunks.Count; i++)
            {
                var embeddingVector = embeddings[i].Vector;
                if (embeddingVector.Length != Constants.AiConfig.EmbeddingDimensions)
                {
                    manual.MarkFailed(
                        $"Embedding dimension mismatch: expected {Constants.AiConfig.EmbeddingDimensions}, got {embeddingVector.Length}. Check the configured embedding model.");
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return;
                }

                entities.Add(new ManualChunk(
                    manual.Id,
                    manual.GameId,
                    chunks[i].Index,
                    chunks[i].Content,
                    chunks[i].PageNumber,
                    new Vector(embeddingVector)));
            }

            await _chunkWriteRepository.CreateRangeAsync(entities);
            manual.MarkIndexed(entities.Count, DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Indexed manual {ManualId} into {Count} chunk(s)", manualId, entities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index manual {ManualId}", manualId);
            try
            {
                manual.MarkFailed(ex.Message);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to record indexing failure for manual {ManualId}", manualId);
            }
        }
    }

    private static string GetPhysicalPath(string storedFileName)
    {
        var root = Path.GetFullPath(PathHelper.FullManualsPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, storedFileName));
        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new EntityNotFoundException(nameof(Manual), storedFileName);
        }

        return fullPath;
    }
}
