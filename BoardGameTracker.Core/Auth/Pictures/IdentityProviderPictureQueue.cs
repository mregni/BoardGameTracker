using System.Threading.Channels;
using BoardGameTracker.Core.Auth.Interfaces;

namespace BoardGameTracker.Core.Auth.Pictures;

public class IdentityProviderPictureQueue : IIdentityProviderPictureQueue
{
    public const int Capacity = 64;

    private readonly Channel<IdentityProviderPictureImport> _channel = Channel.CreateBounded<IdentityProviderPictureImport>(new BoundedChannelOptions(Capacity)
    {
        SingleReader = true,
    });

    public bool Enqueue(IdentityProviderPictureImport import)
    {
        return _channel.Writer.TryWrite(import);
    }

    public ValueTask<IdentityProviderPictureImport> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
