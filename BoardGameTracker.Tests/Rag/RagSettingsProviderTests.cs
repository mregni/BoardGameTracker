using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Rag;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class RagSettingsProviderTests
{
    private readonly Mock<IConfigRepository> _configRepositoryMock = new();
    private readonly RagSettingsProvider _provider;

    public RagSettingsProviderTests()
    {
        _provider = new RagSettingsProvider(_configRepositoryMock.Object);
    }

    private void VerifyAllConfigValuesReadOnce()
    {
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AiConfig.Provider), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AiConfig.BaseUrl), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AiConfig.ChatModel), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AiConfig.ApiKey), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AiConfig.EmbeddingBaseUrl), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<int>(Constants.AiConfig.EmbeddingNumGpu), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<int>(Constants.AiConfig.TopK), Times.Once);
        _configRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_ResolvesConfiguredValuesAndFixesEmbeddingModel()
    {
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.Provider)).ReturnsAsync("openai");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.BaseUrl)).ReturnsAsync("https://api.anthropic.com/v1");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.ChatModel)).ReturnsAsync("claude-sonnet-4");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.ApiKey)).ReturnsAsync("sk-ant-123");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.EmbeddingBaseUrl)).ReturnsAsync("http://ollama:11434");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<int>(Constants.AiConfig.EmbeddingNumGpu)).ReturnsAsync(0);
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<int>(Constants.AiConfig.TopK)).ReturnsAsync(5);

        var settings = await _provider.GetAsync();

        settings.ChatProvider.Should().Be("openai");
        settings.ChatBaseUrl.Should().Be("https://api.anthropic.com/v1");
        settings.ChatModel.Should().Be("claude-sonnet-4");
        settings.ChatApiKey.Should().Be("sk-ant-123");
        settings.TopK.Should().Be(5);
        settings.EmbeddingBaseUrl.Should().Be("http://ollama:11434");
        settings.EmbeddingModel.Should().Be(Constants.AiConfig.EmbeddingModel);
        settings.EmbeddingDimensions.Should().Be(Constants.AiConfig.EmbeddingDimensions);
        settings.EmbeddingNumGpu.Should().Be(0);
        VerifyAllConfigValuesReadOnce();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_NormalizesBlankChatApiKeyToNull(string? apiKey)
    {
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.Provider)).ReturnsAsync("ollama");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.BaseUrl)).ReturnsAsync("http://ollama:11434");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.ChatModel)).ReturnsAsync("qwen3:4b");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.ApiKey)).ReturnsAsync(apiKey!);
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.EmbeddingBaseUrl)).ReturnsAsync("http://ollama:11434");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<int>(Constants.AiConfig.EmbeddingNumGpu)).ReturnsAsync(-1);
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<int>(Constants.AiConfig.TopK)).ReturnsAsync(5);

        var settings = await _provider.GetAsync();

        settings.ChatApiKey.Should().BeNull();
        VerifyAllConfigValuesReadOnce();
    }
}
