using System.Threading.Channels;
using BoardGameTracker.Core.Rag.Interfaces;

namespace BoardGameTracker.Core.Rag;

public class ManualIndexingQueue : IManualIndexingQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    public void Enqueue(int manualId)
    {
        _channel.Writer.TryWrite(manualId);
    }

    public ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
