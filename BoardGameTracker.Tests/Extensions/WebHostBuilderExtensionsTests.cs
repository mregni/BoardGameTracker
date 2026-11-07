using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Moq;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using System.Text;
using System.Text.Json;
using Sentry.Protocol.Envelopes;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

[Collection(EnvironmentVariablesCollection.Name)]
public class WebHostBuilderExtensionsTests : IDisposable
{
    private const string DefaultDsn = "https://3d89aa9317b0a7b3108edbafd31da95a@o4506121302573056.ingest.us.sentry.io/4506121326559232";
    private static readonly string[] EnvironmentKeys = ["STATISTICS_ENABLED", "SENTRY_DSN", "ASPNETCORE_ENVIRONMENT", "ENVIRONMENT", "LOGLEVEL"];

    private readonly Dictionary<string, string?> _originalEnvironmentVariables = [];
    private readonly Mock<IWebHostBuilder> _webHostBuilderMock = new();
    private readonly Mock<ITransport> _transportMock;
    private readonly List<Envelope> _envelopes = [];
    private readonly SentryOptions _options;

    public WebHostBuilderExtensionsTests()
    {
        foreach (var key in EnvironmentKeys)
        {
            _originalEnvironmentVariables[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, null);
        }

        _transportMock = new Mock<ITransport>();
        _transportMock
            .Setup(x => x.SendEnvelopeAsync(It.IsAny<Envelope>(), It.IsAny<CancellationToken>()))
            .Callback<Envelope, CancellationToken>((envelope, _) => _envelopes.Add(envelope))
            .Returns(Task.CompletedTask);

        _options = new SentryOptions
        {
            Dsn = "https://public@sentry.invalid/1",
            Transport = _transportMock.Object,
            AutoSessionTracking = false,
            SendClientReports = false,
            TracesSampleRate = 1,
        };
        _options.ApplySentryScrubbing();
    }

