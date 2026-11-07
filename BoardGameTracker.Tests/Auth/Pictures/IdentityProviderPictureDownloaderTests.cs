using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using BoardGameTracker.Core.Auth.Pictures;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth.Pictures;

public class IdentityProviderPictureDownloaderTests : IDisposable
{
    private const string Authority = "https://idp.example.com/realms/bgt";
    private const string CdnHost = "cdn.example.com";
    private const string CdnPicture = "https://cdn.example.com/a.png";
    private static readonly IPAddress PublicAddress = IPAddress.Parse("93.184.216.34");

    private readonly StubHandler _handler = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IHostAddressResolver> _resolverMock = new();
    private readonly Mock<ILogger<IdentityProviderPictureDownloader>> _loggerMock = new();
    private readonly IdentityProviderPictureDownloader _downloader;

    public IdentityProviderPictureDownloaderTests()
    {
        _httpClientFactoryMock.Setup(x => x.CreateClient(IdentityProviderPictureDownloader.HttpClientName)).Returns(() => new HttpClient(_handler, disposeHandler: false));
        _downloader = new IdentityProviderPictureDownloader(_httpClientFactoryMock.Object, _resolverMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _handler.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Limits_ShouldKeepTheirValues()
    {
        IdentityProviderPictureDownloader.HttpClientName.Should().Be("oidc-picture");
        IdentityProviderPictureDownloader.MaxBytes.Should().Be(2097152);
        IdentityProviderPictureDownloader.MaxRedirects.Should().Be(3);
        IdentityProviderPictureDownloader.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnTheImage_AndPinTheVettedAddresses()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ => Image([1, 2, 3]);

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().Equal(1, 2, 3);
        var request = _handler.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Get);
        request.Uri.Should().Be(new Uri(CdnPicture));
        request.Accept.Should().Be("image/*");
        request.Pinned.Should().Equal(PublicAddress);
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldResolveThePunycodeHost()
    {
        ResolveTo("xn--bcher-kva.example.com", PublicAddress);
        _handler.Respond = _ => Image([4]);

        var result = await _downloader.DownloadAsync("https://bücher.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().Equal(4);
        _handler.Requests.Should().ContainSingle().Which.Uri.Should().Be(new Uri("https://xn--bcher-kva.example.com/a.png"));
        VerifyFetched("xn--bcher-kva.example.com");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("http://cdn.example.com/a.png")]
    [InlineData("https://127.0.0.1/a.png")]
    public async Task DownloadAsync_ShouldNotSendARequest_ForARefusedAddress(string url)
    {
        var result = await _downloader.DownloadAsync(url, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Warning, "Skipped the identity provider picture because its address is not an allowed https address");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("data:image/svg+xml;base64,c2VjcmV0")]
    [InlineData("ftp://cdn.example.com/c2VjcmV0.png")]
    [InlineData("/avatars/c2VjcmV0.png")]
    [InlineData("not a web address")]
    public async Task DownloadAsync_ShouldOnlyLogAtDebug_ForAPictureThatIsNotAWebAddress(string url)
    {
        var expected = Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            ? $"Skipped the identity provider picture because it uses the {parsed.Scheme} scheme"
            : "Skipped the identity provider picture because it is not an absolute address";

        var result = await _downloader.DownloadAsync(url, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Debug, expected);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldFetchAPlainHttpPictureOfTheAuthorityHost_OverHttps()
    {
        ResolveTo("idp.example.com", PublicAddress);
        _handler.Respond = _ => Image([5]);

        var result = await _downloader.DownloadAsync("http://idp.example.com/media/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().Equal(5);
        _handler.Requests.Should().ContainSingle().Which.Uri.Should().Be(new Uri("https://idp.example.com/media/a.png"));
        VerifyFetched("idp.example.com");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("not an authority")]
    [InlineData("http://idp.example.com/realms/bgt")]
    public async Task DownloadAsync_ShouldRefuse_WhenTheAuthorityIsNotAValidAddress(string authority)
    {
        var result = await _downloader.DownloadAsync(CdnPicture, authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Warning, "Skipped the identity provider picture because the provider authority is not a valid address");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("10.1.2.3")]
    [InlineData("169.254.169.254")]
    [InlineData("127.0.0.1")]
    public async Task DownloadAsync_ShouldRefuseAHost_ThatResolvesToAnInternalAddress(string address)
    {
        ResolveTo(CdnHost, PublicAddress, IPAddress.Parse(address));

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Warning, "Refused the identity provider picture because cdn.example.com resolves to an internal address");
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuseAHost_ThatResolvesToNothing()
    {
        ResolveTo(CdnHost);

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Warning, "Refused the identity provider picture because cdn.example.com resolves to an internal address");
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldAcceptAPrivateAddress_OfTheAuthorityHost()
    {
        var address = IPAddress.Parse("192.168.1.20");
        ResolveTo("idp.example.com", address);
        _handler.Respond = _ => Image([7]);

        var result = await _downloader.DownloadAsync("https://idp.example.com/media/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().Equal(7);
        _handler.Requests.Should().ContainSingle().Which.Pinned.Should().Equal(address);
        VerifyFetched("idp.example.com");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldAcceptALoopbackAddress_OfALoopbackAuthority()
    {
        ResolveTo("localhost", IPAddress.Loopback);
        _handler.Respond = _ => Image([8]);

        var result = await _downloader.DownloadAsync("http://localhost:9000/a.png", "http://127.0.0.1:8080/realms/bgt", TestContext.Current.CancellationToken);

        result.Should().Equal(8);
        _handler.Requests.Should().ContainSingle().Which.Uri.Should().Be(new Uri("http://localhost:9000/a.png"));
        VerifyFetched("localhost");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_WhenTheHostDoesNotResolve()
    {
        var failure = new SocketException((int)SocketError.HostNotFound);
        _resolverMock.Setup(x => x.ResolveAsync(CdnHost, It.Is<CancellationToken>(t => t.CanBeCanceled))).ThrowsAsync(failure);

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Warning, "Could not resolve cdn.example.com for the identity provider picture", failure);
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_ARedirectToAnotherHost()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ => Redirect("https://evil.example.net/a.png");

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().ContainSingle();
        _loggerMock.VerifyLog(LogLevel.Warning, "Refused a redirect of the identity provider picture away from cdn.example.com");
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_ARedirectToAnInsecureAddress()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ => Redirect("http://cdn.example.com/a.png");

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().ContainSingle();
        _loggerMock.VerifyLog(LogLevel.Warning, "Refused a redirect of the identity provider picture away from cdn.example.com");
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_ARedirectWithoutALocation()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.Found);

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _loggerMock.VerifyLog(LogLevel.Warning, "Refused a redirect of the identity provider picture away from cdn.example.com");
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.MovedPermanently)]
    [InlineData(HttpStatusCode.Found)]
    [InlineData(HttpStatusCode.SeeOther)]
    [InlineData(HttpStatusCode.TemporaryRedirect)]
    [InlineData(HttpStatusCode.PermanentRedirect)]
    public async Task DownloadAsync_ShouldFollowARelativeRedirect_OnTheSameHost(HttpStatusCode status)
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = request => request.RequestUri!.AbsolutePath == "/a.png" ? Redirect("/b.png", status) : Image([9]);

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().Equal(9);
        _handler.Requests.Select(r => r.Uri.AbsolutePath).Should().Equal("/a.png", "/b.png");
        VerifyFetched(CdnHost, 2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldVetTheAddressAgain_OnEveryRedirectHop()
    {
        _resolverMock.SetupSequence(x => x.ResolveAsync(CdnHost, It.Is<CancellationToken>(t => t.CanBeCanceled)))
            .ReturnsAsync([PublicAddress])
            .ReturnsAsync([IPAddress.Parse("10.1.2.3")]);
        _handler.Respond = _ => Redirect("/b.png");

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().ContainSingle();
        _loggerMock.VerifyLog(LogLevel.Warning, "Refused the identity provider picture because cdn.example.com resolves to an internal address");
        VerifyFetched(CdnHost, 2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldFollowTheLastAllowedRedirect()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = request => request.RequestUri!.AbsolutePath == "/3.png"
            ? Image([6])
            : Redirect(request.RequestUri.AbsolutePath switch { "/a.png" => "/1.png", "/1.png" => "/2.png", _ => "/3.png" });

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().Equal(6);
        _handler.Requests.Should().HaveCount(4);
        VerifyFetched(CdnHost, 4);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldStop_AfterTooManyRedirects()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ => Redirect("/again.png");

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().HaveCount(IdentityProviderPictureDownloader.MaxRedirects + 1);
        _loggerMock.VerifyLog(LogLevel.Warning, "The identity provider picture at cdn.example.com redirected more than 3 times");
        VerifyFetched(CdnHost, 4);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_AnAnnouncedOversizedBody()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ =>
        {
            var response = Image([1]);
            response.Content.Headers.ContentLength = IdentityProviderPictureDownloader.MaxBytes + 1;
            return response;
        };

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _loggerMock.VerifyLog(LogLevel.Warning, "The identity provider picture at cdn.example.com is larger than 2097152 bytes");
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldAccept_ABodyOfExactlyTheLimit()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ => Image(new byte[IdentityProviderPictureDownloader.MaxBytes]);

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().HaveCount((int)IdentityProviderPictureDownloader.MaxBytes);
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_AnOversizedBodyWithoutALength()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ => UnannouncedImage(IdentityProviderPictureDownloader.MaxBytes + 1);

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _loggerMock.VerifyLog(LogLevel.Warning, "The identity provider picture at cdn.example.com is larger than 2097152 bytes");
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldAccept_ABodyWithoutALengthOfExactlyTheLimit()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ => UnannouncedImage(IdentityProviderPictureDownloader.MaxBytes);

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().HaveCount((int)IdentityProviderPictureDownloader.MaxBytes);
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData(null)]
    public async Task DownloadAsync_ShouldRefuse_ABodyThatIsNotAnImage(string? mediaType)
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ =>
        {
            var content = new ByteArrayContent("<html></html>"u8.ToArray());
            content.Headers.ContentType = mediaType == null ? null : new MediaTypeHeaderValue(mediaType);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        };

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _loggerMock.VerifyLog(LogLevel.Warning, "The identity provider picture at cdn.example.com is not an image");
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldAccept_AnUppercaseImageMediaType()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ =>
        {
            var response = Image([2]);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("IMAGE/PNG");
            return response;
        };

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().Equal(2);
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, 404)]
    [InlineData(HttpStatusCode.Ambiguous, 300)]
    [InlineData(HttpStatusCode.NotModified, 304)]
    public async Task DownloadAsync_ShouldReturnNothing_ForAnErrorStatus(HttpStatusCode status, int code)
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.Respond = _ => new HttpResponseMessage(status);

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().ContainSingle();
        _loggerMock.VerifyLog(LogLevel.Warning, $"The identity provider picture at cdn.example.com returned {code}");
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnNothing_WhenTheRequestTimesOut()
    {
        ResolveTo(CdnHost, PublicAddress);
        _handler.HangUntilCancelled = true;

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _loggerMock.VerifyLog(LogLevel.Warning, "Downloading the identity provider picture from cdn.example.com took longer than 5 seconds");
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnNothing_WhenTheNetworkFails()
    {
        ResolveTo(CdnHost, PublicAddress);
        var failure = new HttpRequestException("connection refused");
        _handler.Respond = _ => throw failure;

        var result = await _downloader.DownloadAsync(CdnPicture, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _loggerMock.VerifyLog(LogLevel.Warning, "Could not download the identity provider picture from cdn.example.com", failure);
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_ShouldPassOnCancellation_ByTheCaller()
    {
        ResolveTo(CdnHost, PublicAddress);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var action = () => _downloader.DownloadAsync(CdnPicture, Authority, cancelled.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        _handler.Requests.Should().BeEmpty();
        VerifyFetched(CdnHost);
        VerifyNoOtherCalls();
    }

    private void ResolveTo(string host, params IPAddress[] addresses)
    {
        _resolverMock.Setup(x => x.ResolveAsync(host, It.Is<CancellationToken>(t => t.CanBeCanceled))).ReturnsAsync(addresses);
    }

    private void VerifyFetched(string host, int hops = 1)
    {
        _httpClientFactoryMock.Verify(x => x.CreateClient(IdentityProviderPictureDownloader.HttpClientName), Times.Once);
        _resolverMock.Verify(x => x.ResolveAsync(host, It.Is<CancellationToken>(t => t.CanBeCanceled)), Times.Exactly(hops));
    }

    private void VerifyNoOtherCalls()
    {
        _httpClientFactoryMock.VerifyNoOtherCalls();
        _resolverMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    private static HttpResponseMessage Image(byte[] bytes)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static HttpResponseMessage UnannouncedImage(long size)
    {
        var content = new NoLengthContent(new StreamContent(new MemoryStream(new byte[size])));
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static HttpResponseMessage Redirect(string location, HttpStatusCode status = HttpStatusCode.Found)
    {
        var response = new HttpResponseMessage(status);
        response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
        return response;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK);

        public bool HangUntilCancelled { get; set; }

        public List<(HttpMethod Method, Uri Uri, string Accept, IPAddress[]? Pinned)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            request.Options.TryGetValue(PinnedAddressConnector.PinnedAddresses, out var pinned);
            Requests.Add((request.Method, request.RequestUri!, request.Headers.Accept.ToString(), pinned));
            if (HangUntilCancelled)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return Respond(request);
        }
    }

    private sealed class NoLengthContent(HttpContent inner) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => inner.CopyToAsync(stream);

        protected override Task<Stream> CreateContentReadStreamAsync() => inner.ReadAsStreamAsync();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
