using Microsoft.Extensions.AI;

namespace BoardGameTracker.Core.Rag.Interfaces;

public interface IAiClientFactory
{
    Task<IEmbeddingGenerator<string, Embedding<float>>> CreateEmbeddingGeneratorAsync(CancellationToken cancellationToken = default);
    Task<IChatClient> CreateChatClientAsync(CancellationToken cancellationToken = default);
    Task EnsureModelsAvailableAsync(CancellationToken cancellationToken = default);
}
