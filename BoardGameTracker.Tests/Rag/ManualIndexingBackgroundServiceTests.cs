using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Rag.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class ManualIndexingBackgroundServiceTests
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(5);

    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _scopedProviderMock = new();
    private readonly Mock<IManualIndexingQueue> _queueMock = new();
    private readonly Mock<IManualIndexingService> _indexingServiceMock = new();

    public ManualIndexingBackgroundServiceTests()
    {
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_scopedProviderMock.Object);
        _scopedProviderMock
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

    private void VerifyScopedInfrastructure(int scopeCount)
    {
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Exactly(scopeCount));
        _scopeMock.VerifyGet(x => x.ServiceProvider, Times.Exactly(scopeCount));
        _scopeMock.Verify(x => x.Dispose(), Times.Exactly(scopeCount));
        _scopedProviderMock.Verify(x => x.GetService(typeof(IManualIndexingService)), Times.Exactly(scopeCount));
        _queueMock.Verify(x => x.DequeueAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    private void VerifyNoOtherCalls()
    {
        _scopeFactoryMock.VerifyNoOtherCalls();
        _scopeMock.VerifyNoOtherCalls();
        _scopedProviderMock.VerifyNoOtherCalls();
        _queueMock.VerifyNoOtherCalls();
        _indexingServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBackfillPendingManuals_BeforeProcessingTheQueue()
    {
        var calls = new List<string>();
        var indexed = new TaskCompletionSource();
        _indexingServiceMock
            .Setup(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                lock (calls)
                {
                    calls.Add("backfill");
                }
            })
            .Returns(Task.CompletedTask);
        _indexingServiceMock
            .Setup(x => x.IndexAsync(7, It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                lock (calls)
                {
                    calls.Add("index");
                }
                indexed.TrySetResult();
            })
            .Returns(Task.CompletedTask);
        SetupQueueToBlockAfter(7);

        await RunUntilAsync(CreateService(), indexed.Task);

        calls.Should().Equal("backfill", "index");
        _indexingServiceMock.Verify(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _indexingServiceMock.Verify(x => x.IndexAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        VerifyScopedInfrastructure(2);
        VerifyNoOtherCalls();
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

        _indexingServiceMock.Verify(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _indexingServiceMock.Verify(x => x.IndexAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        VerifyScopedInfrastructure(2);
        VerifyNoOtherCalls();
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

        _indexingServiceMock.Verify(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _indexingServiceMock.Verify(x => x.IndexAsync(42, It.IsAny<CancellationToken>()), Times.Once);
        VerifyScopedInfrastructure(2);
        VerifyNoOtherCalls();
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

        _indexingServiceMock.Verify(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _indexingServiceMock.Verify(x => x.IndexAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _indexingServiceMock.Verify(x => x.IndexAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        VerifyScopedInfrastructure(3);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldContinueProcessing_WhenIndexingThrowsOperationCanceled()
    {
        _indexingServiceMock
            .Setup(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var secondIndexed = new TaskCompletionSource();
        _indexingServiceMock
            .Setup(x => x.IndexAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        _indexingServiceMock
            .Setup(x => x.IndexAsync(2, It.IsAny<CancellationToken>()))
            .Callback(() => secondIndexed.TrySetResult())
            .Returns(Task.CompletedTask);
        SetupQueueToBlockAfter(1, 2);

        await RunUntilAsync(CreateService(), secondIndexed.Task);

        _indexingServiceMock.Verify(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _indexingServiceMock.Verify(x => x.IndexAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _indexingServiceMock.Verify(x => x.IndexAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        VerifyScopedInfrastructure(3);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateAndDisposeAScopePerIndexedManual()
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

        _indexingServiceMock.Verify(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _indexingServiceMock.Verify(x => x.IndexAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        VerifyScopedInfrastructure(3);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopCleanly_WhenServiceIsStopped()
    {
        _indexingServiceMock
            .Setup(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dequeueReached = new TaskCompletionSource();
        _queueMock
            .Setup(x => x.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) =>
            {
                dequeueReached.TrySetResult();
                return new ValueTask<int>(Task.Run(async () =>
                {
                    await Task.Delay(Timeout.Infinite, ct);
                    return 0;
                }, ct));
            });

        var service = CreateService();
        await service.StartAsync(CancellationToken.None);
        await dequeueReached.Task.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask.Should().NotBeNull();
        service.ExecuteTask!.IsCompleted.Should().BeTrue();
        service.ExecuteTask.IsFaulted.Should().BeFalse();
        _indexingServiceMock.Verify(x => x.EnqueuePendingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _queueMock.Verify(x => x.DequeueAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        _scopeMock.VerifyGet(x => x.ServiceProvider, Times.Once);
        _scopeMock.Verify(x => x.Dispose(), Times.Once);
        _scopedProviderMock.Verify(x => x.GetService(typeof(IManualIndexingService)), Times.Once);
        VerifyNoOtherCalls();
    }
}
