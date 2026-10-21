using System.Net;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Common;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth.Pictures;

public class IdentityProviderPictureDownloader : IIdentityProviderPictureDownloader
{
    public const string HttpClientName = "oidc-picture";
    public const long MaxBytes = 2 * 1024 * 1024;
    public const int MaxRedirects = 3;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHostAddressResolver _resolver;
    private readonly ILogger<IdentityProviderPictureDownloader> _logger;

    public IdentityProviderPictureDownloader(IHttpClientFactory httpClientFactory, IHostAddressResolver resolver, ILogger<IdentityProviderPictureDownloader> logger)
    {
        _httpClientFactory = httpClientFactory;
        _resolver = resolver;
        _logger = logger;
    }

    public async Task<byte[]?> DownloadAsync(string pictureUrl, string authority, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri) || !SecureUrlPolicy.IsAcceptable(authority))
        {
            _logger.LogWarning("Skipped the identity provider picture because the provider authority is not a valid address");
            return null;
        }

        var target = IdentityProviderPicturePolicy.Validate(pictureUrl, authorityUri);
        if (target == null)
        {
            _logger.LogWarning("Skipped the identity provider picture because its address is not an allowed https address");
            return null;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            return await FetchAsync(target, authorityUri, timeout.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Downloading the identity provider picture from {Host} took longer than {Timeout} seconds", target.Host, Timeout.TotalSeconds);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not download the identity provider picture from {Host}", target.Host);
            return null;
        }
    }

    private async Task<byte[]?> FetchAsync(Uri target, Uri authority, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var originalHost = target.IdnHost;

        for (var hop = 0; hop <= MaxRedirects; hop++)
        {
            var addresses = await ResolveAllowedAsync(target, authority, cancellationToken);
            if (addresses == null)
            {
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, target);
            request.Options.Set(PinnedAddressConnector.PinnedAddresses, addresses);
            request.Headers.Accept.ParseAdd("image/*");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (IsRedirect(response.StatusCode))
            {
                var next = NextHop(target, response, authority);
                if (next == null || !string.Equals(next.IdnHost, originalHost, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Refused a redirect of the identity provider picture away from {Host}", originalHost);
                    return null;
                }

                target = next;
                continue;
            }

            return await ReadImageAsync(response, target, cancellationToken);
        }

        _logger.LogWarning("The identity provider picture at {Host} redirected more than {Max} times", originalHost, MaxRedirects);
        return null;
    }

    private async Task<IPAddress[]?> ResolveAllowedAsync(Uri target, Uri authority, CancellationToken cancellationToken)
    {
        IPAddress[] addresses;
        try
        {
            addresses = await _resolver.ResolveAsync(target.IdnHost, cancellationToken);
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            _logger.LogWarning(ex, "Could not resolve {Host} for the identity provider picture", target.Host);
            return null;
        }

        var isAuthorityHost = IdentityProviderPicturePolicy.IsAuthorityHost(target, authority);
        var authorityIsLoopback = SecureUrlPolicy.IsLoopbackHost(authority.IdnHost);
        if (addresses.Length == 0 || !addresses.All(a => IdentityProviderPicturePolicy.IsAllowedAddress(a, isAuthorityHost, authorityIsLoopback)))
        {
            _logger.LogWarning("Refused the identity provider picture because {Host} resolves to an internal address", target.Host);
            return null;
        }

        return addresses;
    }

    private static Uri? NextHop(Uri current, HttpResponseMessage response, Uri authority)
    {
        var location = response.Headers.Location;
        if (location == null)
        {
            return null;
        }

        var absolute = location.IsAbsoluteUri ? location : new Uri(current, location);
        return IdentityProviderPicturePolicy.Validate(absolute.AbsoluteUri, authority);
    }

    private async Task<byte[]?> ReadImageAsync(HttpResponseMessage response, Uri target, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("The identity provider picture at {Host} returned {StatusCode}", target.Host, (int)response.StatusCode);
            return null;
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType == null || !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("The identity provider picture at {Host} is not an image", target.Host);
            return null;
        }

        if (response.Content.Headers.ContentLength is > MaxBytes)
        {
            _logger.LogWarning("The identity provider picture at {Host} is larger than {Max} bytes", target.Host, MaxBytes);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxBytes)
            {
                _logger.LogWarning("The identity provider picture at {Host} is larger than {Max} bytes", target.Host, MaxBytes);
                return null;
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return buffer.ToArray();
    }

    private static bool IsRedirect(HttpStatusCode status)
    {
        return status is HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
    }
}
