using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace BoardGameTracker.Core.Email;

public class EmailService : IEmailService
{
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly ISmtpSender _smtpSender;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IEnvironmentProvider environmentProvider, ISmtpSender smtpSender, ILogger<EmailService> logger)
    {
        _environmentProvider = environmentProvider;
        _smtpSender = smtpSender;
        _logger = logger;
    }

    public bool IsConfigured => _environmentProvider.EmailEnabled;

    public async Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new DomainException(Constants.Errors.EmailNotConfigured);
        }

        var fromAddress = _environmentProvider.SmtpFromAddress!;
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_environmentProvider.SmtpFromName ?? fromAddress, fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody }.ToMessageBody();

        _logger.LogDebug("Sending email to {To} with subject {Subject}", to, subject);
        await _smtpSender.SendAsync(
            message,
            _environmentProvider.SmtpHost!,
            _environmentProvider.SmtpPort,
            _environmentProvider.SmtpUseSsl,
            _environmentProvider.SmtpUsername,
            _environmentProvider.SmtpPassword,
            cancellationToken);
    }
}
