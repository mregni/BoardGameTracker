using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Moq;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using System.Text;
using System.Text.Json;
using Sentry.Protocol.Envelopes;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

[Collection(EnvironmentVariablesCollection.Name)]
public class WebHostBuilderExtensionsTests
{
    private readonly Mock<ITransport> _transportMock;
    private readonly List<Envelope> _envelopes = [];
    private readonly SentryOptions _options;

    public WebHostBuilderExtensionsTests()
    {
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

    private void VerifyNoOtherCalls()
    {
        _transportMock.VerifyNoOtherCalls();
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

        _transportMock.Verify(x => x.SendEnvelopeAsync(It.IsAny<Envelope>(), It.IsAny<CancellationToken>()), Times.Once);
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

        _transportMock.Verify(x => x.SendEnvelopeAsync(It.IsAny<Envelope>(), It.IsAny<CancellationToken>()), Times.Once);
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

        WebHostBuilderExtensions.ConfigureSentry(options);
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

        _transportMock.Verify(x => x.SendEnvelopeAsync(It.IsAny<Envelope>(), It.IsAny<CancellationToken>()), Times.Once);
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
