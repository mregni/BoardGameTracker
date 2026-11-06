using System.Net;
using System.Threading.Tasks;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class UntrustedProxyWarningMiddlewareTests
{
    private const string Warning = "Requests arrive with an X-Forwarded-For header from 10.0.0.2, but TRUSTED_PROXIES is empty. Client IPs, HTTPS detection and the login rate limit are based on the proxy address. Set TRUSTED_PROXIES to the proxy IP or network (for example 172.16.0.0/12 for Docker)";

    private static DefaultHttpContext ForwardedContext()
    {
        var forwarded = new DefaultHttpContext();
        forwarded.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.2");
        forwarded.Request.Headers["X-Forwarded-For"] = "203.0.113.9";
        return forwarded;
    }

    [Fact]
    public async Task InvokeAsync_ShouldWarnOncePerPipeline_SoEachAppInstanceWarnsOnItsOwn()
    {
        var firstLogger = new Mock<ILogger<UntrustedProxyWarningMiddleware>>();
        var secondLogger = new Mock<ILogger<UntrustedProxyWarningMiddleware>>();
        var first = new UntrustedProxyWarningMiddleware(_ => Task.CompletedTask, firstLogger.Object);
        var second = new UntrustedProxyWarningMiddleware(_ => Task.CompletedTask, secondLogger.Object);
        var forwarded = ForwardedContext();

        await first.InvokeAsync(forwarded);
        await second.InvokeAsync(forwarded);
        await second.InvokeAsync(forwarded);

        firstLogger.VerifyLog(LogLevel.Warning, Warning);
        secondLogger.VerifyLog(LogLevel.Warning, Warning);
        firstLogger.VerifyNoOtherCalls();
        secondLogger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvokeAsync_ShouldWarnOnceAndAlwaysCallNext_WhenForwardedHeadersArrive()
    {
        var loggerMock = new Mock<ILogger<UntrustedProxyWarningMiddleware>>();
        var calls = 0;
        var middleware = new UntrustedProxyWarningMiddleware(_ =>
        {
            calls++;
            return Task.CompletedTask;
        }, loggerMock.Object);

        var forwarded = ForwardedContext();
        var plain = new DefaultHttpContext();

        await middleware.InvokeAsync(forwarded);
        await middleware.InvokeAsync(forwarded);
        await middleware.InvokeAsync(plain);

        calls.Should().Be(3);
        loggerMock.VerifyLog(LogLevel.Warning, Warning);
        loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotWarn_WhenNoForwardedHeaderArrives()
    {
        var loggerMock = new Mock<ILogger<UntrustedProxyWarningMiddleware>>();
        var calls = 0;
        var middleware = new UntrustedProxyWarningMiddleware(_ =>
        {
            calls++;
            return Task.CompletedTask;
        }, loggerMock.Object);

        await middleware.InvokeAsync(new DefaultHttpContext());

        calls.Should().Be(1);
        loggerMock.VerifyNoOtherCalls();
    }
}
