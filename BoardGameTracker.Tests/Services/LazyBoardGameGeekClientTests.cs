using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Settings.Interfaces;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class LazyBoardGameGeekClientTests
{
    private const string EmptyItemsXml =
        """<?xml version="1.0" encoding="utf-8"?><items termsofuse="https://boardgamegeek.com/xmlapi/termsofuse"></items>""";

    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private int _httpClientFactoryCalls;
    private readonly LazyBoardGameGeekClient _client;

    public LazyBoardGameGeekClientTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock.Setup(x => x.GetBggApiKeyAsync()).ReturnsAsync("test-api-key");

        _handlerMock = new Mock<HttpMessageHandler>();
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(EmptyItemsXml, Encoding.UTF8, "application/xml")
            });

        _client = new LazyBoardGameGeekClient(CreateHttpClient, _settingsServiceMock.Object);
    }

    private HttpClient CreateHttpClient()
    {
        _httpClientFactoryCalls++;
        return new HttpClient(_handlerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldNotResolveApiKeyOrHttpClient()
    {
        _settingsServiceMock.VerifyNoOtherCalls();
        _httpClientFactoryCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetThingAsync_ShouldResolveApiKeyAndHttpClientOnlyOnce_AcrossCalls()
    {
        var first = await _client.GetThingAsync(new ThingRequest([1]));
        var second = await _client.GetThingAsync(new ThingRequest([2]));

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        _settingsServiceMock.Verify(x => x.GetBggApiKeyAsync(), Times.Once);
        _settingsServiceMock.VerifyNoOtherCalls();
        _httpClientFactoryCalls.Should().Be(1);
    }
}
