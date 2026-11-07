using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Email;
using BoardGameTracker.Core.Email.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
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

    private void VerifyNoOtherCalls()
    {
        _scopeFactoryMock.VerifyNoOtherCalls();
        _scopeMock.VerifyNoOtherCalls();
        _serviceProviderMock.VerifyNoOtherCalls();
        _emailServiceMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    private void VerifyScopes(int count)
    {
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Exactly(count));
        _scopeMock.Verify(x => x.ServiceProvider, Times.Exactly(count));
        _serviceProviderMock.Verify(x => x.GetService(typeof(IEmailService)), Times.Exactly(count));
        _scopeMock.Verify(x => x.Dispose(), Times.Exactly(count));
    }

    [Fact]
    public async Task Queue_ShouldSendTheMailInItsOwnScope()
    {
        var disposed = new TaskCompletionSource();
        _scopeMock.Setup(x => x.Dispose()).Callback(() => disposed.TrySetResult());
        _emailServiceMock
            .Setup(x => x.SendAsync("to@test.com", "Subject", "<p>Body</p>", null, CancellationToken.None))
            .Returns(Task.CompletedTask);

        _sender.Queue("to@test.com", "Subject", "<p>Body</p>");

        await disposed.Task.WaitAsync(SignalTimeout);
        _emailServiceMock.Verify(x => x.SendAsync("to@test.com", "Subject", "<p>Body</p>", null, CancellationToken.None), Times.Once);
        VerifyScopes(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Queue_ShouldLogAndSwallowAFailedSend()
    {
        var logged = new TaskCompletionSource();
        var failure = new InvalidOperationException("smtp down");
        _emailServiceMock
            .Setup(x => x.SendAsync("to@test.com", "Subject", "<p>Body</p>", null, CancellationToken.None))
            .ThrowsAsync(failure);
        _loggerMock
            .Setup(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                failure,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => logged.TrySetResult());

        _sender.Queue("to@test.com", "Subject", "<p>Body</p>");

        await logged.Task.WaitAsync(SignalTimeout);
        _emailServiceMock.Verify(x => x.SendAsync("to@test.com", "Subject", "<p>Body</p>", null, CancellationToken.None), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "Failed to send a queued email with subject Subject", failure);
        VerifyScopes(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Queue_ShouldSendOneMailAtATime()
    {
        const int mails = 5;
        var active = 0;
        var maxActive = 0;
        var disposed = 0;
        var gate = new object();
        var allDone = new TaskCompletionSource();
        _scopeMock
            .Setup(x => x.Dispose())
            .Callback(() =>
            {
                if (Interlocked.Increment(ref disposed) == mails)
                {
                    allDone.TrySetResult();
                }
            });

        var recipients = Enumerable.Range(0, mails).Select(i => $"to{i}@test.com").ToList();
        foreach (var recipient in recipients)
        {
            _emailServiceMock
                .Setup(x => x.SendAsync(recipient, "Subject", "<p>Body</p>", null, CancellationToken.None))
                .Returns(async () =>
                {
                    var now = Interlocked.Increment(ref active);
                    lock (gate)
                    {
                        maxActive = Math.Max(maxActive, now);
                    }
                    await Task.Delay(20);
                    Interlocked.Decrement(ref active);
                });
            _sender.Queue(recipient, "Subject", "<p>Body</p>");
        }

        await allDone.Task.WaitAsync(SignalTimeout);
        maxActive.Should().Be(1);
        foreach (var recipient in recipients)
        {
            _emailServiceMock.Verify(x => x.SendAsync(recipient, "Subject", "<p>Body</p>", null, CancellationToken.None), Times.Once);
        }

        VerifyScopes(mails);
        VerifyNoOtherCalls();
    }
}
