using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;

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

    public Manual(string title, string storedFileName, string contentType, long fileSizeBytes, int gameId, DateTime uploadDate)
    {
        Title = title;
        StoredFileName = storedFileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        GameId = Guard.Against.NegativeOrZero(gameId);
        UploadDate = uploadDate;
    }
}
