using System.Net;
using System.Net.Sockets;
using System.Text;
using BoardGameTracker.Core.Email;
using FluentAssertions;
using MimeKit;
using Xunit;

namespace BoardGameTracker.Tests.Email;

public class MailKitSmtpSenderTests
{
    private readonly MailKitSmtpSender _sender = new();

    private static MimeMessage Message()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("BGT", "from@test.com"));
        message.To.Add(MailboxAddress.Parse("to@test.com"));
        message.Subject = "Hello";
        message.Body = new TextPart("plain") { Text = "Body" };
        return message;
    }

    private static string AuthPlain(string username, string password) =>
        "AUTH PLAIN " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"\0{username}\0{password}"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_ShouldDeliverWithoutAuthenticating_WhenThereIsNoUsername(string? username)
    {
        using var server = new FakeSmtpServer();

        await _sender.SendAsync(Message(), "127.0.0.1", server.Port, false, username, "pass", TestContext.Current.CancellationToken);

        var commands = await server.CommandsAsync();
        commands.Select(c => c.Split(' ')[0]).Should().Equal("EHLO", "MAIL", "RCPT", "DATA", "QUIT");
        commands[1].Should().StartWith("MAIL FROM:<from@test.com>");
        commands[2].Should().Be("RCPT TO:<to@test.com>");
        server.Data.Should().Contain("Subject: Hello").And.Contain("Body");
    }

    [Theory]
    [InlineData("pass", "pass")]
    [InlineData(null, "")]
    public async Task SendAsync_ShouldAuthenticateBeforeSending_WhenThereIsAUsername(string? password, string expected)
    {
        using var server = new FakeSmtpServer();

        await _sender.SendAsync(Message(), "127.0.0.1", server.Port, false, "user", password, TestContext.Current.CancellationToken);

        var commands = await server.CommandsAsync();
        commands.Select(c => c.Split(' ')[0]).Should().Equal("EHLO", "AUTH", "MAIL", "RCPT", "DATA", "QUIT");
        commands[1].Should().Be(AuthPlain("user", expected));
    }

    [Fact]
    public async Task SendAsync_ShouldRefuseToSendInTheClear_WhenTheServerCannotStartTls()
    {
        using var server = new FakeSmtpServer();

        var act = () => _sender.SendAsync(Message(), "127.0.0.1", server.Port, true, "user", "pass", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotSupportedException>();
        var commands = await server.CommandsAsync();
        commands.Should().ContainSingle().Which.Should().StartWith("EHLO");
    }

    [Fact]
    public async Task SendAsync_ShouldStopBeforeConnecting_WhenTheTokenIsCancelled()
    {
        using var server = new FakeSmtpServer();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => _sender.SendAsync(Message(), "127.0.0.1", server.Port, false, null, null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        server.Stop();
        (await server.CommandsAsync()).Should().BeEmpty();
    }

    private sealed class FakeSmtpServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly List<string> _commands = [];
        private readonly StringBuilder _data = new();
        private readonly Task _serving;

        public FakeSmtpServer()
        {
            _listener.Start();
            _serving = ServeAsync();
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public string Data => _data.ToString();

        public async Task<List<string>> CommandsAsync()
        {
            await _serving.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            return _commands;
        }

        public void Stop() => _listener.Stop();

        public void Dispose() => _listener.Dispose();

        private async Task ServeAsync()
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync();
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.Latin1);
                await ReplyAsync(stream, "220 fake ESMTP");
                while (await reader.ReadLineAsync() is { } line)
                {
                    _commands.Add(line);
                    var verb = line.Split(' ')[0].ToUpperInvariant();
                    if (verb == "EHLO")
                    {
                        await ReplyAsync(stream, "250-fake\r\n250 AUTH PLAIN");
                    }
                    else if (verb == "AUTH")
                    {
                        await ReplyAsync(stream, "235 accepted");
                    }
                    else if (verb == "DATA")
                    {
                        await ReplyAsync(stream, "354 go ahead");
                        while (await reader.ReadLineAsync() is { } body && body != ".")
                        {
                            _data.AppendLine(body);
                        }

                        await ReplyAsync(stream, "250 queued");
                    }
                    else if (verb == "QUIT")
                    {
                        await ReplyAsync(stream, "221 bye");
                        return;
                    }
                    else
                    {
                        await ReplyAsync(stream, "250 ok");
                    }
                }
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException or IOException)
            {
                _commands.TrimExcess();
            }
        }

        private static async Task ReplyAsync(NetworkStream stream, string reply) =>
            await stream.WriteAsync(Encoding.ASCII.GetBytes(reply + "\r\n"));
    }
}
