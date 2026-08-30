using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.ChangeDetection;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Settings.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.ChangeDetection;

public class ChangeDetectionClientTests
{
    private const string WatchId = "e0808154-28da-4b85-9a71-24a409e694f1";

    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly MemoryCache _cache;
    private readonly CountingHandler _handler;
    private readonly ChangeDetectionClient _client;

    public ChangeDetectionClientTests()
    {
        _handler = new CountingHandler("In Stock: True - Price: 22.5");
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(ChangeDetectionClient.HttpClientName))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));

        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .ReturnsAsync(("https://changes.example.com", "api-key"));

        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc));

        _cache = new MemoryCache(new MemoryCacheOptions());
        _client = new ChangeDetectionClient(
            _httpClientFactoryMock.Object,
            _settingsServiceMock.Object,
            _cache,
            _dateTimeProviderMock.Object,
            new Mock<ILogger<ChangeDetectionClient>>().Object);
    }

    [Fact]
    public async Task GetLatestAsync_ShouldParseAndStampFetchedAt()
    {
        var result = await _client.GetLatestAsync(WatchId);

        result.Available.Should().BeTrue();
        result.InStock.Should().BeTrue();
        result.Price.Should().Be(22.5m);
        result.FetchedAt.Should().Be(new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetLatestAsync_ShouldServeSecondCallFromCache()
    {
        await _client.GetLatestAsync(WatchId);
        await _client.GetLatestAsync(WatchId);

        _handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLatestAsync_ShouldBypassCache_WhenForceRefresh()
    {
        await _client.GetLatestAsync(WatchId);
        await _client.GetLatestAsync(WatchId, forceRefresh: true);

        _handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetLatestAsync_ShouldNotCacheUnavailableResults()
    {
        _handler.StatusCode = HttpStatusCode.NotFound;

        await _client.GetLatestAsync(WatchId);
        await _client.GetLatestAsync(WatchId);

        _handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReturnUnavailable_WhenNotConfigured()
    {
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .ReturnsAsync((null, null));

        var result = await _client.GetLatestAsync(WatchId);

        result.Available.Should().BeFalse();
        _handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetLatestBatchAsync_ShouldOnlyFetchUncachedWatches()
    {
        await _client.GetLatestAsync(WatchId);

        var other = "f1919265-39eb-5c96-a082-35b510f705a2";
        var results = await _client.GetLatestAsync(new[] { WatchId, other });

        results.Should().HaveCount(2);
        results[WatchId].Available.Should().BeTrue();
        _handler.CallCount.Should().Be(2);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly string _body;

        public CountingHandler(string body)
        {
            _body = body;
        }

        public int CallCount { get; private set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(_body)
            });
        }
    }
}