    public void Dispose()
    {
        foreach (var kvp in _originalEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _transportMock.VerifyNoOtherCalls();
        _webHostBuilderMock.VerifyNoOtherCalls();
    }

    private void VerifyEnvelopeSent()
    {
        _transportMock.Verify(x => x.SendEnvelopeAsync(It.Is<Envelope>(e => _envelopes.Contains(e)), It.IsAny<CancellationToken>()), Times.Once);
    }

    private SentryClient ClientFor(SentryAspNetCoreOptions options)
    {
        options.Dsn = "https://public@sentry.invalid/1";
        options.Transport = _transportMock.Object;
        options.AutoSessionTracking = false;
        options.SendClientReports = false;
        return new SentryClient(options);
    }

    [Fact]
    public async Task ApplySentryScrubbing_ShouldFilterEventRequestAndBreadcrumbs()
    {
        var client = new SentryClient(_options);
        var scope = new Scope(_options);
        scope.Request.Url = "https://bgt.example.com/api/auth/oidc/x/callback?code=abc&state=def&lang=en";
        scope.Request.QueryString = "?code=abc&state=def&lang=en";
        scope.AddBreadcrumb(new Breadcrumb("GET /rsvp?linkId=abc", "http", new Dictionary<string, string> { ["url"] = "/auth-callback?code=abc&page=1" }));

        client.CaptureEvent(new SentryEvent { Message = "boom" }, scope);
        await client.FlushAsync(TimeSpan.FromSeconds(5));

        var @event = await SingleItem("event");
        var request = @event.GetProperty("request");
        request.GetProperty("url").GetString().Should().Be("https://bgt.example.com/api/auth/oidc/x/callback?code=[Filtered]&state=[Filtered]&lang=en");
        request.GetProperty("query_string").GetString().Should().Be("?code=[Filtered]&state=[Filtered]&lang=en");
        var breadcrumb = @event.GetProperty("breadcrumbs").EnumerateArray().Should().ContainSingle().Subject;
        breadcrumb.GetProperty("message").GetString().Should().Be("GET /rsvp?linkId=[Filtered]");
        breadcrumb.GetProperty("data").GetProperty("url").GetString().Should().Be("/auth-callback?code=[Filtered]&page=1");

        VerifyEnvelopeSent();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplySentryScrubbing_ShouldFilterTransactionRequest()
    {
        var client = new SentryClient(_options);
        var tracer = new TransactionTracer(DisabledHub.Instance, new TransactionContext("GET api/auth/oidc/{provider}/callback", "http.server", isSampled: true, nameSource: TransactionNameSource.Route));
        tracer.Finish();
        var transaction = new SentryTransaction(tracer);
        transaction.Request.Url = "https://bgt.example.com/api/auth/oidc/x/callback?code=abc&lang=en";

        client.CaptureTransaction(transaction);
        await client.FlushAsync(TimeSpan.FromSeconds(5));

        var sent = await SingleItem("transaction");
        sent.GetProperty("transaction").GetString().Should().Be("GET api/auth/oidc/{provider}/callback");
        sent.GetProperty("request").GetProperty("url").GetString().Should().Be("https://bgt.example.com/api/auth/oidc/x/callback?code=[Filtered]&lang=en");

        VerifyEnvelopeSent();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplySentryScrubbing_ShouldDropTransactionWithCodeInName()
    {
        var client = new SentryClient(_options);
        var tracer = new TransactionTracer(DisabledHub.Instance, new TransactionContext("GET /auth-callback?code=abc", "http.server", isSampled: true, nameSource: TransactionNameSource.Url));
        tracer.Finish();

        client.CaptureTransaction(new SentryTransaction(tracer));
        await client.FlushAsync(TimeSpan.FromSeconds(5));

        _envelopes.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ConfigureSentry_ShouldNeverCaptureRequestBodiesOrPersonalData_AndScrubEvents()
    {
        var options = new SentryAspNetCoreOptions
        {
            MaxRequestBodySize = RequestSize.Always,
            SendDefaultPii = true
        };

        BoardGameTracker.Common.Extensions.WebHostBuilderExtensions.ConfigureSentry(options);
        options.Dsn = "https://public@sentry.invalid/1";
        options.Transport = _transportMock.Object;
        options.AutoSessionTracking = false;
        options.SendClientReports = false;

        options.MaxRequestBodySize.Should().Be(RequestSize.None);
        options.SendDefaultPii.Should().BeFalse();

        var client = new SentryClient(options);
        var scope = new Scope(options);
        scope.Request.Url = "https://bgt.example.com/api/auth/oidc/x/callback?code=abc&lang=en";
        client.CaptureEvent(new SentryEvent { Message = "boom" }, scope);
        await client.FlushAsync(TimeSpan.FromSeconds(5));

        var @event = await SingleItem("event");
        @event.GetProperty("request").GetProperty("url").GetString().Should().Be("https://bgt.example.com/api/auth/oidc/x/callback?code=[Filtered]&lang=en");

        VerifyEnvelopeSent();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, DefaultDsn)]
    [InlineData("", DefaultDsn)]
    [InlineData("https://own@sentry.example.com/2", "https://own@sentry.example.com/2")]
    public void ConfigureSentry_ShouldUseTheOwnDsn_OnlyWhenOneIsSet(string? configured, string expected)
    {
        Environment.SetEnvironmentVariable("SENTRY_DSN", configured);
        var options = new SentryAspNetCoreOptions();

        BoardGameTracker.Common.Extensions.WebHostBuilderExtensions.ConfigureSentry(options);

        options.Dsn.Should().Be(expected);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void ConfigureSentry_ShouldSampleATenthOfTheTraces_AndNameTheEnvironment()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "staging");
        var options = new SentryAspNetCoreOptions();

        BoardGameTracker.Common.Extensions.WebHostBuilderExtensions.ConfigureSentry(options);

        options.Environment.Should().Be("staging");
        options.TracesSampleRate.Should().Be(0.1);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("development", "debug", true)]
    [InlineData("Development", "debug", true)]
    [InlineData("development", "info", false)]
    [InlineData("production", "debug", false)]
    public void ConfigureSentry_ShouldOnlyDebug_InDevelopmentWithTheDebugLogLevel(string environment, string logLevel, bool expected)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
        Environment.SetEnvironmentVariable("LOGLEVEL", logLevel);
        var options = new SentryAspNetCoreOptions();

        BoardGameTracker.Common.Extensions.WebHostBuilderExtensions.ConfigureSentry(options);

        options.Debug.Should().Be(expected);
        LogLevelExtensions.GetEnvironmentLogLevel().Should().Be(logLevel == "debug" ? LogEventLevel.Debug : LogEventLevel.Information);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ConfigureSentry_ShouldDropExceptionsThatAreNotServerErrors()
    {
        var options = new SentryAspNetCoreOptions();
        BoardGameTracker.Common.Extensions.WebHostBuilderExtensions.ConfigureSentry(options);
        var client = ClientFor(options);

        client.CaptureEvent(new SentryEvent(new DomainException("error.x")), new Scope(options));
        await client.FlushAsync(TimeSpan.FromSeconds(5));

        _envelopes.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConfigureSentry_ShouldSendServerErrorsAndMessages_WithoutTheServerName(bool withException)
    {
        var options = new SentryAspNetCoreOptions();
        BoardGameTracker.Common.Extensions.WebHostBuilderExtensions.ConfigureSentry(options);
        var client = ClientFor(options);
        var @event = withException ? new SentryEvent(new InvalidOperationException("boom")) : new SentryEvent { Message = "boom" };
        @event.ServerName = "host-1";

        client.CaptureEvent(@event, new Scope(options));
        await client.FlushAsync(TimeSpan.FromSeconds(5));

        var sent = await SingleItem("event");
        sent.TryGetProperty("server_name", out _).Should().BeFalse();
        VerifyEnvelopeSent();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("yes")]
    public void UseConfiguredSentry_ShouldLeaveTheHostAlone_WhenStatisticsAreOff(string? value)
    {
        Environment.SetEnvironmentVariable("STATISTICS_ENABLED", value);

        var result = _webHostBuilderMock.Object.UseConfiguredSentry();

        result.Should().BeSameAs(_webHostBuilderMock.Object);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    public void UseConfiguredSentry_ShouldAddSentry_WhenStatisticsAreOn(string value)
    {
        Environment.SetEnvironmentVariable("STATISTICS_ENABLED", value);
        var services = new ServiceCollection();
        _webHostBuilderMock
            .Setup(x => x.ConfigureServices(It.IsAny<Action<IServiceCollection>>()))
            .Callback<Action<IServiceCollection>>(configure => configure(services))
            .Returns(_webHostBuilderMock.Object);
        _webHostBuilderMock
            .Setup(x => x.ConfigureServices(It.IsAny<Action<WebHostBuilderContext, IServiceCollection>>()))
            .Returns(_webHostBuilderMock.Object);

        var result = _webHostBuilderMock.Object.UseConfiguredSentry();

        result.Should().BeSameAs(_webHostBuilderMock.Object);
        services.Should().Contain(x => x.ServiceType == typeof(Microsoft.AspNetCore.Hosting.IStartupFilter));
        _webHostBuilderMock.Verify(x => x.ConfigureServices(It.IsAny<Action<IServiceCollection>>()), Times.Once);
        _webHostBuilderMock.Verify(x => x.ConfigureServices(It.IsAny<Action<WebHostBuilderContext, IServiceCollection>>()), Times.Once);
        VerifyNoOtherCalls();
    }

    private async Task<JsonElement> SingleItem(string type)
    {
        var envelope = _envelopes.Should().ContainSingle().Subject;
        using var stream = new MemoryStream();
        await envelope.SerializeAsync(stream, null);
        var lines = Encoding.UTF8.GetString(stream.ToArray()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var index = Array.FindIndex(lines, line => JsonDocument.Parse(line).RootElement.TryGetProperty("type", out var t) && t.GetString() == type);
        index.Should().BeGreaterThan(0);
        return JsonDocument.Parse(lines[index + 1]).RootElement.Clone();
    }
}
