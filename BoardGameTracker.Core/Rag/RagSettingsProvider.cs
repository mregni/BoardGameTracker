using BoardGameTracker.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Rag.Interfaces;

namespace BoardGameTracker.Core.Rag;

public class RagSettingsProvider : IRagSettingsProvider
{
    private readonly IConfigRepository _configRepository;

    public RagSettingsProvider(IConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    public async Task<RagSettings> GetAsync()
    {
        var chatProvider = await _configRepository.GetConfigValueAsync<string>(Constants.AiConfig.Provider);
        var chatBaseUrl = await _configRepository.GetConfigValueAsync<string>(Constants.AiConfig.BaseUrl);
        var chatModel = await _configRepository.GetConfigValueAsync<string>(Constants.AiConfig.ChatModel);
        var chatApiKey = await _configRepository.GetConfigValueAsync<string>(Constants.AiConfig.ApiKey);
        var embeddingBaseUrl = await _configRepository.GetConfigValueAsync<string>(Constants.AiConfig.EmbeddingBaseUrl);
        var embeddingNumGpu = await _configRepository.GetConfigValueAsync<int>(Constants.AiConfig.EmbeddingNumGpu);
        var topK = await _configRepository.GetConfigValueAsync<int>(Constants.AiConfig.TopK);

        return new RagSettings(
            chatProvider,
            chatBaseUrl,
            chatModel,
            string.IsNullOrWhiteSpace(chatApiKey) ? null : chatApiKey,
            embeddingBaseUrl,
            Constants.AiConfig.EmbeddingModel,
            Constants.AiConfig.EmbeddingDimensions,
            embeddingNumGpu,
            topK);
    }
}
