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
        var provider = await _configRepository.GetConfigValueAsync<string>(Constants.AiConfig.Provider);
        var baseUrl = await _configRepository.GetConfigValueAsync<string>(Constants.AiConfig.BaseUrl);
        var chatModel = await _configRepository.GetConfigValueAsync<string>(Constants.AiConfig.ChatModel);
        var topK = await _configRepository.GetConfigValueAsync<int>(Constants.AiConfig.TopK);
        var apiKey = await _configRepository.GetConfigValueAsync<string>(Constants.AiConfig.ApiKey);

        return new RagSettings(
            provider,
            baseUrl,
            chatModel,
            Constants.AiConfig.EmbeddingModel,
            Constants.AiConfig.EmbeddingDimensions,
            string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
            topK);
    }
}
