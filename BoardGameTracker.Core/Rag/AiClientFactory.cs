using System.ClientModel;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Rag.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models;
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
        if (settings.EmbeddingNumGpu >= 0)
        {
            return new OllamaEmbeddingGenerator(
                CreateOllamaApiClient(settings.EmbeddingBaseUrl),
                settings.EmbeddingModel,
                settings.EmbeddingNumGpu);
        }

        return CreateOllama(settings.EmbeddingBaseUrl, settings.EmbeddingModel);
    }

    public async Task<IChatClient> CreateChatClientAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsProvider.GetAsync();
        if (IsOllama(settings.ChatProvider))
        {
            return CreateOllama(settings.ChatBaseUrl, settings.ChatModel);
        }

        return CreateOpenAiClient(settings.ChatBaseUrl, settings.ChatApiKey)
            .GetChatClient(settings.ChatModel)
            .AsIChatClient();
    }

    public async Task EnsureModelsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsProvider.GetAsync();

        var embeddingClient = CreateOllamaApiClient(settings.EmbeddingBaseUrl);
        await EnsureModelPulledAsync(embeddingClient, settings.EmbeddingModel, cancellationToken);

        if (IsOllama(settings.ChatProvider))
        {
            var chatClient = CreateOllamaApiClient(settings.ChatBaseUrl);
            await EnsureModelPulledAsync(chatClient, settings.ChatModel, cancellationToken);
        }
    }

    private OllamaApiClient CreateOllamaApiClient(string baseUrl)
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.BaseAddress = new Uri(baseUrl);
        return new OllamaApiClient(httpClient);
    }

    private OllamaApiClient CreateOllama(string baseUrl, string model)
    {
        var client = CreateOllamaApiClient(baseUrl);
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

    private static OpenAIClient CreateOpenAiClient(string baseUrl, string? apiKey)
    {
        var options = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
        return new OpenAIClient(new ApiKeyCredential(apiKey ?? string.Empty), options);
    }

    private static bool IsOllama(string provider) =>
        string.Equals(provider, Constants.AiConfig.OllamaProvider, StringComparison.OrdinalIgnoreCase);

    private sealed class OllamaEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly OllamaApiClient _client;
        private readonly string _model;
        private readonly int _numGpu;

        public OllamaEmbeddingGenerator(OllamaApiClient client, string model, int numGpu)
        {
            _client = client;
            _model = model;
            _numGpu = numGpu;
        }

        public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var request = new EmbedRequest
            {
                Model = _model,
                Input = values.ToList(),
                Options = new RequestOptions { NumGpu = _numGpu }
            };

            var response = await _client.EmbedAsync(request, cancellationToken);
            return new GeneratedEmbeddings<Embedding<float>>(
                response.Embeddings.Select(embedding => new Embedding<float>(embedding)));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() => _client.Dispose();
    }
}
