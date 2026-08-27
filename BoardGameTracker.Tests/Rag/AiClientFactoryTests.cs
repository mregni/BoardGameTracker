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

    private void SetupSettings(string chatProvider, string chatModel = "qwen3:4b", string embeddingModel = "bge-m3",
        int embeddingNumGpu = -1)
    {
        _settingsProviderMock
            .Setup(x => x.GetAsync())
            .ReturnsAsync(new RagSettings(chatProvider, BaseUrl, chatModel, "api-key", BaseUrl, embeddingModel, 1024,
                embeddingNumGpu, 5));
    }

    private void VerifyNoOtherCalls()
    {
        _httpClientFactoryMock.VerifyNoOtherCalls();
        _settingsProviderMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("ollama")]
    [InlineData("openai")]
    public async Task CreateEmbeddingGeneratorAsync_ShouldReturnAutoPlacedOllamaClient_WhenNumGpuIsUnset(string chatProvider)
    {
        SetupSettings(chatProvider, embeddingNumGpu: -1);

        var generator = await _factory.CreateEmbeddingGeneratorAsync();

        var ollama = generator.Should().BeOfType<OllamaApiClient>().Subject;
        ollama.SelectedModel.Should().Be("bge-m3");
        ollama.Uri.Should().Be(new Uri(BaseUrl));
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateEmbeddingGeneratorAsync_ShouldPinToCpu_WhenNumGpuIsZero()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider, embeddingNumGpu: 0);

        var generator = await _factory.CreateEmbeddingGeneratorAsync();
        await generator.GenerateAsync(["hello"]);

        generator.Should().NotBeOfType<OllamaApiClient>();
        _handler.LastEmbedBody.Should().Contain("\"num_gpu\":0");
        _handler.LastEmbedBody.Should().Contain("bge-m3");
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateChatClientAsync_ShouldReturnOllamaClient_WhenProviderIsOllama()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider);

        var client = await _factory.CreateChatClientAsync();

        client.Should().BeOfType<OllamaApiClient>().Which.SelectedModel.Should().Be("qwen3:4b");
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
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
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("anthropic")]
    [InlineData("some-unknown-provider")]
    public async Task CreateChatClientAsync_ShouldFallBackToOpenAiClient_WhenProviderIsNotOllama(string provider)
    {
        SetupSettings(provider);

        var client = await _factory.CreateChatClientAsync();

        client.Should().NotBeOfType<OllamaApiClient>();
        client.GetType().Name.Should().Contain("OpenAI");
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
        VerifyNoOtherCalls();
    }

    public static TheoryData<string, string, string[], string[]> EnsureModelCases => new()
    {
        { "openai", "gpt-4o", new string[0], new[] { "bge-m3" } },
        { "ollama", "qwen3:4b", new[] { "bge-m3", "qwen3:4b" }, new string[0] },
        { "ollama", "qwen3", new[] { "bge-m3:latest", "qwen3:4b" }, new string[0] },
        { "ollama", "qwen3:4b", new[] { "BGE-M3:latest", "qwen3:4b" }, new string[0] },
        { "ollama", "qwen3:4b", new string[0], new[] { "bge-m3", "qwen3:4b" } },
        { "ollama", "qwen3:4b", new[] { "bge-m3:latest" }, new[] { "qwen3:4b" } }
    };

    [Theory]
    [MemberData(nameof(EnsureModelCases))]
    public async Task EnsureModelsAvailableAsync_ShouldPullExactlyTheMissingModels(
        string provider, string chatModel, string[] localModels, string[] expectedPulledModels)
    {
        SetupSettings(provider, chatModel: chatModel);
        _handler.LocalModels = localModels;

        await _factory.EnsureModelsAvailableAsync();

        _handler.PullRequests.Should().HaveCount(expectedPulledModels.Length);
        foreach (var model in expectedPulledModels)
        {
            _handler.PullRequests.Should().ContainSingle(body => body.Contains(model));
        }
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(
            x => x.CreateClient(AiClientFactory.HttpClientName),
            Times.Exactly(provider == Constants.AiConfig.OllamaProvider ? 2 : 1));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnsureModelsAvailableAsync_ShouldThrow_WhenPullingAModelFails()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider);
        _handler.LocalModels = [];
        _handler.FailPulls = true;

        await FluentActions.Awaiting(() => _factory.EnsureModelsAvailableAsync())
            .Should().ThrowAsync<Exception>();

        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];
        public IReadOnlyList<string> LocalModels { get; set; } = [];
        public List<string> PullRequests { get; } = [];
        public bool FailPulls { get; set; }
        public string? LastEmbedBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            Requests.Add(path);

            if (path.Contains("pull", StringComparison.OrdinalIgnoreCase))
            {
                PullRequests.Add(request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken));
                if (FailPulls)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("""{"error":"pull failed"}""", Encoding.UTF8, "application/json")
                    };
                }
                return Json("""{"status":"success"}""");
            }

            if (path.Contains("embed", StringComparison.OrdinalIgnoreCase))
            {
                LastEmbedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                return Json("""{"embeddings":[[0.1,0.2,0.3]]}""");
            }

            var models = string.Join(",", LocalModels.Select(m =>
                $$"""{"name":"{{m}}","model":"{{m}}","modified_at":"2026-01-01T00:00:00Z","size":1,"digest":"d"}"""));
            return Json($$"""{"models":[{{models}}]}""");
        }

        private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }
}
