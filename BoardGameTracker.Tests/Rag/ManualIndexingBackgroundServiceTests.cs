using System;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Rag.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class ManualIndexingBackgroundServiceTests
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(5);

    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IManualIndexingQueue> _queueMock = new();
    private readonly Mock<IManualIndexingService> _indexingServiceMock = new();

    public ManualIndexingBackgroundServiceTests()
    {
        var scopeMock = new Mock<IServiceScope>();
        var scopedProviderMock = new Mock<IServiceProvider>();

        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(x => x.ServiceProvider).Returns(scopedProviderMock.Object);
        scopedProviderMock
            .Setup(x => x.GetService(typeof(IManualIndexingService)))
            .Returns(_indexingServiceMock.Object);
    }

    private ManualIndexingBackgroundService CreateService() =>
        new(_scopeFactoryMock.Object, _queueMock.Object, Mock.Of<ILogger<ManualIndexingBackgroundService>>());

    private void SetupQueueToBlockAfter(params int[] manualIds)
    {
        var call = 0;
        _queueMock
            .Setup(x => x.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) =>
            {
                var index = Interlocked.Increment(ref call) - 1;
                if (index < manualIds.Length)
                {
                    return new ValueTask<int>(manualIds[index]);
                }
                return new ValueTask<int>(Task.Run(async () =>
                {
                    await Task.Delay(Timeout.Infinite, ct);
                    return 0;
                }, ct));
            });
    }

    private static async Task RunUntilAsync(ManualIndexingBackgroundService service, Task signal)
    {
        await service.StartAsync(CancellationToken.None);
        try
        {
            await signal.WaitAsync(SignalTimeout);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBackfillPendingManuals_BeforeProcessingTheQueue()
    {
        var backfilled = new TaskCompletionSource();
        _indexingServiceMock
            .Setup(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()))
            .Callback(() => backfilled.TrySetResult())
            .Returns(Task.CompletedTask);
        SetupQueueToBlockAfter();

        await RunUntilAsync(CreateService(), backfilled.Task);

        _indexingServiceMock.Verify(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepRunning_WhenBackfillThrows()
    {
        _indexingServiceMock
            .Setup(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("backfill boom"));

        var indexed = new TaskCompletionSource();
        _indexingServiceMock
            .Setup(x => x.IndexAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() => indexed.TrySetResult())
            .Returns(Task.CompletedTask);
        SetupQueueToBlockAfter(7);

        await RunUntilAsync(CreateService(), indexed.Task);

        _indexingServiceMock.Verify(x => x.IndexAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIndexDequeuedManual()
    {
        _indexingServiceMock
            .Setup(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var indexed = new TaskCompletionSource();
        _indexingServiceMock
            .Setup(x => x.IndexAsync(42, It.IsAny<CancellationToken>()))
            .Callback(() => indexed.TrySetResult())
            .Returns(Task.CompletedTask);
        SetupQueueToBlockAfter(42);

        await RunUntilAsync(CreateService(), indexed.Task);

        _indexingServiceMock.Verify(x => x.IndexAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldContinueProcessing_WhenIndexingOneManualThrows()
    {
        _indexingServiceMock
            .Setup(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var secondIndexed = new TaskCompletionSource();
        _indexingServiceMock
            .Setup(x => x.IndexAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("indexing boom"));
        _indexingServiceMock
            .Setup(x => x.IndexAsync(2, It.IsAny<CancellationToken>()))
            .Callback(() => secondIndexed.TrySetResult())
            .Returns(Task.CompletedTask);
        SetupQueueToBlockAfter(1, 2);

        await RunUntilAsync(CreateService(), secondIndexed.Task);

        _indexingServiceMock.Verify(x => x.IndexAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _indexingServiceMock.Verify(x => x.IndexAsync(2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateAScopePerIndexedManual()
    {
        _indexingServiceMock
            .Setup(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var secondIndexed = new TaskCompletionSource();
        var indexed = 0;
        _indexingServiceMock
            .Setup(x => x.IndexAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                if (Interlocked.Increment(ref indexed) >= 2)
                {
                    secondIndexed.TrySetResult();
                }
            })
            .Returns(Task.CompletedTask);
        SetupQueueToBlockAfter(1, 2);

        await RunUntilAsync(CreateService(), secondIndexed.Task);

        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.AtLeast(3));
    }
}
