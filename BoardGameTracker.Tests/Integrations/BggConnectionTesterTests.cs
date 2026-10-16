using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Integrations;
using BoardGameTracker.Core.Integrations;
using BoardGameTracker.Core.Settings.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Integrations;

public class BggConnectionTesterTests
{
    private const string ApiKey = "secret-bgg-key";

    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<BggConnectionTester>> _loggerMock;
    private readonly StubHandler _handler;
    private readonly BggConnectionTester _tester;

    public BggConnectionTesterTests()
    {
        _handler = new StubHandler();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(BggConnectionTester.HttpClientName))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));

        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock.Setup(x => x.GetBggApiKeyAsync()).ReturnsAsync($" {ApiKey} ");

        _loggerMock = new Mock<ILogger<BggConnectionTester>>();
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _tester = new BggConnectionTester(_httpClientFactoryMock.Object, _settingsServiceMock.Object, _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _httpClientFactoryMock.VerifyNoOtherCalls();
        _settingsServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Integration_ShouldBeBgg()
    {
        _tester.Integration.Should().Be(IntegrationKind.Bgg);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TestAsync_ShouldReportNotConfigured_WithoutCallingBgg_WhenNoKeyIsSet(string? key)
    {
        _settingsServiceMock.Setup(x => x.GetBggApiKeyAsync()).ReturnsAsync(key);

        var result = await _tester.TestAsync();

        result.Should().Be(IntegrationTestResult.NotConfigured);
        _handler.Requests.Should().BeEmpty();
        _settingsServiceMock.Verify(x => x.GetBggApiKeyAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestAsync_ShouldReportOk_AndSendTheTrimmedKeyAsBearerToken_WhenBggAccepts()
    {
        _handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK);

        var result = await _tester.TestAsync();

        result.Status.Should().Be(IntegrationConnectionStatus.Ok);
        result.MessageKey.Should().Be(IntegrationMessageKeys.Ok);
        var request = _handler.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.ToString().Should().Be(BggConnectionTester.ProbeUrl);
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be(ApiKey);
        _settingsServiceMock.Verify(x => x.GetBggApiKeyAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(BggConnectionTester.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, IntegrationConnectionStatus.Unauthorized, IntegrationMessageKeys.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, IntegrationConnectionStatus.Unauthorized, IntegrationMessageKeys.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests, IntegrationConnectionStatus.RateLimited, IntegrationMessageKeys.RateLimited)]
    [InlineData(HttpStatusCode.InternalServerError, IntegrationConnectionStatus.Unreachable, IntegrationMessageKeys.UnexpectedResponse)]
    [InlineData(HttpStatusCode.ServiceUnavailable, IntegrationConnectionStatus.Unreachable, IntegrationMessageKeys.UnexpectedResponse)]
    [InlineData(HttpStatusCode.Accepted, IntegrationConnectionStatus.Ok, IntegrationMessageKeys.Ok)]
    public async Task TestAsync_ShouldMapTheBggResponse(HttpStatusCode statusCode, IntegrationConnectionStatus expected, string messageKey)
    {
        _handler.Respond = _ => new HttpResponseMessage(statusCode);

        var result = await _tester.TestAsync();

        result.Status.Should().Be(expected);
        result.MessageKey.Should().Be(messageKey);
        _settingsServiceMock.Verify(x => x.GetBggApiKeyAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(BggConnectionTester.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestAsync_ShouldReportATimeout_WhenTheRequestTimesOut()
    {
        _handler.Respond = _ => throw new TaskCanceledException("timeout", new System.TimeoutException());

        var result = await _tester.TestAsync();

        result.Status.Should().Be(IntegrationConnectionStatus.Unreachable);
        result.MessageKey.Should().Be(IntegrationMessageKeys.Timeout);
        _settingsServiceMock.Verify(x => x.GetBggApiKeyAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(BggConnectionTester.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestAsync_ShouldReportUnreachable_WhenBggCannotBeReached()
    {
        _handler.Respond = _ => throw new HttpRequestException(HttpRequestError.NameResolutionError, "no dns");

        var result = await _tester.TestAsync();

        result.Status.Should().Be(IntegrationConnectionStatus.Unreachable);
        result.MessageKey.Should().Be(IntegrationMessageKeys.Unreachable);
        _settingsServiceMock.Verify(x => x.GetBggApiKeyAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(BggConnectionTester.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestAsync_ShouldReportUnauthorized_WhenTheKeyCannotBeSentAsAHeader()
    {
        _handler.Respond = _ => throw new System.FormatException("bad header");

        var result = await _tester.TestAsync();

        result.Should().Be(IntegrationTestResult.Unauthorized);
        _settingsServiceMock.Verify(x => x.GetBggApiKeyAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(BggConnectionTester.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestAsync_ShouldRethrow_WhenTheCallerCancels()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => _tester.TestAsync(cts.Token);

        await act.Should().ThrowAsync<System.OperationCanceledException>();
        _settingsServiceMock.Verify(x => x.GetBggApiKeyAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(BggConnectionTester.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestAsync_ShouldNeverLogTheKey()
    {
        _handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized);

        await _tester.TestAsync();

        var logged = _loggerMock.Invocations
            .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
            .Select(invocation => invocation.Arguments[2]?.ToString() ?? string.Empty)
            .ToList();
        logged.Should().ContainSingle().Which.Should().Contain("401");
        logged.Should().NotContain(message => message.Contains(ApiKey));
        _settingsServiceMock.Verify(x => x.GetBggApiKeyAsync(), Times.Once);
        _httpClientFactoryMock.Verify(x => x.CreateClient(BggConnectionTester.HttpClientName), Times.Once);
        VerifyNoOtherCalls();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public System.Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            return Task.FromResult(Respond(request));
        }
    }
}
