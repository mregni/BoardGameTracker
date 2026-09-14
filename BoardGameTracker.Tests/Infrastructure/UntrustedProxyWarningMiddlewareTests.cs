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
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("TRUSTED_PROXIES")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
