using BoardGameTracker.Common.Helpers;
using FluentAssertions;
using Sentry;
using Sentry.Extensibility;
using Xunit;

namespace BoardGameTracker.Tests.Helpers;

public class SentryScrubberTests
{
    private const string LinkId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

    [Theory]
    [InlineData("https://bgt.example.com/api/auth/oidc/authelia/callback?code=abc123&state=xyz", "https://bgt.example.com/api/auth/oidc/authelia/callback?code=[Filtered]&state=[Filtered]")]
    [InlineData("/auth-callback?code=abc&state=def&page=2", "/auth-callback?code=[Filtered]&state=[Filtered]&page=2")]
    [InlineData("/rsvp?linkId=" + LinkId, "/rsvp?linkId=[Filtered]")]
    [InlineData("?CODE=abc", "?CODE=[Filtered]")]
    [InlineData("code=abc&session_state=s1", "code=[Filtered]&session_state=[Filtered]")]
    [InlineData("/cb?token=t&id_token=i&access_token=a&refresh_token=r", "/cb?token=[Filtered]&id_token=[Filtered]&access_token=[Filtered]&refresh_token=[Filtered]")]
    [InlineData("/x?password=p&client_secret=s&api_key=k&apiKey=k2", "/x?password=[Filtered]&client_secret=[Filtered]&api_key=[Filtered]&apiKey=[Filtered]")]
    [InlineData("/x#access_token=a&state=s", "/x#access_token=[Filtered]&state=[Filtered]")]
    [InlineData("/api/gamenight/link/" + LinkId + "/rsvp", "/api/gamenight/link/[Filtered]/rsvp")]
    [InlineData("/api/manual/gamenight/" + LinkId + "/manual/4/download", "/api/manual/gamenight/[Filtered]/manual/4/download")]
    [InlineData("/api/games?page=2&sort=name&zipcode=1000&codes=1", "/api/games?page=2&sort=name&zipcode=1000&codes=1")]
    [InlineData("/api/gamenight/4", "/api/gamenight/4")]
    [InlineData("GET api/auth/oidc/{provider}/callback", "GET api/auth/oidc/{provider}/callback")]
    public void Scrub_ShouldFilterSensitiveValuesOnly(string input, string expected)
    {
        SentryScrubber.Scrub(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Scrub_ShouldReturnEmptyInputUnchanged(string? input)
    {
        SentryScrubber.Scrub(input).Should().Be(input);
    }

    [Fact]
    public void ScrubEvent_ShouldFilterRequestMessageTransactionNameAndTags()
    {
        var @event = new SentryEvent
        {
            TransactionName = "GET /auth-callback?code=abc",
            Message = new SentryMessage { Message = "failed /rsvp?linkId=abc", Formatted = "failed /rsvp?linkId=abc" },
        };
        @event.Request.Url = "https://bgt.example.com/api/auth/oidc/x/callback?code=abc&state=def&lang=en";
        @event.Request.QueryString = "?code=abc&state=def&lang=en";
        @event.Request.Headers["Referer"] = "https://bgt.example.com/auth-callback?code=abc";
        @event.Request.Headers["Accept"] = "application/json";
        @event.SetTag("url", "/auth-callback?code=abc");
        @event.SetTag("route", "games");

        var result = SentryScrubber.ScrubEvent(@event);

        result.Should().BeSameAs(@event);
        result.Request.Url.Should().Be("https://bgt.example.com/api/auth/oidc/x/callback?code=[Filtered]&state=[Filtered]&lang=en");
        result.Request.QueryString.Should().Be("?code=[Filtered]&state=[Filtered]&lang=en");
        result.Request.Headers["Referer"].Should().Be("https://bgt.example.com/auth-callback?code=[Filtered]");
        result.Request.Headers["Accept"].Should().Be("application/json");
        result.TransactionName.Should().Be("GET /auth-callback?code=[Filtered]");
        result.Message!.Message.Should().Be("failed /rsvp?linkId=[Filtered]");
        result.Message.Formatted.Should().Be("failed /rsvp?linkId=[Filtered]");
        result.Tags["url"].Should().Be("/auth-callback?code=[Filtered]");
        result.Tags["route"].Should().Be("games");
    }

    [Fact]
    public void ScrubEvent_ShouldHandleEventWithoutMessage()
    {
        var @event = new SentryEvent();

        var result = SentryScrubber.ScrubEvent(@event);

        result.Message.Should().BeNull();
        result.Request.Url.Should().BeNull();
    }

    [Fact]
    public void ScrubTransaction_ShouldFilterRequestTagsAndSpans()
    {
        var transaction = CreateTransaction("GET api/auth/oidc/{provider}/callback");
        transaction.Request.Url = "https://bgt.example.com/api/auth/oidc/x/callback?code=abc&lang=en";
        transaction.Request.QueryString = "?code=abc&lang=en";
        transaction.SetTag("http.url", "/api/auth/oidc/x/callback?state=s");
        transaction.SetTag("route", "callback");

        var result = SentryScrubber.ScrubTransaction(transaction);

        result.Should().BeSameAs(transaction);
        result!.Name.Should().Be("GET api/auth/oidc/{provider}/callback");
        result.Request.Url.Should().Be("https://bgt.example.com/api/auth/oidc/x/callback?code=[Filtered]&lang=en");
        result.Request.QueryString.Should().Be("?code=[Filtered]&lang=en");
        result.Tags["http.url"].Should().Be("/api/auth/oidc/x/callback?state=[Filtered]");
        result.Tags["route"].Should().Be("callback");
        var span = result.Spans.Should().ContainSingle().Subject;
        span.Description.Should().Be("GET https://idp.example.com/userinfo?access_token=[Filtered]");
        span.Data["url"].Should().Be("https://idp.example.com/userinfo?access_token=[Filtered]");
        span.Data["status"].Should().Be(200);
    }

    [Fact]
    public void ScrubTransaction_ShouldDropTransactionWhoseNameContainsSecrets()
    {
        var transaction = CreateTransaction("GET /auth-callback?code=abc");

        SentryScrubber.ScrubTransaction(transaction).Should().BeNull();
    }

    [Fact]
    public void ScrubBreadcrumb_ShouldFilterMessageAndData()
    {
        var breadcrumb = new Breadcrumb(
            "navigated to /auth-callback?code=abc",
            "navigation",
            new Dictionary<string, string>
            {
                ["from"] = "/login",
                ["to"] = "/auth-callback?code=abc&state=def",
                ["url"] = "/api/gamenight/link/" + LinkId,
            },
            "navigation",
            BreadcrumbLevel.Warning);

        var result = SentryScrubber.ScrubBreadcrumb(breadcrumb);

        result.Should().NotBeSameAs(breadcrumb);
        result.Message.Should().Be("navigated to /auth-callback?code=[Filtered]");
        result.Type.Should().Be("navigation");
        result.Category.Should().Be("navigation");
        result.Level.Should().Be(BreadcrumbLevel.Warning);
        result.Data!["from"].Should().Be("/login");
        result.Data["to"].Should().Be("/auth-callback?code=[Filtered]&state=[Filtered]");
        result.Data["url"].Should().Be("/api/gamenight/link/[Filtered]");
    }

    [Fact]
    public void ScrubBreadcrumb_ShouldReturnSameInstanceWhenNothingSensitive()
    {
        var breadcrumb = new Breadcrumb("loaded games", "default", new Dictionary<string, string> { ["url"] = "/api/games?page=2" });

        SentryScrubber.ScrubBreadcrumb(breadcrumb).Should().BeSameAs(breadcrumb);
    }

    [Fact]
    public void ScrubBreadcrumb_ShouldFilterMessageWhenDataIsMissing()
    {
        var breadcrumb = new Breadcrumb("GET /rsvp?linkId=abc", "http");

        var result = SentryScrubber.ScrubBreadcrumb(breadcrumb);

        result.Message.Should().Be("GET /rsvp?linkId=[Filtered]");
    }

    private static SentryTransaction CreateTransaction(string name)
    {
        var tracer = new TransactionTracer(DisabledHub.Instance, new TransactionContext(name, "http.server", isSampled: true, nameSource: TransactionNameSource.Route));
        var child = tracer.StartChild("http.client", "GET https://idp.example.com/userinfo?access_token=abc");
        child.SetData("url", "https://idp.example.com/userinfo?access_token=abc");
        child.SetData("status", 200);
        child.Finish();
        return new SentryTransaction(tracer);
    }
}
