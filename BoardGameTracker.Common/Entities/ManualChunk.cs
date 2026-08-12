using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using Pgvector;

namespace BoardGameTracker.Common.Entities;

public class ManualChunk : HasId
{
    public int ManualId { get; private set; }
    public Manual Manual { get; private set; } = null!;
    public int GameId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int? PageNumber { get; private set; }
    public Vector Embedding { get; private set; } = null!;

    private ManualChunk()
    {
    }

    public ManualChunk(int manualId, int gameId, int chunkIndex, string content, int? pageNumber, Vector embedding)
    {
        ManualId = Guard.Against.NegativeOrZero(manualId);
        GameId = Guard.Against.NegativeOrZero(gameId);
        ChunkIndex = Guard.Against.Negative(chunkIndex);
        Content = Guard.Against.NullOrWhiteSpace(content);
        PageNumber = pageNumber;
        Embedding = Guard.Against.Null(embedding);
    }
}
