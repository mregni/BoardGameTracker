namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IIdentityProviderPictureDownloader
{
    Task<byte[]?> DownloadAsync(string pictureUrl, string authority, CancellationToken cancellationToken);
}
