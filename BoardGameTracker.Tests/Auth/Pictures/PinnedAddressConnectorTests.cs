using System.Net;
using System.Net.Sockets;
using System.Text;
using BoardGameTracker.Core.Auth.Pictures;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth.Pictures;

public class PinnedAddressConnectorTests
{
    private const string Refused = "The request carries no vetted address to connect to.";

    [Fact]
    public async Task ConnectAsync_ShouldConnectToThePinnedAddress_InsteadOfResolvingTheHost()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = ServeOnceAsync(listener);
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"http://pinned.invalid:{port}/a.png");
        request.Options.Set(PinnedAddressConnector.PinnedAddresses, [IPAddress.Loopback]);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("pinned");
        (await server).Should().StartWith("GET /a.png HTTP/1.1").And.Contain($"Host: pinned.invalid:{port}");
    }

    [Fact]
    public async Task ConnectAsync_ShouldRefuse_ARequestWithoutPinnedAddresses()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:9/a.png");

        var action = () => client.SendAsync(request, TestContext.Current.CancellationToken);

        (await action.Should().ThrowAsync<HttpRequestException>()).Which.ToString().Should().Contain(Refused);
    }

    [Fact]
    public async Task ConnectAsync_ShouldRefuse_AnEmptyAddressList()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:9/a.png");
        request.Options.Set(PinnedAddressConnector.PinnedAddresses, []);

        var action = () => client.SendAsync(request, TestContext.Current.CancellationToken);

        (await action.Should().ThrowAsync<HttpRequestException>()).Which.ToString().Should().Contain(Refused);
    }

    [Fact]
    public async Task ConnectAsync_ShouldPassOnTheFailure_WhenThePinnedAddressRefusesTheConnection()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"http://pinned.invalid:{port}/a.png");
        request.Options.Set(PinnedAddressConnector.PinnedAddresses, [IPAddress.Loopback]);

        var action = () => client.SendAsync(request, TestContext.Current.CancellationToken);

        (await action.Should().ThrowAsync<HttpRequestException>()).WithInnerException<SocketException>();
    }

    private static HttpClient CreateClient()
    {
        return new HttpClient(new SocketsHttpHandler { ConnectCallback = PinnedAddressConnector.ConnectAsync, UseProxy = false });
    }

    private static async Task<string> ServeOnceAsync(TcpListener listener)
    {
        using var socket = await listener.AcceptTcpClientAsync(TestContext.Current.CancellationToken);
        var stream = socket.GetStream();
        var buffer = new byte[4096];
        var received = new StringBuilder();
        while (!received.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
        {
            var read = await stream.ReadAsync(buffer, TestContext.Current.CancellationToken);
            received.Append(Encoding.ASCII.GetString(buffer, 0, read));
        }

        await stream.WriteAsync("HTTP/1.1 200 OK\r\nContent-Length: 6\r\nConnection: close\r\n\r\npinned"u8.ToArray(), TestContext.Current.CancellationToken);
        return received.ToString();
    }
}
