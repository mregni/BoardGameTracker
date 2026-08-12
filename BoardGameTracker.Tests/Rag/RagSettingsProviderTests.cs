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

    [Fact]
    public async Task GetAsync_ResolvesConfiguredValuesAndFixesEmbeddingModel()
    {
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.Provider)).ReturnsAsync("ollama");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.BaseUrl)).ReturnsAsync("http://ollama:11434");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.ChatModel)).ReturnsAsync("qwen3:4b");
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<int>(Constants.AiConfig.TopK)).ReturnsAsync(5);
        _configRepositoryMock.Setup(x => x.GetConfigValueAsync<string>(Constants.AiConfig.ApiKey)).ReturnsAsync(string.Empty);

        var settings = await _provider.GetAsync();

        settings.Provider.Should().Be("ollama");
        settings.BaseUrl.Should().Be("http://ollama:11434");
        settings.ChatModel.Should().Be("qwen3:4b");
        settings.TopK.Should().Be(5);
        settings.ApiKey.Should().BeNull();
        settings.EmbeddingModel.Should().Be(Constants.AiConfig.EmbeddingModel);
        settings.EmbeddingDimensions.Should().Be(Constants.AiConfig.EmbeddingDimensions);
    }
}
