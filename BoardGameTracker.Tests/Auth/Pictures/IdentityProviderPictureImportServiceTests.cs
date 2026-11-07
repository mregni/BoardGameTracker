using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Auth.Pictures;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth.Pictures;

public class IdentityProviderPictureImportServiceTests
{
    private const string FailureMessage = "Could not import the identity provider picture of user user-1";
    private static readonly IdentityProviderPictureImport Import = new("user-1", "https://idp.example.com/avatars/jane.png", "https://idp.example.com");

    private readonly Mock<IServiceProvider> _servicesMock = new();
    private readonly Mock<IUserProfileImageService> _imagesMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<ILogger<IdentityProviderPictureImportService>> _loggerMock = new();
    private readonly IdentityProviderPictureQueue _queue = new();
    private readonly IdentityProviderPictureImportService _worker;

    public IdentityProviderPictureImportServiceTests()
    {
        _servicesMock.Setup(x => x.GetService(typeof(IUserProfileImageService))).Returns(_imagesMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_servicesMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _worker = new IdentityProviderPictureImportService(_scopeFactoryMock.Object, _queue, _loggerMock.Object);
    }

    [Fact]
    public async Task ImportAsync_ShouldImportInItsOwnScope_WithTheStoppingToken()
    {
        using var stopping = new CancellationTokenSource();

        await _worker.ImportAsync(Import, stopping.Token);

        _imagesMock.Verify(x => x.ImportFromIdentityProviderAsync(Import, stopping.Token), Times.Once);
        VerifyScopeUsed();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportAsync_ShouldSwallowFailures_SoTheQueueKeepsRunning()
    {
        using var stopping = new CancellationTokenSource();
        var failure = new InvalidOperationException("boom");
        _imagesMock.Setup(x => x.ImportFromIdentityProviderAsync(Import, stopping.Token)).ThrowsAsync(failure);

        await _worker.ImportAsync(Import, stopping.Token);

        _imagesMock.Verify(x => x.ImportFromIdentityProviderAsync(Import, stopping.Token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, FailureMessage, failure);
        VerifyScopeUsed();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportAsync_ShouldOnlyLogAtDebug_WhenTheImportStopsDuringShutdown()
    {
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();
        var cancelled = new OperationCanceledException(stopping.Token);
        _imagesMock.Setup(x => x.ImportFromIdentityProviderAsync(Import, stopping.Token)).ThrowsAsync(cancelled);

        await _worker.ImportAsync(Import, stopping.Token);

        _imagesMock.Verify(x => x.ImportFromIdentityProviderAsync(Import, stopping.Token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Stopped importing the identity provider picture of user user-1 during shutdown", cancelled);
        VerifyScopeUsed();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportAsync_ShouldWarn_WhenTheImportIsCancelledWithoutAShutdown()
    {
        using var stopping = new CancellationTokenSource();
        var cancelled = new OperationCanceledException();
        _imagesMock.Setup(x => x.ImportFromIdentityProviderAsync(Import, stopping.Token)).ThrowsAsync(cancelled);

        await _worker.ImportAsync(Import, stopping.Token);

        _imagesMock.Verify(x => x.ImportFromIdentityProviderAsync(Import, stopping.Token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, FailureMessage, cancelled);
        VerifyScopeUsed();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportAsync_ShouldSwallowAScopeFailure()
    {
        var failure = new ObjectDisposedException("scope factory");
        _scopeFactoryMock.Setup(x => x.CreateScope()).Throws(failure);

        await _worker.ImportAsync(Import, TestContext.Current.CancellationToken);

        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, FailureMessage, failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldImportQueuedPictures_AndKeepGoingAfterAFailure()
    {
        var second = Import with { UserId = "user-2" };
        var failure = new InvalidOperationException("boom");
        var imported = new TaskCompletionSource();
        _imagesMock.Setup(x => x.ImportFromIdentityProviderAsync(Import, It.Is<CancellationToken>(t => t.CanBeCanceled))).ThrowsAsync(failure);
        _imagesMock.Setup(x => x.ImportFromIdentityProviderAsync(second, It.Is<CancellationToken>(t => t.CanBeCanceled)))
            .Callback(imported.SetResult)
            .Returns(Task.CompletedTask);

        await _worker.StartAsync(TestContext.Current.CancellationToken);
        _queue.Enqueue(Import).Should().BeTrue();
        _queue.Enqueue(second).Should().BeTrue();
        await imported.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await _worker.StopAsync(TestContext.Current.CancellationToken);
        _worker.Dispose();

        _imagesMock.Verify(x => x.ImportFromIdentityProviderAsync(Import, It.Is<CancellationToken>(t => t.CanBeCanceled)), Times.Once);
        _imagesMock.Verify(x => x.ImportFromIdentityProviderAsync(second, It.Is<CancellationToken>(t => t.CanBeCanceled)), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, FailureMessage, failure);
        VerifyScopeUsed(2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStop_WhenTheHostStopsWhileTheQueueIsEmpty()
    {
        var imported = new TaskCompletionSource();
        _imagesMock.Setup(x => x.ImportFromIdentityProviderAsync(Import, It.Is<CancellationToken>(t => t.CanBeCanceled)))
            .Callback(imported.SetResult)
            .Returns(Task.CompletedTask);
        await _worker.StartAsync(TestContext.Current.CancellationToken);
        _queue.Enqueue(Import).Should().BeTrue();
        await imported.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await _worker.StopAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await _worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        _worker.Dispose();

        _worker.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue();
        _imagesMock.Verify(x => x.ImportFromIdentityProviderAsync(Import, It.Is<CancellationToken>(t => t.CanBeCanceled)), Times.Once);
        VerifyScopeUsed();
        VerifyNoOtherCalls();
    }

    private void VerifyScopeUsed(int times = 1)
    {
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Exactly(times));
        _scopeMock.Verify(x => x.ServiceProvider, Times.Exactly(times));
        _scopeMock.Verify(x => x.Dispose(), Times.Exactly(times));
        _servicesMock.Verify(x => x.GetService(typeof(IUserProfileImageService)), Times.Exactly(times));
    }

    private void VerifyNoOtherCalls()
    {
        _servicesMock.VerifyNoOtherCalls();
        _imagesMock.VerifyNoOtherCalls();
        _scopeMock.VerifyNoOtherCalls();
        _scopeFactoryMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }
}
