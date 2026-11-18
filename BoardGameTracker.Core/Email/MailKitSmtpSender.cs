using BoardGameTracker.Core.Email.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BoardGameTracker.Core.Email;

public class MailKitSmtpSender : ISmtpSender
{
    public async Task SendAsync(MimeMessage message, string host, int port, bool useSsl, string? username, string? password, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient { Timeout = 15000 };
        var socketOptions = (port, useSsl) switch
        {
            (465, _) => SecureSocketOptions.SslOnConnect,
            (_, true) => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.None
        };
        await client.ConnectAsync(host, port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(username))
        {
            await client.AuthenticateAsync(username, password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
