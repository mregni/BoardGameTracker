using System.Net;
using BoardGameTracker.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class OidcTransportPolicyTests
{
    private static HttpRequest Request(string scheme, string host)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        return context.Request;
    }

    [Theory]
    [InlineData("https", "games.example.com", true)]
    [InlineData("https", "192.168.1.20:5444", true)]
    [InlineData("http", "games.example.com", false)]
    [InlineData("http", "192.168.1.20:5444", false)]
    [InlineData("http", "10.0.0.5", false)]
    [InlineData("http", "localhost", true)]
    [InlineData("http", "localhost:5444", true)]
    [InlineData("http", "LOCALHOST:5444", true)]
    [InlineData("http", "app.localhost:5444", true)]
    [InlineData("http", "127.0.0.1:5444", true)]
    [InlineData("http", "127.10.0.1", true)]
    [InlineData("http", "[::1]:5444", true)]
    [InlineData("http", "[::1]", true)]
    [InlineData("http", "localhost.example.com", false)]
    [InlineData("http", "notlocalhost", false)]
    [InlineData("http", "", false)]
    public void IsAllowed_ShouldOnlyAllowHttpsOrALoopbackHost(string scheme, string host, bool expected)
    {
        OidcTransportPolicy.IsAllowed(Request(scheme, host)).Should().Be(expected);
    }

    [Theory]
    [InlineData("https://games.example.com", true)]
    [InlineData("https://192.168.1.20:5444", true)]
    [InlineData("http://games.example.com", false)]
    [InlineData("http://192.168.1.20:5444", false)]
    [InlineData("http://localhost:5444", true)]
    [InlineData("http://bgt.localhost", true)]
    [InlineData("http://127.0.0.1:5444", true)]
    [InlineData("http://[::1]:5444", true)]
    [InlineData("ftp://localhost", false)]
    [InlineData("games.example.com", false)]
    [InlineData("", false)]
    public void IsAllowed_ShouldJudgeAPublicBaseUrl(string baseUrl, bool expected)
    {
        OidcTransportPolicy.IsAllowed(baseUrl).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("localhost.", true)]
    [InlineData("[::1]", true)]
    [InlineData("::1", true)]
    [InlineData("192.168.1.20", false)]
    public void IsLoopbackHost_ShouldRecogniseLoopbackNames(string? host, bool expected)
    {
        OidcTransportPolicy.IsLoopbackHost(host).Should().Be(expected);
    }

    [Theory]
    [InlineData("172.18.0.2", "https", true)]
    [InlineData("172.18.0.2", "http", false)]
    [InlineData("203.0.113.9", "https", false)]
    public async Task IsAllowed_ShouldHonourTheForwardedProto_OnlyFromATrustedProxy(string remoteIp, string forwardedProto, bool expected)
    {
        var options = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownProxies.Add(IPAddress.Parse("172.18.0.2"));
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("games.example.com");
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        context.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.50";
        var allowed = false;
        var middleware = new ForwardedHeadersMiddleware(ctx =>
        {
            allowed = OidcTransportPolicy.IsAllowed(ctx.Request);
            return Task.CompletedTask;
        }, NullLoggerFactory.Instance, Options.Create(options));

        await middleware.Invoke(context);

        allowed.Should().Be(expected);
    }
}
