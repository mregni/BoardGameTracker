using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Rag.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OllamaSharp;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class AiClientFactoryTests
{
    private const string BaseUrl = "http://ollama:11434";

    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IRagSettingsProvider> _settingsProviderMock = new();
    private readonly RecordingHandler _handler = new();
    private readonly AiClientFactory _factory;

    public AiClientFactoryTests()
    {
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(AiClientFactory.HttpClientName))
            .Returns(() => new HttpClient(_handler));

        _factory = new AiClientFactory(
            _httpClientFactoryMock.Object,
            _settingsProviderMock.Object,
            Mock.Of<ILogger<AiClientFactory>>());
    }

    private void SetupSettings(string provider, string chatModel = "qwen3:4b", string embeddingModel = "bge-m3")
    {
        _settingsProviderMock
            .Setup(x => x.GetAsync())
            .ReturnsAsync(new RagSettings(provider, BaseUrl, chatModel, embeddingModel, 1024, "api-key", 5));
    }

    [Fact]
    public async Task CreateEmbeddingGeneratorAsync_ShouldReturnOllamaClient_WhenProviderIsOllama()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider);

        var generator = await _factory.CreateEmbeddingGeneratorAsync();

        var ollama = generator.Should().BeOfType<OllamaApiClient>().Subject;
        ollama.SelectedModel.Should().Be("bge-m3");
        ollama.Uri.Should().Be(new Uri(BaseUrl));
    }

    [Fact]
    public async Task CreateChatClientAsync_ShouldReturnOllamaClient_WhenProviderIsOllama()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider);

        var client = await _factory.CreateChatClientAsync();

        client.Should().BeOfType<OllamaApiClient>().Which.SelectedModel.Should().Be("qwen3:4b");
    }

    [Theory]
    [InlineData("ollama")]
    [InlineData("Ollama")]
    [InlineData("OLLAMA")]
    public async Task CreateChatClientAsync_ShouldMatchProviderCaseInsensitively(string provider)
    {
        SetupSettings(provider);

        var client = await _factory.CreateChatClientAsync();

        client.Should().BeOfType<OllamaApiClient>();
    }

    [Fact]
    public async Task CreateEmbeddingGeneratorAsync_ShouldNotUseOllama_WhenProviderIsOpenAi()
    {
        SetupSettings(Constants.AiConfig.OpenAiProvider);

        var generator = await _factory.CreateEmbeddingGeneratorAsync();

        generator.Should().NotBeOfType<OllamaApiClient>();
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateChatClientAsync_ShouldNotUseOllama_WhenProviderIsOpenAi()
    {
        SetupSettings(Constants.AiConfig.OpenAiProvider);

        var client = await _factory.CreateChatClientAsync();

        client.Should().NotBeOfType<OllamaApiClient>();
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task EnsureModelsAvailableAsync_ShouldDoNothing_WhenProviderIsNotOllama()
    {
        SetupSettings(Constants.AiConfig.OpenAiProvider);

        await _factory.EnsureModelsAvailableAsync();

        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureModelsAvailableAsync_ShouldNotPull_WhenBothModelsAreAlreadyLocal()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider);
        _handler.LocalModels = ["bge-m3", "qwen3:4b"];

        await _factory.EnsureModelsAvailableAsync();

        _handler.PullRequestCount.Should().Be(0);
    }

    [Fact]
    public async Task EnsureModelsAvailableAsync_ShouldNotPull_WhenLocalModelOnlyDiffersByTag()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider, chatModel: "qwen3", embeddingModel: "bge-m3");
        _handler.LocalModels = ["bge-m3:latest", "qwen3:4b"];

        await _factory.EnsureModelsAvailableAsync();

        _handler.PullRequestCount.Should().Be(0);
    }

    [Fact]
    public async Task EnsureModelsAvailableAsync_ShouldPullEveryMissingModel()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider);
        _handler.LocalModels = [];

        await _factory.EnsureModelsAvailableAsync();

        _handler.PullRequestCount.Should().Be(2);
    }

    [Fact]
    public async Task EnsureModelsAvailableAsync_ShouldOnlyPullTheMissingModel()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider);
        _handler.LocalModels = ["bge-m3:latest"];

        await _factory.EnsureModelsAvailableAsync();

        _handler.PullRequestCount.Should().Be(1);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];
        public IReadOnlyList<string> LocalModels { get; set; } = [];
        public int PullRequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            Requests.Add(path);

            if (path.Contains("pull", StringComparison.OrdinalIgnoreCase))
            {
                PullRequestCount++;
                return Task.FromResult(Json("""{"status":"success"}"""));
            }

            var models = string.Join(",", LocalModels.Select(m =>
                $$"""{"name":"{{m}}","model":"{{m}}","modified_at":"2026-01-01T00:00:00Z","size":1,"digest":"d"}"""));
            return Task.FromResult(Json($$"""{"models":[{{models}}]}"""));
        }

        private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }
}
