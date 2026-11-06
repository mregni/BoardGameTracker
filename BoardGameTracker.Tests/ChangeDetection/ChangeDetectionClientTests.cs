using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Core.ChangeDetection;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.ChangeDetection;

public class ChangeDetectionClientTests
{
    private const string WatchId = "e0808154-28da-4b85-9a71-24a409e694f1";
    private const string CreatedWatchId = "f1919265-39eb-5c96-a082-35b510f705a2";
    private const string Url = "https://shop.example.com/brass";
    private const long LastChecked = 1756382400;

    private const string WatchJson =
        """{"url":"https://shop.example.com/brass","title":"Brass: Birmingham","last_checked":1756382400}""";

    private const string WatchWithRestockJson =
        """{"url":"https://shop.example.com/brass","title":"Brass: Birmingham","last_checked":1756382400,"restock":{"price":"34.99","currency":"EUR","in_stock":true}}""";

    private const string HistoryText = "In Stock: True - Price: 22.5";

    private static readonly DateTime Now = new(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<ILogger<ChangeDetectionClient>> _loggerMock;
    private readonly RoutingHandler _handler;
    private readonly TestClock _clock = new();
    private readonly ChangeDetectionClient _client;

    public ChangeDetectionClientTests()
    {
        _handler = new RoutingHandler();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(ChangeDetectionClient.HttpClientName))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));

        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .ReturnsAsync(("https://changes.example.com", "api-key"));

        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);

        _loggerMock = new Mock<ILogger<ChangeDetectionClient>>();

        _client = new ChangeDetectionClient(
            _httpClientFactoryMock.Object,
            _settingsServiceMock.Object,
            new MemoryCache(new MemoryCacheOptions { Clock = _clock }),
            _dateTimeProviderMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _settingsServiceMock.VerifyNoOtherCalls();
        _httpClientFactoryMock.VerifyNoOtherCalls();
        _dateTimeProviderMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    private void VerifyUsage(int settingsReads, int clients, int fetches)
    {
        _settingsServiceMock.Verify(x => x.GetChangeDetectionSettingsAsync(), Times.Exactly(settingsReads));
        _httpClientFactoryMock.Verify(x => x.CreateClient(ChangeDetectionClient.HttpClientName), Times.Exactly(clients));
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Exactly(fetches));
    }

    [Fact]
    public async Task GetLatestAsync_ShouldCombineWatchMetadataWithLatestSnapshot()
    {
        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.InStock.Should().BeTrue();
        result.Price.Should().Be(22.5m);
        result.Currency.Should().BeNull();
        result.SourceUrl.Should().Be(Url);
        result.Title.Should().Be("Brass: Birmingham");
        result.CheckedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(LastChecked).UtcDateTime);
        result.RecheckQueued.Should().BeFalse();
        result.FetchedAt.Should().Be(Now);
        _handler.Requests.Should().Equal(
            $"GET /api/v1/watch/{WatchId}",
            $"GET /api/v1/watch/{WatchId}/history/latest");
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("https://changes.example.com", " api-key ", "/api/v1/watch/")]
    [InlineData(" https://changes.example.com/// ", "api-key", "/api/v1/watch/")]
    [InlineData("http://changes.example.com/cd", "api-key", "/cd/api/v1/watch/")]
    public async Task GetLatestAsync_ShouldNormalizeTheBaseUrlAndSendTheTrimmedKey(string baseUrl, string apiKey, string expectedPrefix)
    {
        _settingsServiceMock.Setup(x => x.GetChangeDetectionSettingsAsync()).ReturnsAsync((baseUrl, apiKey));

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        _handler.Requests[0].Should().Be($"GET {expectedPrefix}{WatchId}");
        _handler.LastApiKey.Should().Be("api-key");
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
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
        result.SourceUrl.Should().Be(Url);
        _handler.Requests.Should().ContainSingle();
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("\"in stock\"")]
    [InlineData("{}")]
    [InlineData("""{"in_stock":"yes"}""")]
    [InlineData("""{"in_stock":null,"price":null}""")]
    [InlineData("""{"price":true}""")]
    public async Task GetLatestAsync_ShouldFallBackToTheSnapshot_WhenTheRestockBlockHasNothingUsable(string restock)
    {
        _handler.WatchBody = $$"""{"url":"https://shop.example.com/brass","restock":{{restock}}}""";

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.InStock.Should().BeTrue();
        result.Price.Should().Be(22.5m);
        _handler.Requests.Should().HaveCount(2);
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldKeepTheRestockCurrency_WhenThePriceComesFromTheSnapshot()
    {
        _handler.WatchBody = """{"url":"https://shop.example.com/brass","restock":{"currency":"USD"}}""";

        var result = await _client.GetLatestAsync(WatchId);

        result.Currency.Should().Be("USD");
        result.Price.Should().Be(22.5m);
        _handler.Requests.Should().HaveCount(2);
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReadStockWithoutAPrice_FromTheRestockBlock()
    {
        _handler.WatchBody = """{"restock":{"in_stock":false}}""";

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.InStock.Should().BeFalse();
        result.Price.Should().BeNull();
        result.Currency.Should().BeNull();
        result.SourceUrl.Should().BeNull();
        result.Title.Should().BeNull();
        _handler.Requests.Should().ContainSingle();
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReadAPriceWithoutStock_FromTheRestockBlock()
    {
        _handler.WatchBody = """{"restock":{"price":0}}""";

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.Price.Should().Be(0m);
        result.InStock.Should().BeNull();
        _handler.Requests.Should().ContainSingle();
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("""{"title":"   "}""", null)]
    [InlineData("""{"title":null}""", null)]
    [InlineData("""{"title":1830}""", "1830")]
    public async Task GetLatestAsync_ShouldNormalizeTheWatchTitle(string watch, string? expected)
    {
        _handler.WatchBody = watch;

        var result = await _client.GetLatestAsync(WatchId);

        result.Title.Should().Be(expected);
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldServeSecondCallFromCache()
    {
        var first = await _client.GetLatestAsync(WatchId);
        var second = await _client.GetLatestAsync(WatchId);

        second.Should().BeSameAs(first);
        _handler.Requests.Should().HaveCount(2);
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldKeepAResultForThirtyMinutes()
    {
        await _client.GetLatestAsync(WatchId);

        _clock.Advance(TimeSpan.FromMinutes(30) - TimeSpan.FromSeconds(1));
        await _client.GetLatestAsync(WatchId);
        _handler.Requests.Should().HaveCount(2);

        _clock.Advance(TimeSpan.FromSeconds(2));
        await _client.GetLatestAsync(WatchId);
        _handler.Requests.Should().HaveCount(4);
        VerifyUsage(2, 2, 2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldKeepAFailureForOneMinute()
    {
        _handler.WatchStatus = HttpStatusCode.NotFound;
        await _client.GetLatestAsync(WatchId);

        _clock.Advance(TimeSpan.FromSeconds(59));
        await _client.GetLatestAsync(WatchId);
        _handler.Requests.Should().ContainSingle();

        _clock.Advance(TimeSpan.FromSeconds(2));
        await _client.GetLatestAsync(WatchId);
        _handler.Requests.Should().HaveCount(2);
        VerifyUsage(2, 2, 2);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io returned NotFound for watch {WatchId}", Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldQueueARecheckOnlyOncePerThrottleWindow_WhenForceRefreshing()
    {
        var first = await _client.GetLatestAsync(WatchId, forceRefresh: true);
        var second = await _client.GetLatestAsync(WatchId, forceRefresh: true);

        first.RecheckQueued.Should().BeTrue();
        second.RecheckQueued.Should().BeFalse();
        second.Should().NotBeSameAs(first);
        _handler.Requests.Should().Equal(
            $"GET /api/v1/watch/{WatchId}?recheck=1",
            $"GET /api/v1/watch/{WatchId}",
            $"GET /api/v1/watch/{WatchId}/history/latest",
            $"GET /api/v1/watch/{WatchId}",
            $"GET /api/v1/watch/{WatchId}/history/latest");
        VerifyUsage(2, 2, 2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldQueueANewRecheck_AfterTenMinutes()
    {
        await _client.GetLatestAsync(WatchId, forceRefresh: true);

        _clock.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromSeconds(1));
        var insideTheWindow = await _client.GetLatestAsync(WatchId, forceRefresh: true);
        _clock.Advance(TimeSpan.FromSeconds(2));
        var afterTheWindow = await _client.GetLatestAsync(WatchId, forceRefresh: true);

        insideTheWindow.RecheckQueued.Should().BeFalse();
        afterTheWindow.RecheckQueued.Should().BeTrue();
        _handler.Requests.Count(request => request.EndsWith("recheck=1", StringComparison.Ordinal)).Should().Be(2);
        VerifyUsage(3, 3, 3);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReleaseTheThrottle_WhenTheInstanceRefusesTheRecheck()
    {
        _handler.RecheckStatus = HttpStatusCode.InternalServerError;

        var refused = await _client.GetLatestAsync(WatchId, forceRefresh: true);
        _handler.RecheckStatus = HttpStatusCode.OK;
        var retried = await _client.GetLatestAsync(WatchId, forceRefresh: true);

        refused.Status.Should().Be(ChangeDetectionStatus.Ok);
        refused.RecheckQueued.Should().BeFalse();
        retried.RecheckQueued.Should().BeTrue();
        VerifyUsage(2, 2, 2);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io refused to queue a recheck for watch {WatchId}: InternalServerError");
        VerifyNoOtherCalls();
    }

    [Fact(Skip = "BUG: a pending watch drops RecheckQueued, so the client never polls for the first snapshot")]
    public async Task GetLatestAsync_ShouldReportTheQueuedRecheck_WhenTheWatchHasNoSnapshotYet()
    {
        _handler.HistoryStatus = HttpStatusCode.NotFound;

        var result = await _client.GetLatestAsync(WatchId, forceRefresh: true);

        result.Status.Should().Be(ChangeDetectionStatus.Pending);
        result.RecheckQueued.Should().BeTrue();
        VerifyUsage(1, 1, 1);
        _loggerMock.VerifyLog(LogLevel.Debug, $"changedetection.io returned NotFound for the latest snapshot of watch {WatchId}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldCacheFailuresBriefly_ToAvoidHammeringADownInstance()
    {
        _handler.WatchStatus = HttpStatusCode.NotFound;

        await _client.GetLatestAsync(WatchId);
        var second = await _client.GetLatestAsync(WatchId);

        second.Status.Should().Be(ChangeDetectionStatus.WatchNotFound);
        _handler.Requests.Should().ContainSingle();
        VerifyUsage(1, 1, 1);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io returned NotFound for watch {WatchId}");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, ChangeDetectionStatus.WatchNotFound)]
    [InlineData(HttpStatusCode.Unauthorized, ChangeDetectionStatus.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ChangeDetectionStatus.Unauthorized)]
    [InlineData(HttpStatusCode.MovedPermanently, ChangeDetectionStatus.Misconfigured)]
    [InlineData(HttpStatusCode.Found, ChangeDetectionStatus.Misconfigured)]
    [InlineData((HttpStatusCode)300, ChangeDetectionStatus.Misconfigured)]
    [InlineData((HttpStatusCode)399, ChangeDetectionStatus.Misconfigured)]
    public async Task GetLatestAsync_ShouldMapHttpStatusToReason(HttpStatusCode statusCode, ChangeDetectionStatus expected)
    {
        _handler.WatchStatus = statusCode;

        var result = await _client.GetLatestAsync(WatchId);

        result.Available.Should().BeFalse();
        result.Status.Should().Be(expected);
        VerifyUsage(1, 1, 1);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io returned {statusCode} for watch {WatchId}");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task GetLatestAsync_ShouldReportUnreachable_AndLogQuietly_ForOtherFailures(HttpStatusCode statusCode)
    {
        _handler.WatchStatus = statusCode;

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Unreachable);
        VerifyUsage(1, 1, 1);
        _loggerMock.VerifyLog(LogLevel.Debug, $"changedetection.io returned {statusCode} for watch {WatchId}");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("not json", ChangeDetectionStatus.ParseError)]
    [InlineData("[1,2]", ChangeDetectionStatus.ParseError)]
    public async Task GetLatestAsync_ShouldReportAParseError_WhenTheWatchIsNotAnObject(string body, ChangeDetectionStatus expected)
    {
        _handler.WatchBody = body;

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(expected);
        _handler.Requests.Should().ContainSingle();
        VerifyUsage(1, 1, 1);
        if (body == "not json")
        {
            _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io returned an unreadable watch object for {WatchId}");
        }

        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", "api-key")]
    [InlineData("https://changes.example.com", "  ")]
    public async Task GetLatestAsync_ShouldReturnNotConfigured_WhenSettingsMissing(string? baseUrl, string? apiKey)
    {
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .ReturnsAsync((baseUrl, apiKey));

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.NotConfigured);
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(1, 0, 0);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("not-a-valid-url")]
    [InlineData("ftp://changes.example.com")]
    public async Task GetLatestAsync_ShouldReturnMisconfigured_WhenBaseUrlInvalid(string baseUrl)
    {
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .ReturnsAsync((baseUrl, "api-key"));

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Misconfigured);
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(1, 0, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, "changedetection.io base URL is not a valid http(s) URL");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldPropagateCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await _client.GetLatestAsync(WatchId, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(1, 1, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldPropagateACancellationDuringTheRequest_WithoutCachingAFailure()
    {
        _handler.HangUntilCancelled = true;
        using var cts = new CancellationTokenSource();

        var pending = _client.GetLatestAsync(WatchId, cancellationToken: cts.Token);
        await _handler.RequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        var act = async () => await pending;
        await act.Should().ThrowAsync<OperationCanceledException>();

        _handler.HangUntilCancelled = false;
        var next = await _client.GetLatestAsync(WatchId);
        next.Status.Should().Be(ChangeDetectionStatus.Ok);
        VerifyUsage(2, 2, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReportUnreachable_WhenTheRequestTimesOutWithoutTheCallerCancelling()
    {
        var timeout = new TaskCanceledException("timeout");
        _handler.WatchException = timeout;

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Unreachable);
        VerifyUsage(1, 1, 1);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io fetch for watch {WatchId} failed", timeout);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReportUnreachable_AndCacheIt_WhenTheInstanceIsDown()
    {
        var failure = new HttpRequestException("Connection refused");
        _handler.WatchException = failure;

        var first = await _client.GetLatestAsync(WatchId);
        var second = await _client.GetLatestAsync(WatchId);

        first.Available.Should().BeFalse();
        first.Status.Should().Be(ChangeDetectionStatus.Unreachable);
        second.Status.Should().Be(ChangeDetectionStatus.Unreachable);
        _handler.Requests.Should().ContainSingle();
        VerifyUsage(1, 1, 1);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io fetch for watch {WatchId} failed", failure);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetLatestAsync_ShouldReportNotConfigured_WithoutCallingTheInstance_WhenTheWatchIdIsBlank(string watchId)
    {
        var result = await _client.GetLatestAsync(watchId);

        result.Status.Should().Be(ChangeDetectionStatus.NotConfigured);
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(0, 0, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReportMisconfigured_WhenTheApiKeyCannotBeSentAsAHeader()
    {
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .ReturnsAsync(("https://changes.example.com", "bad\nkey"));

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Misconfigured);
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, "changedetection.io API key is invalid");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestBatchAsync_ShouldReturnNothing_WithoutReadingSettings_WhenNoWatchIdIsGiven()
    {
        var results = await _client.GetLatestAsync(new[] { "", "  " });

        results.Should().BeEmpty();
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(0, 0, 0);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, null, ChangeDetectionStatus.NotConfigured)]
    [InlineData("not-a-valid-url", "api-key", ChangeDetectionStatus.Misconfigured)]
    public async Task GetLatestBatchAsync_ShouldReportTheSettingsProblem_ForEveryWatch(string? baseUrl, string? apiKey, ChangeDetectionStatus expected)
    {
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .ReturnsAsync((baseUrl, apiKey));

        var results = await _client.GetLatestAsync(new[] { WatchId, CreatedWatchId });

        results.Keys.Should().BeEquivalentTo(WatchId, CreatedWatchId);
        results.Values.Should().AllSatisfy(result => result.Status.Should().Be(expected));
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(1, 0, 0);
        if (expected == ChangeDetectionStatus.Misconfigured)
        {
            _loggerMock.VerifyLog(LogLevel.Warning, "changedetection.io base URL is not a valid http(s) URL");
        }

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateWatchAsync_ShouldPutTheNewUrl_AndDropTheCachedResult()
    {
        await _client.GetLatestAsync(WatchId);

        var status = await _client.UpdateWatchAsync(WatchId, "https://other.example.com/brass", "Brass");
        await _client.GetLatestAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.Ok);
        _handler.Requests.Should().Contain($"PUT /api/v1/watch/{WatchId}");
        _handler.LastPutBody.Should().Be("""{"url":"https://other.example.com/brass","title":"Brass"}""");
        _handler.Requests.Count(r => r == $"GET /api/v1/watch/{WatchId}").Should().Be(2);
        VerifyUsage(3, 3, 2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateWatchAsync_ShouldReportAMissingWatch_AndKeepTheCachedResult()
    {
        await _client.GetLatestAsync(WatchId);
        _handler.PutStatus = HttpStatusCode.NotFound;

        var status = await _client.UpdateWatchAsync(WatchId, "https://other.example.com/brass", "Brass");
        await _client.GetLatestAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.WatchNotFound);
        _handler.Requests.Count(r => r == $"GET /api/v1/watch/{WatchId}").Should().Be(1);
        VerifyUsage(2, 2, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateWatchAsync_ShouldNotCallTheInstance_WhenTheWatchIdIsNotAGuid()
    {
        var status = await _client.UpdateWatchAsync("../systeminfo", Url, "Brass");

        status.Should().Be(ChangeDetectionStatus.WatchNotFound);
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(0, 0, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateWatchAsync_ShouldReportTheSettingsProblem()
    {
        _settingsServiceMock.Setup(x => x.GetChangeDetectionSettingsAsync()).ReturnsAsync((null, null));

        var status = await _client.UpdateWatchAsync(WatchId, Url, "Brass");

        status.Should().Be(ChangeDetectionStatus.NotConfigured);
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(1, 0, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateWatchAsync_ShouldReportUnreachable_WhenTheInstanceIsDown()
    {
        var failure = new HttpRequestException("Connection refused");
        _handler.WriteException = failure;

        var status = await _client.UpdateWatchAsync(WatchId, Url, "Brass");

        status.Should().Be(ChangeDetectionStatus.Unreachable);
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, "changedetection.io update watch failed", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteWatchAsync_ShouldDeleteTheWatch_AndDropTheCachedResult()
    {
        await _client.GetLatestAsync(WatchId);

        var status = await _client.DeleteWatchAsync(WatchId);
        await _client.GetLatestAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.Ok);
        _handler.Requests.Should().Contain($"DELETE /api/v1/watch/{WatchId}");
        _handler.Requests.Count(r => r == $"GET /api/v1/watch/{WatchId}").Should().Be(2);
        VerifyUsage(3, 3, 2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteWatchAsync_ShouldReportAMissingWatch_AndKeepTheCachedResult()
    {
        await _client.GetLatestAsync(WatchId);
        _handler.DeleteStatus = HttpStatusCode.NotFound;

        var status = await _client.DeleteWatchAsync(WatchId);
        await _client.GetLatestAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.WatchNotFound);
        _handler.Requests.Count(r => r == $"GET /api/v1/watch/{WatchId}").Should().Be(1);
        VerifyUsage(2, 2, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteWatchAsync_ShouldNotCallTheInstance_WhenTheWatchIdIsNotAGuid()
    {
        var status = await _client.DeleteWatchAsync("../systeminfo");

        status.Should().Be(ChangeDetectionStatus.WatchNotFound);
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(0, 0, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteWatchAsync_ShouldUseTheGivenSettings_WithoutReadingStoredOnes()
    {
        var status = await _client.DeleteWatchAsync(WatchId, "https://old.example.com", "old-key");

        status.Should().Be(ChangeDetectionStatus.Ok);
        _handler.Requests.Should().Equal($"DELETE /api/v1/watch/{WatchId}");
        _handler.LastHost.Should().Be("old.example.com");
        _handler.LastApiKey.Should().Be("old-key");
        VerifyUsage(0, 1, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteWatchAsync_ShouldRefuseANonGuid_WhenSettingsAreGiven()
    {
        var status = await _client.DeleteWatchAsync("../systeminfo", "https://old.example.com", "old-key");

        status.Should().Be(ChangeDetectionStatus.WatchNotFound);
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(0, 0, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteWatchAsync_ShouldReportNotConfigured_WhenTheSettingsAreEmpty()
    {
        var status = await _client.DeleteWatchAsync(WatchId, null, null);

        status.Should().Be(ChangeDetectionStatus.NotConfigured);
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(0, 0, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteWatchAsync_ShouldReportUnreachable_WhenTheInstanceIsDown()
    {
        var failure = new HttpRequestException("Connection refused");
        _handler.WriteException = failure;

        var status = await _client.DeleteWatchAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.Unreachable);
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, "changedetection.io delete watch failed", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestBatchAsync_ShouldOnlyFetchUncachedWatches()
    {
        await _client.GetLatestAsync(WatchId);

        var results = await _client.GetLatestAsync(new[] { WatchId, CreatedWatchId, CreatedWatchId, " " });

        results.Keys.Should().BeEquivalentTo(WatchId, CreatedWatchId);
        results[WatchId].Available.Should().BeTrue();
        results[CreatedWatchId].Price.Should().Be(22.5m);
        _handler.Requests.Should().HaveCount(4);
        VerifyUsage(2, 2, 2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestBatchAsync_ShouldNotReadSettings_WhenEveryWatchIsCached()
    {
        await _client.GetLatestAsync(WatchId);

        var results = await _client.GetLatestAsync(new[] { WatchId });

        results[WatchId].Price.Should().Be(22.5m);
        _handler.Requests.Should().HaveCount(2);
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestBatchAsync_ShouldRefetchACachedWatchAndQueueARecheck_WhenForceRefreshing()
    {
        await _client.GetLatestAsync(WatchId);

        var results = await _client.GetLatestAsync(new[] { WatchId }, forceRefresh: true);

        results[WatchId].RecheckQueued.Should().BeTrue();
        _handler.Requests.Should().Contain($"GET /api/v1/watch/{WatchId}?recheck=1");
        _handler.Requests.Should().HaveCount(5);
        VerifyUsage(2, 2, 2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestBatchAsync_ShouldPropagateCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await _client.GetLatestAsync(new[] { WatchId }, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(1, 1, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestBatchAsync_ShouldRunAtMostFourFetchesAtOnce()
    {
        var watchIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid().ToString()).ToList();
        _handler.HoldWatchRequests = true;

        var results = await _client.GetLatestAsync(watchIds);

        results.Should().HaveCount(6);
        _handler.MaxWatchRequestsInFlight.Should().Be(4);
        VerifyUsage(1, 1, 6);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReportPending_WhenTheWatchHasNoSnapshotYet()
    {
        _handler.HistoryStatus = HttpStatusCode.NotFound;

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Pending);
        result.Available.Should().BeFalse();
        result.SourceUrl.Should().Be(Url);
        VerifyUsage(1, 1, 1);
        _loggerMock.VerifyLog(LogLevel.Debug, $"changedetection.io returned NotFound for the latest snapshot of watch {WatchId}");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, ChangeDetectionStatus.Unreachable)]
    [InlineData(HttpStatusCode.Forbidden, ChangeDetectionStatus.Unauthorized)]
    public async Task GetLatestAsync_ShouldKeepTheMetadata_WhenTheSnapshotCannotBeRead(HttpStatusCode statusCode, ChangeDetectionStatus expected)
    {
        _handler.HistoryStatus = statusCode;

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(expected);
        result.SourceUrl.Should().Be(Url);
        result.Title.Should().Be("Brass: Birmingham");
        VerifyUsage(1, 1, 1);
        _loggerMock.VerifyLog(LogLevel.Debug, $"changedetection.io returned {statusCode} for the latest snapshot of watch {WatchId}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWatchInfoAsync_ShouldReturnTheWatchMetadata()
    {
        var (status, info) = await _client.GetWatchInfoAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.Ok);
        info.Should().Be(new ChangeDetectionWatchInfo(Url, "Brass: Birmingham", DateTimeOffset.FromUnixTimeSeconds(LastChecked).UtcDateTime));
        _handler.Requests.Should().Equal($"GET /api/v1/watch/{WatchId}");
        VerifyUsage(1, 1, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWatchInfoAsync_ShouldReturnAnEmptyUrl_WhenTheWatchHasNone()
    {
        _handler.WatchBody = "{}";

        var (status, info) = await _client.GetWatchInfoAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.Ok);
        info.Should().Be(new ChangeDetectionWatchInfo(string.Empty, null, null));
        VerifyUsage(1, 1, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWatchInfoAsync_ShouldReportWatchNotFound()
    {
        _handler.WatchStatus = HttpStatusCode.NotFound;

        var (status, info) = await _client.GetWatchInfoAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.WatchNotFound);
        info.Should().BeNull();
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io returned NotFound for watch {WatchId}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWatchInfoAsync_ShouldReportTheSettingsProblem()
    {
        _settingsServiceMock.Setup(x => x.GetChangeDetectionSettingsAsync()).ReturnsAsync((null, null));

        var (status, info) = await _client.GetWatchInfoAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.NotConfigured);
        info.Should().BeNull();
        VerifyUsage(1, 0, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWatchInfoAsync_ShouldReportUnreachable_WhenTheInstanceIsDown()
    {
        var failure = new HttpRequestException("Connection refused");
        _handler.WatchException = failure;

        var (status, info) = await _client.GetWatchInfoAsync(WatchId);

        status.Should().Be(ChangeDetectionStatus.Unreachable);
        info.Should().BeNull();
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, "changedetection.io watch lookup failed", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchAsync_ShouldPostARestockWatchAndReturnItsId()
    {
        var (status, watchId) = await _client.CreateWatchAsync(Url, "Brass");

        status.Should().Be(ChangeDetectionStatus.Ok);
        watchId.Should().Be(CreatedWatchId);
        _handler.Requests.Should().ContainSingle().Which.Should().Be("POST /api/v1/watch");
        _handler.LastPostBody.Should().Be(
            """{"url":"https://shop.example.com/brass","title":"Brass","processor":"restock_diff","tag":"boardgametracker","time_between_check":{"hours":24}}""");
        VerifyUsage(1, 1, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchAsync_ShouldAcceptABareStringId()
    {
        _handler.CreateBody = $"\"{CreatedWatchId}\"";

        var (status, watchId) = await _client.CreateWatchAsync(Url, "Brass");

        status.Should().Be(ChangeDetectionStatus.Ok);
        watchId.Should().Be(CreatedWatchId);
        VerifyUsage(1, 1, 0);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("42")]
    [InlineData("""{"uuid":"  "}""")]
    public async Task CreateWatchAsync_ShouldReportAParseError_WhenTheAnswerHoldsNoId(string body)
    {
        _handler.CreateBody = body;

        var (status, watchId) = await _client.CreateWatchAsync(Url, "Brass");

        status.Should().Be(ChangeDetectionStatus.ParseError);
        watchId.Should().BeNull();
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io created a watch for {Url} but returned no uuid");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchAsync_ShouldReportUnauthorized_WhenTheKeyIsRejected()
    {
        _handler.CreateStatus = HttpStatusCode.Forbidden;
        _handler.CreateBody = "no access";

        var (status, watchId) = await _client.CreateWatchAsync(Url, "Brass");

        status.Should().Be(ChangeDetectionStatus.Unauthorized);
        watchId.Should().BeNull();
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io refused to create a watch for {Url}: Forbidden no access");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(500, 500)]
    [InlineData(501, 500)]
    public async Task CreateWatchAsync_ShouldLogAtMost500CharactersOfTheError(int length, int logged)
    {
        _handler.CreateStatus = HttpStatusCode.BadRequest;
        _handler.CreateBody = new string('x', length);

        var (status, _) = await _client.CreateWatchAsync(Url, "Brass");

        status.Should().Be(ChangeDetectionStatus.Unreachable);
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io refused to create a watch for {Url}: BadRequest {new string('x', logged)}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchAsync_ShouldReportTheSettingsProblem()
    {
        _settingsServiceMock.Setup(x => x.GetChangeDetectionSettingsAsync()).ReturnsAsync((null, null));

        var (status, watchId) = await _client.CreateWatchAsync(Url, "Brass");

        status.Should().Be(ChangeDetectionStatus.NotConfigured);
        watchId.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(1, 0, 0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWatchAsync_ShouldReportUnreachable_WhenTheInstanceIsDown()
    {
        var failure = new HttpRequestException("Connection refused");
        _handler.WriteException = failure;

        var (status, watchId) = await _client.CreateWatchAsync(Url, "Brass");

        status.Should().Be(ChangeDetectionStatus.Unreachable);
        watchId.Should().BeNull();
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, "changedetection.io create watch failed", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReportOk_WhenTheInstanceAnswers()
    {
        var status = await _client.TestConnectionAsync();

        status.Should().Be(ChangeDetectionStatus.Ok);
        _handler.Requests.Should().Equal("GET /api/v1/systeminfo");
        VerifyUsage(1, 1, 0);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, ChangeDetectionStatus.Unreachable)]
    [InlineData(HttpStatusCode.Unauthorized, ChangeDetectionStatus.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ChangeDetectionStatus.Unauthorized)]
    [InlineData(HttpStatusCode.Found, ChangeDetectionStatus.Misconfigured)]
    public async Task TestConnectionAsync_ShouldReportWhyItFailed(HttpStatusCode statusCode, ChangeDetectionStatus expected)
    {
        _handler.SystemInfoStatus = statusCode;

        var status = await _client.TestConnectionAsync();

        status.Should().Be(expected);
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io connection test returned {statusCode}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReportUnreachable_WhenTheAnswerIsNotJson()
    {
        _handler.SystemInfoBody = "<html>login</html>";

        var status = await _client.TestConnectionAsync();

        status.Should().Be(ChangeDetectionStatus.Unreachable);
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, "changedetection.io connection test failed");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, null, ChangeDetectionStatus.NotConfigured)]
    [InlineData("not-a-valid-url", "api-key", ChangeDetectionStatus.Misconfigured)]
    public async Task TestConnectionAsync_ShouldReportTheSettingsProblem_WithoutCallingTheInstance(string? baseUrl, string? apiKey, ChangeDetectionStatus expected)
    {
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .ReturnsAsync((baseUrl, apiKey));

        var status = await _client.TestConnectionAsync();

        status.Should().Be(expected);
        _handler.Requests.Should().BeEmpty();
        VerifyUsage(1, 0, 0);
        if (expected == ChangeDetectionStatus.Misconfigured)
        {
            _loggerMock.VerifyLog(LogLevel.Warning, "changedetection.io base URL is not a valid http(s) URL");
        }

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReportUnreachable_WhenTheInstanceIsDown()
    {
        var failure = new HttpRequestException("Connection refused");
        _handler.SystemInfoException = failure;

        var status = await _client.TestConnectionAsync();

        status.Should().Be(ChangeDetectionStatus.Unreachable);
        VerifyUsage(1, 1, 0);
        _loggerMock.VerifyLog(LogLevel.Warning, "changedetection.io connection test failed", failure);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("""{"price":"12,50","in_stock":true}""", 12.5)]
    [InlineData("""{"price":"1299","in_stock":true}""", 1299)]
    [InlineData("""{"price":39.99,"in_stock":true}""", 39.99)]
    public async Task GetLatestAsync_ShouldReadTheRestockPrice_WithTheSameRulesAsTheSnapshot(string restock, double expected)
    {
        _handler.WatchBody = $$"""{"url":"https://shop.example.com/brass","restock":{{restock}}}""";

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.Price.Should().Be((decimal)expected);
        _handler.Requests.Should().ContainSingle();
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("""{"price":"1,234","in_stock":true}""")]
    [InlineData("""{"price":-5,"in_stock":true}""")]
    [InlineData("""{"price":-0.01,"in_stock":true}""")]
    [InlineData("""{"price":"-5","in_stock":true}""")]
    [InlineData("""{"price":1e40,"in_stock":true}""")]
    public async Task GetLatestAsync_ShouldDropAnAmbiguousOrNegativeRestockPrice_ButKeepTheStock(string restock)
    {
        _handler.WatchBody = $$"""{"url":"https://shop.example.com/brass","restock":{{restock}}}""";

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.InStock.Should().BeTrue();
        result.Price.Should().BeNull();
        _handler.Requests.Should().ContainSingle();
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("1e300")]
    [InlineData("253402300800")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("\"yesterday\"")]
    public async Task GetLatestAsync_ShouldIgnoreAnUnusableLastChecked_InsteadOfFailingTheWholeFetch(string lastChecked)
    {
        _handler.WatchBody = $$"""{"url":"https://shop.example.com/brass","last_checked":{{lastChecked}}}""";

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.Ok);
        result.Price.Should().Be(22.5m);
        result.CheckedAt.Should().BeNull();
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("1", 1970, 1, 1, 0, 0, 1)]
    [InlineData("253402300799", 9999, 12, 31, 23, 59, 59)]
    public async Task GetLatestAsync_ShouldAcceptTheOutermostLastChecked(string lastChecked, int year, int month, int day, int hour, int minute, int second)
    {
        _handler.WatchBody = $$"""{"last_checked":{{lastChecked}}}""";

        var result = await _client.GetLatestAsync(WatchId);

        result.CheckedAt.Should().Be(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc));
        VerifyUsage(1, 1, 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReportAParseError_WhenTheSnapshotIsNotARestockSnapshot()
    {
        _handler.HistoryBody = "<html>Some page text</html>";

        var result = await _client.GetLatestAsync(WatchId);

        result.Status.Should().Be(ChangeDetectionStatus.ParseError);
        result.Available.Should().BeFalse();
        result.SourceUrl.Should().Be(Url);
        VerifyUsage(1, 1, 1);
        _loggerMock.VerifyLog(LogLevel.Warning, $"changedetection.io snapshot for watch {WatchId} is not a restock snapshot");
        VerifyNoOtherCalls();
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; private set; } = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by) => UtcNow += by;
    }

    private sealed class RoutingHandler : HttpMessageHandler
    {
        private int _watchRequestsInFlight;
        private int _maxWatchRequestsInFlight;

        public List<string> Requests { get; } = new();
        public string WatchBody { get; set; } = WatchJson;
        public HttpStatusCode WatchStatus { get; set; } = HttpStatusCode.OK;
        public string HistoryBody { get; set; } = HistoryText;
        public HttpStatusCode HistoryStatus { get; set; } = HttpStatusCode.OK;
        public HttpStatusCode CreateStatus { get; set; } = HttpStatusCode.Created;
        public string CreateBody { get; set; } = $$$"""{"uuid":"{{{CreatedWatchId}}}"}""";
        public HttpStatusCode SystemInfoStatus { get; set; } = HttpStatusCode.OK;
        public string SystemInfoBody { get; set; } = """{"version":"0.50.1"}""";
        public HttpStatusCode RecheckStatus { get; set; } = HttpStatusCode.OK;
        public Exception? WatchException { get; set; }
        public Exception? WriteException { get; set; }
        public Exception? SystemInfoException { get; set; }
        public bool HangUntilCancelled { get; set; }
        public bool HoldWatchRequests { get; set; }
        public int MaxWatchRequestsInFlight => _maxWatchRequestsInFlight;
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? LastPostBody { get; private set; }
        public string? LastPutBody { get; private set; }
        public string? LastApiKey { get; private set; }
        public string? LastHost { get; private set; }
        public HttpStatusCode PutStatus { get; set; } = HttpStatusCode.OK;
        public HttpStatusCode DeleteStatus { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            lock (Requests)
            {
                Requests.Add($"{request.Method} {path}");
            }

            LastApiKey = request.Headers.GetValues("x-api-key").Single();
            LastHost = request.RequestUri.Host;

            if (request.Method != HttpMethod.Get && WriteException != null)
            {
                throw WriteException;
            }

            if (request.Method == HttpMethod.Post && path == "/api/v1/watch")
            {
                LastPostBody = request.Content == null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);
                return Response(CreateStatus, CreateBody);
            }

            if (request.Method == HttpMethod.Delete)
            {
                return Response(DeleteStatus, "OK");
            }

            if (request.Method == HttpMethod.Put)
            {
                LastPutBody = request.Content == null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);
                return Response(PutStatus, "OK");
            }

            if (path.Contains("recheck=1", StringComparison.Ordinal))
            {
                return Response(RecheckStatus, "OK");
            }

            if (path == "/api/v1/systeminfo")
            {
                if (SystemInfoException != null)
                {
                    throw SystemInfoException;
                }

                return Response(SystemInfoStatus, SystemInfoBody);
            }

            if (path.EndsWith("/history/latest", StringComparison.Ordinal))
            {
                return Response(HistoryStatus, HistoryBody);
            }

            if (WatchException != null)
            {
                throw WatchException;
            }

            if (HangUntilCancelled)
            {
                RequestStarted.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            if (HoldWatchRequests)
            {
                var inFlight = Interlocked.Increment(ref _watchRequestsInFlight);
                lock (Requests)
                {
                    _maxWatchRequestsInFlight = Math.Max(_maxWatchRequestsInFlight, inFlight);
                }

                await Task.Delay(100, cancellationToken);
                Interlocked.Decrement(ref _watchRequestsInFlight);
            }

            return Response(WatchStatus, WatchBody);
        }

        private static HttpResponseMessage Response(HttpStatusCode statusCode, string body) =>
            new(statusCode) { Content = new StringContent(body) };
    }
}
