using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using BoardGameTracker.Core.Auth.Pictures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth.Pictures;

public class IdentityProviderPictureDownloaderTests : IDisposable
{
    private const string Authority = "https://idp.example.com/realms/bgt";
    private static readonly IPAddress PublicAddress = IPAddress.Parse("93.184.216.34");

    private readonly StubHandler _handler = new();
    private readonly Mock<IHostAddressResolver> _resolverMock = new();
    private readonly Mock<ILogger<IdentityProviderPictureDownloader>> _loggerMock = new();
    private readonly IdentityProviderPictureDownloader _downloader;

    public IdentityProviderPictureDownloaderTests()
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(IdentityProviderPictureDownloader.HttpClientName)).Returns(() => new HttpClient(_handler, disposeHandler: false));
        _resolverMock.Setup(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([PublicAddress]);
        _downloader = new IdentityProviderPictureDownloader(factory.Object, _resolverMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _handler.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnTheImage_AndPinTheVettedAddresses()
    {
        _handler.Respond = _ => Image([1, 2, 3]);

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().Equal(1, 2, 3);
        var request = _handler.Requests.Should().ContainSingle().Subject;
        request.Uri.Should().Be(new Uri("https://cdn.example.com/a.png"));
        request.Pinned.Should().Equal(PublicAddress);
    }

    [Theory]
    [InlineData("http://cdn.example.com/a.png")]
    [InlineData("https://127.0.0.1/a.png")]
    [InlineData("data:image/png;base64,AAAA")]
    public async Task DownloadAsync_ShouldNotSendARequest_ForARefusedAddress(string url)
    {
        var result = await _downloader.DownloadAsync(url, Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
        VerifyWarning();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_WhenTheAuthorityIsNotAValidAddress()
    {
        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", "not an authority", TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData("10.1.2.3")]
    [InlineData("169.254.169.254")]
    [InlineData("127.0.0.1")]
    public async Task DownloadAsync_ShouldRefuseAHost_ThatResolvesToAnInternalAddress(string address)
    {
        _resolverMock.Setup(x => x.ResolveAsync("cdn.example.com", It.IsAny<CancellationToken>())).ReturnsAsync([PublicAddress, IPAddress.Parse(address)]);

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
        VerifyWarning();
    }

    [Fact]
    public async Task DownloadAsync_ShouldAcceptAPrivateAddress_OfTheAuthorityHost()
    {
        _resolverMock.Setup(x => x.ResolveAsync("idp.example.com", It.IsAny<CancellationToken>())).ReturnsAsync([IPAddress.Parse("192.168.1.20")]);
        _handler.Respond = _ => Image([7]);

        var result = await _downloader.DownloadAsync("https://idp.example.com/media/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().Equal(7);
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_WhenTheHostDoesNotResolve()
    {
        _resolverMock.Setup(x => x.ResolveAsync("cdn.example.com", It.IsAny<CancellationToken>())).ThrowsAsync(new SocketException((int)SocketError.HostNotFound));

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        VerifyWarning();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_ARedirectToAnotherHost()
    {
        _handler.Respond = _ => Redirect("https://evil.example.net/a.png");

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().ContainSingle();
        VerifyWarning();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_ARedirectToAnInsecureAddress()
    {
        _handler.Respond = _ => Redirect("http://cdn.example.com/a.png");

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_ARedirectWithoutALocation()
    {
        _handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.Found);

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAsync_ShouldFollowARelativeRedirect_OnTheSameHost()
    {
        _handler.Respond = request => request.RequestUri!.AbsolutePath == "/a.png" ? Redirect("/b.png") : Image([9]);

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().Equal(9);
        _handler.Requests.Select(r => r.Uri.AbsolutePath).Should().Equal("/a.png", "/b.png");
    }

    [Fact]
    public async Task DownloadAsync_ShouldStop_AfterTooManyRedirects()
    {
        _handler.Respond = _ => Redirect("/again.png");

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _handler.Requests.Should().HaveCount(IdentityProviderPictureDownloader.MaxRedirects + 1);
        VerifyWarning();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_AnAnnouncedOversizedBody()
    {
        _handler.Respond = _ =>
        {
            var response = Image([1]);
            response.Content.Headers.ContentLength = IdentityProviderPictureDownloader.MaxBytes + 1;
            return response;
        };

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        VerifyWarning();
    }

    [Fact]
    public async Task DownloadAsync_ShouldRefuse_AnOversizedBodyWithoutALength()
    {
        _handler.Respond = _ =>
        {
            var content = new NoLengthContent(new StreamContent(new MemoryStream(new byte[IdentityProviderPictureDownloader.MaxBytes + 10])));
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        };

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        VerifyWarning();
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData(null)]
    public async Task DownloadAsync_ShouldRefuse_ABodyThatIsNotAnImage(string? mediaType)
    {
        _handler.Respond = _ =>
        {
            var content = new ByteArrayContent("<html></html>"u8.ToArray());
            content.Headers.ContentType = mediaType == null ? null : new MediaTypeHeaderValue(mediaType);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        };

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        VerifyWarning();
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnNothing_ForAnErrorStatus()
    {
        _handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        VerifyWarning();
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnNothing_WhenTheRequestTimesOut()
    {
        _handler.Respond = _ => throw new TaskCanceledException("timed out");

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        VerifyWarning();
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnNothing_WhenTheNetworkFails()
    {
        _handler.Respond = _ => throw new HttpRequestException("connection refused");

        var result = await _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        VerifyWarning();
    }

    [Fact]
    public async Task DownloadAsync_ShouldPassOnCancellation_ByTheCaller()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _handler.Respond = _ => throw new TaskCanceledException();

        var action = () => _downloader.DownloadAsync("https://cdn.example.com/a.png", Authority, cancelled.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    private void VerifyWarning()
    {
        _loggerMock.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    private static HttpResponseMessage Image(byte[] bytes)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static HttpResponseMessage Redirect(string location)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
        return response;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK);

        public List<(Uri Uri, IPAddress[]? Pinned)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            request.Options.TryGetValue(PinnedAddressConnector.PinnedAddresses, out var pinned);
            Requests.Add((request.RequestUri!, pinned));
            return Task.FromResult(Respond(request));
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
