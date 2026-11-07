using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Rag.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OllamaSharp;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class AiClientFactoryTests
{
    private const string BaseUrl = "http://ollama:11434";
    private const string ChatBaseUrl = "http://chat:11434";
    private const string EmbeddingBaseUrl = "http://embed:11434";

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
            NullLogger<AiClientFactory>.Instance);
    }

    private void SetupSettings(string chatProvider, string chatModel = "qwen3:4b", string embeddingModel = "bge-m3",
        int embeddingNumGpu = -1, string? apiKey = "api-key", string chatBaseUrl = BaseUrl, string embeddingBaseUrl = BaseUrl)
    {
        _settingsProviderMock
            .Setup(x => x.GetAsync())
            .ReturnsAsync(new RagSettings(chatProvider, chatBaseUrl, chatModel, apiKey, embeddingBaseUrl, embeddingModel, 1024,
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
        SetupSettings(chatProvider, embeddingNumGpu: -1, chatBaseUrl: ChatBaseUrl, embeddingBaseUrl: BaseUrl);

        var generator = await _factory.CreateEmbeddingGeneratorAsync();

        var ollama = generator.Should().BeOfType<OllamaApiClient>().Subject;
        ollama.SelectedModel.Should().Be("bge-m3");
        ollama.Uri.Should().Be(new Uri(BaseUrl));
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task CreateEmbeddingGeneratorAsync_ShouldPinTheGpuLayers_WhenNumGpuIsSet(int numGpu)
    {
        SetupSettings(Constants.AiConfig.OllamaProvider, embeddingNumGpu: numGpu, chatBaseUrl: ChatBaseUrl, embeddingBaseUrl: EmbeddingBaseUrl);

        var generator = await _factory.CreateEmbeddingGeneratorAsync();
        var embeddings = await generator.GenerateAsync(["hello", "world"], cancellationToken: TestContext.Current.CancellationToken);

        generator.Should().NotBeOfType<OllamaApiClient>();
        embeddings.Single().Vector.ToArray().Should().Equal(0.1f, 0.2f, 0.3f);
        _handler.LastEmbedBody.Should().Contain($"\"num_gpu\":{numGpu}");
        _handler.LastEmbedBody.Should().Contain("\"model\":\"bge-m3\"");
        _handler.LastEmbedBody.Should().Contain("\"input\":[\"hello\",\"world\"]");
        _handler.Hosts.Should().Equal("embed");
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PinnedEmbeddingGenerator_ShouldOnlyExposeItself_WithoutAServiceKey()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider, embeddingNumGpu: 0);

        var generator = await _factory.CreateEmbeddingGeneratorAsync();

        generator.GetService(typeof(IEmbeddingGenerator<string, Embedding<float>>)).Should().BeSameAs(generator);
        generator.GetService(typeof(IEmbeddingGenerator<string, Embedding<float>>), "key").Should().BeNull();
        generator.GetService(typeof(string)).Should().BeNull();
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateChatClientAsync_ShouldReturnOllamaClient_WhenProviderIsOllama()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider, chatBaseUrl: ChatBaseUrl, embeddingBaseUrl: EmbeddingBaseUrl);

        var client = await _factory.CreateChatClientAsync();

        var ollama = client.Should().BeOfType<OllamaApiClient>().Subject;
        ollama.SelectedModel.Should().Be("qwen3:4b");
        ollama.Uri.Should().Be(new Uri(ChatBaseUrl));
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
        var metadata = client.GetService<ChatClientMetadata>();
        metadata!.DefaultModelId.Should().Be("qwen3:4b");
        metadata.ProviderUri.Should().Be(new Uri(BaseUrl));
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateChatClientAsync_ShouldUseTheChatBaseUrl_ForAnOpenAiCompatibleServer()
    {
        SetupSettings("openai", chatBaseUrl: "http://llm.local:1234/v1", embeddingBaseUrl: EmbeddingBaseUrl);

        var client = await _factory.CreateChatClientAsync();

        client.GetService<ChatClientMetadata>()!.ProviderUri.Should().Be(new Uri("http://llm.local:1234/v1"));
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact(Skip = "BUG: a blank API key crashes ApiKeyCredential, so a keyless OpenAI-compatible server cannot be used")]
    public async Task CreateChatClientAsync_ShouldWorkWithoutAnApiKey_ForALocalOpenAiCompatibleServer()
    {
        SetupSettings("openai", apiKey: null);

        var client = await _factory.CreateChatClientAsync();

        client.GetService<ChatClientMetadata>()!.DefaultModelId.Should().Be("qwen3:4b");
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    public static TheoryData<string, string, string[], string[]> EnsureModelCases => new()
    {
        { "openai", "gpt-4o", new string[0], new[] { "bge-m3" } },
        { "ollama", "qwen3:4b", new[] { "bge-m3", "qwen3:4b" }, new string[0] },
        { "ollama", "qwen3", new[] { "bge-m3:latest", "qwen3:4b" }, new string[0] },
        { "ollama", "qwen3:4b", new[] { "BGE-M3:latest", "qwen3:4b" }, new string[0] },
        { "ollama", "qwen3:4b", new string[0], new[] { "bge-m3", "qwen3:4b" } },
        { "ollama", "qwen3:4b", new[] { "bge-m3:latest" }, new[] { "qwen3:4b" } },
        { "openai", "gpt-4o", new[] { "bge-m3-large" }, new[] { "bge-m3" } }
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
            _handler.PullRequests.Should().ContainSingle(body => body.Contains($"\"{model}\""));
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
            .Should().ThrowAsync<HttpRequestException>();

        _handler.PullRequests.Should().ContainSingle().Which.Should().Contain("\"bge-m3\"");

        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnsureModelsAvailableAsync_ShouldAskEachServer_ForItsOwnModel()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider, chatBaseUrl: ChatBaseUrl, embeddingBaseUrl: EmbeddingBaseUrl);
        _handler.LocalModels = ["bge-m3", "qwen3:4b"];

        await _factory.EnsureModelsAvailableAsync();

        _handler.Hosts.Should().Equal("embed", "chat");
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnsureModelsAvailableAsync_ShouldStopListing_WhenTheCallerCancels()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await FluentActions.Awaiting(() => _factory.EnsureModelsAvailableAsync(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        _handler.Requests.Should().BeEmpty();
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnsureModelsAvailableAsync_ShouldStopPulling_WhenTheCallerCancelsAfterTheListing()
    {
        SetupSettings(Constants.AiConfig.OllamaProvider);
        using var cts = new CancellationTokenSource();
        _handler.OnList = cts.Cancel;

        await FluentActions.Awaiting(() => _factory.EnsureModelsAvailableAsync(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        _handler.PullRequests.Should().BeEmpty();
        _settingsProviderMock.Verify(x => x.GetAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(AiClientFactory.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];
        public List<string> Hosts { get; } = [];
        public Action? OnList { get; set; }
        public IReadOnlyList<string> LocalModels { get; set; } = [];
        public List<string> PullRequests { get; } = [];
        public bool FailPulls { get; set; }
        public string? LastEmbedBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = request.RequestUri!.AbsolutePath;
            Requests.Add(path);
            Hosts.Add(request.RequestUri.Host);

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

            OnList?.Invoke();
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
