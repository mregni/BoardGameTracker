using System;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Email;
using BoardGameTracker.Core.Email.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Email;

public class BackgroundEmailSenderTests
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(5);

    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ILogger<BackgroundEmailSender>> _loggerMock = new();
    private readonly BackgroundEmailSender _sender;

    public BackgroundEmailSenderTests()
    {
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IEmailService))).Returns(_emailServiceMock.Object);
        _sender = new BackgroundEmailSender(_scopeFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Queue_ShouldSendTheMailInItsOwnScope()
    {
        var disposed = new TaskCompletionSource();
        _scopeMock.Setup(x => x.Dispose()).Callback(() => disposed.TrySetResult());
        _emailServiceMock
            .Setup(x => x.SendAsync("to@test.com", "Subject", "<p>Body</p>", null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sender.Queue("to@test.com", "Subject", "<p>Body</p>");

        await disposed.Task.WaitAsync(SignalTimeout);
        _emailServiceMock.Verify(x => x.SendAsync("to@test.com", "Subject", "<p>Body</p>", null, It.IsAny<CancellationToken>()), Times.Once);
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task Queue_ShouldLogAndSwallowAFailedSend()
    {
        var logged = new TaskCompletionSource();
        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));
        _loggerMock
            .Setup(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => logged.TrySetResult());

        _sender.Queue("to@test.com", "Subject", "<p>Body</p>");

        await logged.Task.WaitAsync(SignalTimeout);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Subject")),
                It.Is<InvalidOperationException>(e => e.Message == "smtp down"),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
