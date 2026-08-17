namespace BoardGameTracker.Core.Rag;

public record RagSettings(
    string ChatProvider,
    string ChatBaseUrl,
    string ChatModel,
    string? ChatApiKey,
    string EmbeddingBaseUrl,
    string EmbeddingModel,
    int EmbeddingDimensions,
    int EmbeddingNumGpu,
    int TopK);
