using System.ClientModel;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Rag.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OpenAI;

namespace BoardGameTracker.Core.Rag;

public class AiClientFactory : IAiClientFactory
{
    public const string HttpClientName = "ai";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRagSettingsProvider _settingsProvider;
    private readonly ILogger<AiClientFactory> _logger;

    public AiClientFactory(
        IHttpClientFactory httpClientFactory,
        IRagSettingsProvider settingsProvider,
        ILogger<AiClientFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settingsProvider = settingsProvider;
        _logger = logger;
    }

    public async Task<IEmbeddingGenerator<string, Embedding<float>>> CreateEmbeddingGeneratorAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await _settingsProvider.GetAsync();
        if (IsOllama(settings))
        {
            return CreateOllama(settings, settings.EmbeddingModel);
        }

        return CreateOpenAiClient(settings)
            .GetEmbeddingClient(settings.EmbeddingModel)
            .AsIEmbeddingGenerator();
    }

    public async Task<IChatClient> CreateChatClientAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsProvider.GetAsync();
        if (IsOllama(settings))
        {
            return CreateOllama(settings, settings.ChatModel);
        }

        return CreateOpenAiClient(settings)
            .GetChatClient(settings.ChatModel)
            .AsIChatClient();
    }

    public async Task EnsureModelsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsProvider.GetAsync();
        if (!IsOllama(settings))
        {
            return;
        }

        var client = CreateOllamaApiClient(settings);
        await EnsureModelPulledAsync(client, settings.EmbeddingModel, cancellationToken);
        await EnsureModelPulledAsync(client, settings.ChatModel, cancellationToken);
    }

    private OllamaApiClient CreateOllamaApiClient(RagSettings settings)
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.BaseAddress = new Uri(settings.BaseUrl);
        return new OllamaApiClient(httpClient);
    }

    private OllamaApiClient CreateOllama(RagSettings settings, string model)
    {
        var client = CreateOllamaApiClient(settings);
        client.SelectedModel = model;
        return client;
    }

    private async Task EnsureModelPulledAsync(OllamaApiClient client, string model, CancellationToken cancellationToken)
    {
        var localModels = await client.ListLocalModelsAsync(cancellationToken);
        if (localModels.Any(m => m.Name == model || m.Name.StartsWith($"{model}:", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _logger.LogInformation("Pulling AI model {Model}", model);
        await foreach (var _ in client.PullModelAsync(model, cancellationToken))
        {
        }
        _logger.LogInformation("Finished pulling AI model {Model}", model);
    }

    private static OpenAIClient CreateOpenAiClient(RagSettings settings)
    {
        var options = new OpenAIClientOptions { Endpoint = new Uri(settings.BaseUrl) };
        return new OpenAIClient(new ApiKeyCredential(settings.ApiKey ?? string.Empty), options);
    }

    private static bool IsOllama(RagSettings settings) =>
        string.Equals(settings.Provider, Constants.AiConfig.OllamaProvider, StringComparison.OrdinalIgnoreCase);
}
