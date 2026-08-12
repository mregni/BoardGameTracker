namespace BoardGameTracker.Core.Rag;

public record RagSettings(
    string Provider,
    string BaseUrl,
    string ChatModel,
    string EmbeddingModel,
    int EmbeddingDimensions,
    string? ApiKey,
    int TopK);
