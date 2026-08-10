using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class Manual : HasId
{
    private string _title = string.Empty;
    private string _storedFileName = string.Empty;
    private string _contentType = string.Empty;

    public string Title
    {
        get => _title;
        private set => _title = Guard.Against.NullOrWhiteSpace(value);
    }

    public string StoredFileName
    {
        get => _storedFileName;
        private set => _storedFileName = Guard.Against.NullOrWhiteSpace(value);
    }

    public string ContentType
    {
        get => _contentType;
        private set => _contentType = Guard.Against.NullOrWhiteSpace(value);
    }

    public long FileSizeBytes { get; private set; }
    public DateTime UploadDate { get; private set; }

    public Game Game { get; private set; } = null!;
    public int GameId { get; private set; }

    public ManualIndexStatus IndexStatus { get; private set; } = ManualIndexStatus.Pending;
    public int IndexedChunkCount { get; private set; }
    public string? IndexError { get; private set; }
    public DateTime? IndexedDate { get; private set; }

    public ICollection<ManualChunk> Chunks { get; private set; } = new List<ManualChunk>();

    public Manual(string title, string storedFileName, string contentType, long fileSizeBytes, int gameId, DateTime uploadDate)
    {
        Title = title;
        StoredFileName = storedFileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        GameId = Guard.Against.NegativeOrZero(gameId);
        UploadDate = uploadDate;
    }

    public void MarkIndexing()
    {
        IndexStatus = ManualIndexStatus.Indexing;
        IndexError = null;
    }

    public void MarkIndexed(int chunkCount, DateTime indexedDate)
    {
        IndexStatus = ManualIndexStatus.Indexed;
        IndexedChunkCount = Guard.Against.Negative(chunkCount);
        IndexError = null;
        IndexedDate = indexedDate;
    }

    public void MarkFailed(string error)
    {
        IndexStatus = ManualIndexStatus.Failed;
        IndexError = error;
    }

    public void ResetIndexState()
    {
        IndexStatus = ManualIndexStatus.Pending;
        IndexedChunkCount = 0;
        IndexError = null;
        IndexedDate = null;
    }
}
