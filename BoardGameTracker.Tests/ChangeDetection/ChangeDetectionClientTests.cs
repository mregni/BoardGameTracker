using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Models.ChangeDetection;
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
    private const string CreatedWatchId = "f1919265-39eb-5c96-a082-35b510f705a2";
    private const long LastChecked = 1756382400;

    private const string WatchJson =
        """{"url":"https://shop.example.com/brass","title":"Brass: Birmingham","last_checked":1756382400}""";

    private const string WatchWithRestockJson =
        """{"url":"https://shop.example.com/brass","title":"Brass: Birmingham","last_checked":1756382400,"restock":{"price":"34.99","currency":"EUR","in_stock":true}}""";

    private const string HistoryText = "In Stock: True - Price: 22.5";

    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly RoutingHandler _handler;
    private readonly ChangeDetectionClient _client;

    public ChangeDetectionClientTests()
    {
        _handler = new RoutingHandler();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient(ChangeDetectionClient.HttpClientName))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));

        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .ReturnsAsync(("https://changes.example.com", "api-key"));

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.Setup(x => x.UtcNow).Returns(new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc));

        _client = new ChangeDetectionClient(
            httpClientFactoryMock.Object,
            _settingsServiceMock.Object,
            new MemoryCache(new MemoryCacheOptions()),
            dateTimeProviderMock.Object,
            new Mock<ILogger<ChangeDetectionClient>>().Object);
    }

    [Fact]
    public async Task GetLatestAsync_ShouldCombineWatchMetadataWithLatestSnapshot()
    {
        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.InStock.Should().BeTrue();
        result.Price.Should().Be(22.5m);
        result.Currency.Should().BeNull();
        result.SourceUrl.Should().Be("https://shop.example.com/brass");
        result.Title.Should().Be("Brass: Birmingham");
        result.CheckedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(LastChecked).UtcDateTime);
        result.RecheckQueued.Should().BeFalse();
        result.FetchedAt.Should().Be(new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc));
        _handler.Requests.Should().Equal(
            $"GET /api/v1/watch/{WatchId}",
            $"GET /api/v1/watch/{WatchId}/history/latest");
    }

    [Fact]
    public async Task GetLatestAsync_ShouldUseRestockBlock_WhenTheWatchProvidesOne()
    {
        _handler.WatchBody = WatchWithRestockJson;

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.Price.Should().Be(34.99m);
        result.Currency.Should().Be("EUR");
        result.InStock.Should().BeTrue();
        _handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldServeSecondCallFromCache()
    {
        await _client.GetLatestAsync(WatchId);
        await _client.GetLatestAsync(WatchId);

        _handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLatestAsync_ShouldQueueARecheckOnlyOncePerThrottleWindow_WhenForceRefreshing()
    {
        var first = await _client.GetLatestAsync(WatchId, forceRefresh: true);
        var second = await _client.GetLatestAsync(WatchId, forceRefresh: true);

        first.RecheckQueued.Should().BeTrue();
        second.RecheckQueued.Should().BeFalse();
        _handler.Requests.Count(request => request.Contains("recheck=1")).Should().Be(1);
        _handler.Requests.Should().Contain($"GET /api/v1/watch/{WatchId}?recheck=1");
    }

    [Fact]
    public async Task GetLatestAsync_ShouldCacheFailuresBriefly_ToAvoidHammeringADownInstance()
    {
        _handler.WatchStatus = HttpStatusCode.NotFound;

        await _client.GetLatestAsync(WatchId);
        var second = await _client.GetLatestAsync(WatchId);

        second.Status.Should().Be(ChangeDetectionStatus.WatchNotFound);
        _handler.Requests.Should().ContainSingle();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, ChangeDetectionStatus.WatchNotFound)]
    [InlineData(HttpStatusCode.Unauthorized, ChangeDetectionStatus.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ChangeDetectionStatus.Unauthorized)]
    [InlineData(HttpStatusCode.BadGateway, ChangeDetectionStatus.Unreachable)]
    [InlineData(HttpStatusCode.MovedPermanently, ChangeDetectionStatus.Misconfigured)]
    [InlineData(HttpStatusCode.Found, ChangeDetectionStatus.Misconfigured)]
    public async Task GetLatestAsync_ShouldMapHttpStatusToReason(HttpStatusCode statusCode, ChangeDetectionStatus expected)
    {
        _handler.WatchStatus = statusCode;

        var result = await _client.GetLatestAsync(WatchId);

        result.Available.Should().BeFalse();
        result.Status.Should().Be(expected);
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReturnNotConfigured_WhenSettingsMissing()
    {
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .ReturnsAsync((null, null));

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.NotConfigured);
        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReturnMisconfigured_WhenBaseUrlInvalid()
    {
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .ReturnsAsync(("not-a-valid-url", "api-key"));

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Misconfigured);
        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldPropagateCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await _client.GetLatestAsync(WatchId, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetLatestBatchAsync_ShouldOnlyFetchUncachedWatches()
    {
        await _client.GetLatestAsync(WatchId);

        var results = await _client.GetLatestAsync(new[] { WatchId, CreatedWatchId });

        results.Should().HaveCount(2);
        results[WatchId].Available.Should().BeTrue();
        _handler.Requests.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReportPending_WhenTheWatchHasNoSnapshotYet()
    {
        _handler.HistoryStatus = HttpStatusCode.NotFound;

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Pending);
        result.Available.Should().BeFalse();
        result.SourceUrl.Should().Be("https://shop.example.com/brass");
    }

    [Fact]
    public async Task GetWatchInfoAsync_ShouldReturnTheWatchMetadata()
    {
        var (status, info) = await _client.GetWatchInfoAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.Ok);
        info.Should().NotBeNull();
        info!.Url.Should().Be("https://shop.example.com/brass");
        info.Title.Should().Be("Brass: Birmingham");
        info.LastCheckedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(LastChecked).UtcDateTime);
    }

    [Fact]
    public async Task GetWatchInfoAsync_ShouldReportWatchNotFound()
    {
        _handler.WatchStatus = HttpStatusCode.NotFound;

        var (status, info) = await _client.GetWatchInfoAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.WatchNotFound);
        info.Should().BeNull();
    }

    [Fact]
    public async Task CreateWatchAsync_ShouldPostARestockWatchAndReturnItsId()
    {
        var (status, watchId) = await _client.CreateWatchAsync("https://shop.example.com/brass", "Brass");

        status.Should().Be(ChangeDetectionStatus.Ok);
        watchId.Should().Be(CreatedWatchId);
        _handler.Requests.Should().ContainSingle().Which.Should().Be("POST /api/v1/watch");
        _handler.LastPostBody.Should().Contain("\"processor\":\"restock_diff\"");
        _handler.LastPostBody.Should().Contain("https://shop.example.com/brass");
    }

    [Fact]
    public async Task CreateWatchAsync_ShouldReportUnauthorized_WhenTheKeyIsRejected()
    {
        _handler.CreateStatus = HttpStatusCode.Forbidden;

        var (status, watchId) = await _client.CreateWatchAsync("https://shop.example.com/brass", "Brass");

        status.Should().Be(ChangeDetectionStatus.Unauthorized);
        watchId.Should().BeNull();
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnTheInstanceVersion()
    {
        var (ok, version) = await _client.TestConnectionAsync();

        ok.Should().BeTrue();
        version.Should().Be("0.50.1");
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldFail_WhenTheInstanceErrors()
    {
        _handler.SystemInfoStatus = HttpStatusCode.InternalServerError;

        var (ok, version) = await _client.TestConnectionAsync();

        ok.Should().BeFalse();
        version.Should().BeNull();
    }

    private sealed class RoutingHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = new();
        public string WatchBody { get; set; } = WatchJson;
        public HttpStatusCode WatchStatus { get; set; } = HttpStatusCode.OK;
        public string HistoryBody { get; set; } = HistoryText;
        public HttpStatusCode HistoryStatus { get; set; } = HttpStatusCode.OK;
        public HttpStatusCode CreateStatus { get; set; } = HttpStatusCode.Created;
        public HttpStatusCode SystemInfoStatus { get; set; } = HttpStatusCode.OK;
        public string? LastPostBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            Requests.Add($"{request.Method} {path}");

            if (request.Method == HttpMethod.Post && path == "/api/v1/watch")
            {
                LastPostBody = request.Content == null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);
                return Response(CreateStatus, $$$"""{"uuid":"{{{CreatedWatchId}}}"}""");
            }

            if (path.Contains("recheck=1", StringComparison.Ordinal))
            {
                return Response(HttpStatusCode.OK, "OK");
            }

            if (path == "/api/v1/systeminfo")
            {
                return Response(SystemInfoStatus, """{"version":"0.50.1"}""");
            }

            if (path.EndsWith("/history/latest", StringComparison.Ordinal))
            {
                return Response(HistoryStatus, HistoryBody);
            }

            return Response(WatchStatus, WatchBody);
        }

        private static HttpResponseMessage Response(HttpStatusCode statusCode, string body) =>
            new(statusCode) { Content = new StringContent(body) };
    }
}
