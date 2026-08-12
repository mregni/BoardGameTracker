using System;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Rag;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class ManualIndexingQueueTests
{
    private readonly ManualIndexingQueue _queue = new();

    [Fact]
    public async Task DequeueAsync_ShouldReturnEnqueuedItem()
    {
        _queue.Enqueue(42);

        var result = await _queue.DequeueAsync(CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task DequeueAsync_ShouldPreserveEnqueueOrder()
    {
        _queue.Enqueue(1);
        _queue.Enqueue(2);
        _queue.Enqueue(3);

        var first = await _queue.DequeueAsync(CancellationToken.None);
        var second = await _queue.DequeueAsync(CancellationToken.None);
        var third = await _queue.DequeueAsync(CancellationToken.None);

        first.Should().Be(1);
        second.Should().Be(2);
        third.Should().Be(3);
    }

    [Fact]
    public async Task DequeueAsync_ShouldKeepDuplicateIds_WhenSameManualIsEnqueuedTwice()
    {
        _queue.Enqueue(7);
        _queue.Enqueue(7);

        (await _queue.DequeueAsync(CancellationToken.None)).Should().Be(7);
        (await _queue.DequeueAsync(CancellationToken.None)).Should().Be(7);
    }

    [Fact]
    public async Task DequeueAsync_ShouldWaitForItem_WhenQueueIsEmpty()
    {
        var dequeueTask = _queue.DequeueAsync(CancellationToken.None).AsTask();

        dequeueTask.IsCompleted.Should().BeFalse();

        _queue.Enqueue(9);

        var result = await dequeueTask.WaitAsync(TimeSpan.FromSeconds(5));
        result.Should().Be(9);
    }

    [Fact]
    public async Task DequeueAsync_ShouldThrowOperationCanceled_WhenTokenIsCancelledWhileWaiting()
    {
        using var cts = new CancellationTokenSource();
        var dequeueTask = _queue.DequeueAsync(cts.Token).AsTask();

        await cts.CancelAsync();

        await FluentActions.Awaiting(() => dequeueTask).Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DequeueAsync_ShouldThrowOperationCanceled_WhenTokenIsAlreadyCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await FluentActions.Awaiting(() => _queue.DequeueAsync(cts.Token).AsTask())
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
