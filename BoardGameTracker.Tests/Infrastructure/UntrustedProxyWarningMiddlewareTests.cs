using System;
using System.Threading.Tasks;
using BoardGameTracker.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class UntrustedProxyWarningMiddlewareTests
{
    private static void VerifyWarnings(Mock<ILogger<UntrustedProxyWarningMiddleware>> loggerMock, Times times)
    {
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("TRUSTED_PROXIES")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    [Fact]
    public async Task InvokeAsync_ShouldWarnOncePerPipeline_SoEachAppInstanceWarnsOnItsOwn()
    {
        var firstLogger = new Mock<ILogger<UntrustedProxyWarningMiddleware>>();
        var secondLogger = new Mock<ILogger<UntrustedProxyWarningMiddleware>>();
        var first = new UntrustedProxyWarningMiddleware(_ => Task.CompletedTask, firstLogger.Object);
        var second = new UntrustedProxyWarningMiddleware(_ => Task.CompletedTask, secondLogger.Object);
        var forwarded = new DefaultHttpContext();
        forwarded.Request.Headers["X-Forwarded-For"] = "203.0.113.9";

        await first.InvokeAsync(forwarded);
        await second.InvokeAsync(forwarded);
        await second.InvokeAsync(forwarded);

        VerifyWarnings(firstLogger, Times.Once());
        VerifyWarnings(secondLogger, Times.Once());
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

        var forwarded = new DefaultHttpContext();
        forwarded.Request.Headers["X-Forwarded-For"] = "203.0.113.9";
        var plain = new DefaultHttpContext();

        await middleware.InvokeAsync(forwarded);
        await middleware.InvokeAsync(forwarded);
        await middleware.InvokeAsync(plain);

        calls.Should().Be(3);
        VerifyWarnings(loggerMock, Times.Once());
    }
}
