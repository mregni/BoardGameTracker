using BoardGameTracker.Core.Auth.Pictures;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IIdentityProviderPictureQueue
{
    bool Enqueue(IdentityProviderPictureImport import);
    ValueTask<IdentityProviderPictureImport> DequeueAsync(CancellationToken cancellationToken);
}
