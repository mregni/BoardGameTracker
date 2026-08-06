using MimeKit;

namespace BoardGameTracker.Core.Email.Interfaces;

public interface ISmtpSender
{
    Task SendAsync(MimeMessage message, string host, int port, bool useSsl, string? username, string? password, CancellationToken cancellationToken = default);
}
